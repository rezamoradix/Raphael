using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raphael.Attributes;
using Raphael.Configuration;
using Raphael.Effects;
using Raphael.Models;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Raphael.Effects
{
    public interface IEffectProcessor
    {
        Task<SKBitmap> ApplyEffectsAsync(SKBitmap bitmap, RequestQueries queries);
        Dictionary<string, Type> GetAvailableEffects();
        Dictionary<string, EffectInfo> GetEffectInfo();
        Type? GetEffectType(string name);
    }

    public class EffectInfo
    {
        public string Name { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Dictionary<string, PropertyInfo> Properties { get; set; } = new();
        public int Order { get; set; }
        public bool Enabled { get; set; } = true;
    }

    public class EffectProcessor : IEffectProcessor
    {
        private readonly RaphaelConfig _config;
        private readonly ILogger<EffectProcessor> _logger;
        private readonly Dictionary<string, EffectInfo> _effectInfo;
        private readonly Dictionary<string, Type> _effects;
        private readonly Dictionary<string, EffectMapping> _mappings;

        public EffectProcessor(IOptions<RaphaelConfig> options, ILogger<EffectProcessor> logger)
        {
            _config = options.Value;
            _logger = logger;

            // Discover effects using the EffectAttribute
            _effects = DiscoverEffectsFromAssembly();
            _effectInfo = BuildEffectInfo();
            _mappings = BuildMappings();

            _logger.LogInformation("Discovered {Count} effects: {Effects}",
                _effects.Count,
                string.Join(", ", _effects.Keys));
        }

        public async Task<SKBitmap> ApplyEffectsAsync(SKBitmap bitmap, RequestQueries queries)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));

            if (queries == null)
                return bitmap;

            // Get effect order from config or use default
            var effectOrder = _config.Effects?.EffectOrder?.Split(',', StringSplitOptions.RemoveEmptyEntries) ??
                new[] { "Resize", "Crop", "Watermark" };

            // Build list of effects to apply
            var effectsToApply = new List<(string Name, Type Type, object Parameters)>();

            foreach (var effectName in effectOrder)
            {
                var effectType = GetEffectType(effectName.Trim());
                if (effectType == null)
                    continue;

                // Check if this effect should be applied based on query parameters
                var parameters = BuildEffectParameters(effectName.Trim(), queries);
                if (parameters != null)
                {
                    effectsToApply.Add((effectName.Trim(), effectType, parameters));
                }
            }

            // Apply effects in order
            var currentBitmap = bitmap;
            foreach (var (name, type, parameters) in effectsToApply)
            {
                try
                {
                    _logger.LogDebug("Applying effect: {EffectName}", name);
                    currentBitmap = await ApplyEffectAsync(currentBitmap, type, parameters);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to apply effect {EffectName}", name);
                    throw;
                }
            }

            return currentBitmap;
        }

        private async Task<SKBitmap> ApplyEffectAsync(SKBitmap bitmap, Type effectType, object parameters)
        {
            if (!typeof(IEffect).IsAssignableFrom(effectType))
                throw new InvalidOperationException($"Type {effectType.Name} does not implement IEffect");

            var effect = (IEffect)Activator.CreateInstance(effectType)!;

            // Map parameters to effect properties
            var paramDict = parameters as Dictionary<string, object>;
            if (paramDict != null)
            {
                foreach (var prop in effectType.GetProperties())
                {
                    if (paramDict.TryGetValue(prop.Name, out var value) && value != null)
                    {
                        try
                        {
                            var convertedValue = Convert.ChangeType(value, prop.PropertyType);
                            prop.SetValue(effect, convertedValue);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to set property {Property} on {EffectType}",
                                prop.Name, effectType.Name);
                        }
                    }
                }
            }

            await effect.Apply(bitmap);
            return bitmap;
        }

        private Dictionary<string, Type> DiscoverEffectsFromAssembly()
        {
            var effects = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            var processedAssemblies = new HashSet<Assembly>();

            // Get all loaded assemblies
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                if (processedAssemblies.Contains(assembly))
                    continue;

                processedAssemblies.Add(assembly);

                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        // Check if type implements IEffect and is not abstract
                        if (typeof(IEffect).IsAssignableFrom(type) &&
                            !type.IsInterface &&
                            !type.IsAbstract)
                        {
                            // Get the EffectAttribute
                            var attribute = type.GetCustomAttribute<EffectAttribute>();
                            if (attribute != null)
                            {
                                var name = attribute.Name;
                                if (!string.IsNullOrEmpty(name))
                                {
                                    effects[name] = type;
                                    effects[type.Name] = type;
                                    effects[type.FullName ?? type.Name] = type;

                                    _logger.LogDebug("Discovered effect: {Name} -> {Type}", name, type.FullName);
                                }
                            }
                            else
                            {
                                // If no attribute, use the type name without "Effect" suffix
                                var name = type.Name.Replace("Effect", "");
                                effects[name] = type;
                                effects[type.Name] = type;
                                effects[type.FullName ?? type.Name] = type;

                                _logger.LogDebug("Discovered effect (no attribute): {Name} -> {Type}", name, type.FullName);
                            }
                        }
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    _logger.LogWarning(ex, "Failed to load types from assembly {Assembly}", assembly.FullName);
                    // Try to load types that succeeded
                    foreach (var type in ex.Types.Where(t => t != null))
                    {
                        if (type != null && typeof(IEffect).IsAssignableFrom(type) &&
                            !type.IsInterface && !type.IsAbstract)
                        {
                            var attribute = type.GetCustomAttribute<EffectAttribute>();
                            var name = attribute?.Name ?? type.Name.Replace("Effect", "");
                            effects[name] = type;
                            effects[type.Name] = type;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process assembly {Assembly}", assembly.FullName);
                }
            }

            return effects;
        }

        private Dictionary<string, EffectInfo> BuildEffectInfo()
        {
            var info = new Dictionary<string, EffectInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var (name, type) in _effects)
            {
                // Only add the primary name (not aliases)
                var attribute = type.GetCustomAttribute<EffectAttribute>();
                var primaryName = attribute?.Name ?? type.Name.Replace("Effect", "");

                if (!string.Equals(name, primaryName, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(name, type.Name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var effectInfo = new EffectInfo
                {
                    Name = primaryName,
                    TypeName = type.FullName ?? type.Name,
                    Description = attribute?.Description,
                    Properties = type.GetProperties()
                        .Where(p => p.CanWrite)
                        .ToDictionary(p => p.Name, p => p),
                    Order = attribute?.Order ?? 999,
                    Enabled = attribute?.Enabled ?? true
                };

                info[primaryName] = effectInfo;

                // Also store by type name
                info[type.Name] = effectInfo;
            }

            return info;
        }

        private Dictionary<string, EffectMapping> BuildMappings()
        {
            var mappings = new Dictionary<string, EffectMapping>(StringComparer.OrdinalIgnoreCase);

            // Load mappings from configuration
            if (_config.Effects?.Mappings != null)
            {
                foreach (var mapping in _config.Effects.Mappings)
                {
                    mappings[mapping.EffectType] = mapping;
                }
            }

            // Build default mappings for discovered effects that don't have config
            foreach (var (name, type) in _effects)
            {
                var primaryName = type.GetCustomAttribute<EffectAttribute>()?.Name ??
                                  type.Name.Replace("Effect", "");

                if (!mappings.ContainsKey(primaryName) && !mappings.ContainsKey(type.Name))
                {
                    var mapping = CreateDefaultMapping(primaryName, type);
                    mappings[primaryName] = mapping;
                    mappings[type.Name] = mapping;
                }
            }

            return mappings;
        }

        private EffectMapping CreateDefaultMapping(string effectName, Type effectType)
        {
            var mapping = new EffectMapping
            {
                EffectType = effectName,
                Order = 999,
                Defaults = new Dictionary<string, string>(),
                Mappings = new Dictionary<string, string>()
            };

            // Generate default mappings for properties
            foreach (var prop in effectType.GetProperties().Where(p => p.CanWrite))
            {
                // Try to create a query parameter name
                var queryName = prop.Name;

                // Add prefix based on effect name
                if (!effectName.Equals("Resize", StringComparison.OrdinalIgnoreCase))
                {
                    queryName = effectName + prop.Name;
                }

                mapping.Mappings[queryName] = prop.Name;

                // Set default values based on property type
                if (prop.PropertyType == typeof(int))
                    mapping.Defaults[prop.Name] = "0";
                else if (prop.PropertyType == typeof(float) || prop.PropertyType == typeof(double))
                    mapping.Defaults[prop.Name] = "0";
                else if (prop.PropertyType == typeof(bool))
                    mapping.Defaults[prop.Name] = "false";
                else if (prop.PropertyType.IsEnum)
                    mapping.Defaults[prop.Name] = "0";
                else if (prop.PropertyType == typeof(string))
                    mapping.Defaults[prop.Name] = string.Empty;
            }

            return mapping;
        }

        private object? BuildEffectParameters(string effectName, RequestQueries queries)
        {
            var mapping = _mappings.GetValueOrDefault(effectName);
            if (mapping == null)
            {
                // Try to find by type name
                mapping = _mappings.Values.FirstOrDefault(m =>
                    m.EffectType.Equals(effectName, StringComparison.OrdinalIgnoreCase));
            }

            if (mapping == null)
                return null;

            var parameters = new Dictionary<string, object>();

            // Start with defaults
            foreach (var (key, value) in mapping.Defaults)
            {
                parameters[key] = value;
            }

            // Get query parameter values
            var queryProps = typeof(RequestQueries).GetProperties();

            foreach (var (queryParam, effectProp) in mapping.Mappings)
            {
                var queryProp = queryProps.FirstOrDefault(p =>
                    p.Name.Equals(queryParam, StringComparison.OrdinalIgnoreCase));

                if (queryProp != null)
                {
                    var value = queryProp.GetValue(queries);
                    if (value != null)
                    {
                        // Check if the value is different from default
                        if (parameters.TryGetValue(effectProp, out var defaultValue))
                        {
                            // Only override if different from default
                            if (!Equals(value, defaultValue))
                            {
                                parameters[effectProp] = value;
                            }
                        }
                        else
                        {
                            parameters[effectProp] = value;
                        }
                    }
                }
            }

            // Check if any parameters are actually set (not just defaults)
            var hasCustomParams = false;
            foreach (var key in parameters.Keys)
            {
                if (!mapping.Defaults.TryGetValue(key, out var defaultValue) ||
                    !Equals(parameters[key], defaultValue))
                {
                    hasCustomParams = true;
                    break;
                }
            }

            return hasCustomParams ? parameters : null;
        }

        public Type? GetEffectType(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            // Try exact match
            if (_effects.TryGetValue(name, out var type))
                return type;

            // Try with "Effect" suffix
            var withSuffix = name + "Effect";
            if (_effects.TryGetValue(withSuffix, out type))
                return type;

            // Try to find by attribute name
            foreach (var (key, value) in _effects)
            {
                var attr = value.GetCustomAttribute<EffectAttribute>();
                if (attr?.Name?.Equals(name, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return value;
                }
            }

            return null;
        }

        public Dictionary<string, Type> GetAvailableEffects()
        {
            return new Dictionary<string, Type>(_effects);
        }

        public Dictionary<string, EffectInfo> GetEffectInfo()
        {
            return new Dictionary<string, EffectInfo>(_effectInfo);
        }
    }
}