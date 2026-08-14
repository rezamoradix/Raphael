using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Raphael.Configuration;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Raphael.Loaders
{
    public interface ILoaderRegistry
    {
        void RegisterLoader(string name, IImageLoader loader);
        IImageLoader? GetLoader(string name);
        IEnumerable<IImageLoader> GetAllLoaders();
        bool IsLoaderRegistered(string name);
    }

    public class LoaderRegistry : ILoaderRegistry
    {
        private readonly Dictionary<string, IImageLoader> _loaders = new();
        private readonly ILogger<LoaderRegistry> _logger;

        public LoaderRegistry(ILogger<LoaderRegistry> logger)
        {
            _logger = logger;
        }

        public void RegisterLoader(string name, IImageLoader loader)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Loader name cannot be null or empty", nameof(name));

            if (loader == null)
                throw new ArgumentNullException(nameof(loader));

            _loaders[name] = loader;
            _logger.LogInformation("Registered loader: {Name} ({Type})", name, loader.GetType().Name);
        }

        public IImageLoader? GetLoader(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            _loaders.TryGetValue(name, out var loader);
            return loader;
        }

        public IEnumerable<IImageLoader> GetAllLoaders()
        {
            return _loaders.Values;
        }

        public bool IsLoaderRegistered(string name)
        {
            return !string.IsNullOrEmpty(name) && _loaders.ContainsKey(name);
        }
    }

    public class LoaderFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LoaderFactory> _logger;

        public LoaderFactory(IServiceProvider serviceProvider, ILogger<LoaderFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public IImageLoader? CreateLoader(string name, LoaderEntry config)
        {
            if (!config.Enabled)
            {
                _logger.LogDebug("Loader {Name} is disabled", name);
                return null;
            }

            try
            {
                Type? loaderType = null;

                // Try to get type from configuration
                if (!string.IsNullOrEmpty(config.Type))
                {
                    if (!string.IsNullOrEmpty(config.Assembly))
                    {
                        var assembly = Assembly.Load(config.Assembly);
                        loaderType = assembly.GetType(config.Type);
                    }
                    else
                    {
                        loaderType = Type.GetType(config.Type);
                    }
                }

                // If type not found, try to find by name in loaded assemblies
                if (loaderType == null)
                {
                    loaderType = FindLoaderTypeByName(name);
                }

                if (loaderType == null)
                {
                    _logger.LogError("Loader type not found for {Name}", name);
                    return null;
                }

                if (!typeof(IImageLoader).IsAssignableFrom(loaderType))
                {
                    _logger.LogError("Type {Type} does not implement IImageLoader", loaderType.FullName);
                    return null;
                }

                // Create instance using DI if possible, or manually
                var loader = CreateInstance(loaderType, config.Parameters);

                if (loader != null)
                {
                    _logger.LogInformation("Created loader: {Name} -> {Type}", name, loaderType.FullName);
                }

                return loader;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create loader {Name}", name);
                return null;
            }
        }

        private Type? FindLoaderTypeByName(string name)
        {
            // Try common naming patterns
            var typeNames = new[]
            {
                name,
                $"{name}Loader",
                $"{name}ImageLoader",
                $"Raphael.Loaders.{name}Loader",
                $"Raphael.Loaders.{name}ImageLoader"
            };

            foreach (var typeName in typeNames)
            {
                // Try all loaded assemblies
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        var type = assembly.GetType(typeName);
                        if (type != null && typeof(IImageLoader).IsAssignableFrom(type))
                        {
                            return type;
                        }
                    }
                    catch
                    {
                        // Skip assemblies that can't be loaded
                    }
                }
            }

            return null;
        }

        private IImageLoader? CreateInstance(Type loaderType, Dictionary<string, object> parameters)
        {
            try
            {
                // Try to create with DI
                try
                {
                    return (IImageLoader)ActivatorUtilities.CreateInstance(_serviceProvider, loaderType);
                }
                catch
                {
                    // Fall back to manual creation
                }

                // Try parameterless constructor
                var constructor = loaderType.GetConstructor(Type.EmptyTypes);
                if (constructor != null)
                {
                    return (IImageLoader)constructor.Invoke(Array.Empty<object>());
                }

                // Try to find a constructor that matches parameters
                var constructors = loaderType.GetConstructors();
                foreach (var ctor in constructors)
                {
                    var paramInfos = ctor.GetParameters();
                    if (paramInfos.Length == 0)
                        continue;

                    var args = new List<object>();
                    var allParamsFound = true;

                    foreach (var paramInfo in paramInfos)
                    {
                        if (parameters.TryGetValue(paramInfo.Name!, out var value))
                        {
                            try
                            {
                                var converted = Convert.ChangeType(value, paramInfo.ParameterType);
                                args.Add(converted);
                            }
                            catch
                            {
                                allParamsFound = false;
                                break;
                            }
                        }
                        else
                        {
                            allParamsFound = false;
                            break;
                        }
                    }

                    if (allParamsFound)
                    {
                        return (IImageLoader)ctor.Invoke(args.ToArray());
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create instance of {Type}", loaderType.FullName);
                return null;
            }
        }
    }
}