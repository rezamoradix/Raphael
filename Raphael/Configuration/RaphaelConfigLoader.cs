using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Raphael.Configuration
{
    public static class RaphaelEnvironmentConfigLoader
    {
        private static RaphaelEnvironmentConfig? _config;
        private static readonly object _lock = new();

        /// <summary>
        /// Load configuration from appsettings.json and environment variables
        /// </summary>
        public static RaphaelEnvironmentConfig LoadConfiguration(IConfiguration? configuration = null)
        {
            lock (_lock)
            {
                if (_config != null)
                    return _config;

                var config = new RaphaelEnvironmentConfig();

                try
                {
                    // Try to load from IConfiguration (from DI)
                    if (configuration != null)
                    {
                        var section = configuration.GetSection("Raphael");
                        if (section.Exists())
                        {
                            var envConfig = section.Get<RaphaelEnvironmentConfig>();
                            if (envConfig != null)
                            {
                                config = envConfig;
                            }
                        }
                    }

                    // Try to load from appsettings.json if not loaded
                    if (config.Environments.Count == 0)
                    {
                        var appSettingsPath = Path.Combine(
                            AppDomain.CurrentDomain.BaseDirectory,
                            "appsettings.json"
                        );

                        if (File.Exists(appSettingsPath))
                        {
                            var json = File.ReadAllText(appSettingsPath);
                            var options = new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true,
                                Converters = { new JsonStringEnumConverter() }
                            };

                            var appConfig = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, options);
                            if (appConfig != null && appConfig.TryGetValue("Raphael", out var raphaelConfig))
                            {
                                var envConfig = JsonSerializer.Deserialize<RaphaelEnvironmentConfig>(
                                    raphaelConfig.GetRawText(), options
                                );
                                if (envConfig != null)
                                {
                                    config = envConfig;
                                }
                            }
                        }
                    }

                    // Override with environment variable
                    var envOverride = Environment.GetEnvironmentVariable("RAPHAEL_ENV");
                    if (!string.IsNullOrEmpty(envOverride))
                    {
                        config.CurrentEnvironment = envOverride;
                    }

                    // Try to load environment-specific config
                    var envConfigPath = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        $"appsettings.{config.CurrentEnvironment}.json"
                    );

                    if (File.Exists(envConfigPath))
                    {
                        var json = File.ReadAllText(envConfigPath);
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            Converters = { new JsonStringEnumConverter() }
                        };

                        var appConfig = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, options);
                        if (appConfig != null && appConfig.TryGetValue("Raphael", out var raphaelConfig))
                        {
                            var envOverrides = JsonSerializer.Deserialize<RaphaelEnvironmentConfig>(
                                raphaelConfig.GetRawText(), options
                            );
                            if (envOverrides != null)
                            {
                                // Merge environment overrides
                                foreach (var kvp in envOverrides.Environments)
                                {
                                    config.Environments[kvp.Key] = kvp.Value;
                                }
                            }
                        }
                    }

                    // Process inheritance
                    foreach (var envName in config.Environments.Keys.ToList())
                    {
                        var envConfig = config.Environments[envName];
                        if (!string.IsNullOrEmpty(envConfig.InheritsFrom))
                        {
                            if (config.Environments.TryGetValue(envConfig.InheritsFrom, out var parentConfig))
                            {
                                config.Environments[envName] = envConfig.Merge(parentConfig);
                            }
                        }
                    }

                    // Ensure current environment exists
                    if (!config.HasEnvironment(config.CurrentEnvironment))
                    {
                        config.Environments[config.CurrentEnvironment] = new EnvironmentConfig();
                    }

                    _config = config;
                    return config;
                }
                catch (Exception ex)
                {
                    // If config loading fails, use defaults
                    config.Environments["Development"] = new EnvironmentConfig();
                    config.CurrentEnvironment = "Development";
                    _config = config;
                    return config;
                }
            }
        }

        /// <summary>
        /// Register environment config in DI
        /// </summary>
        public static IServiceCollection AddRaphaelEnvironmentConfig(this IServiceCollection services, IConfiguration configuration)
        {
            var config = LoadConfiguration(configuration);
            services.AddSingleton(config);
            services.AddSingleton(Options.Create(config));
            services.AddSingleton<IOptions<RaphaelEnvironmentConfig>>(sp =>
                new OptionsWrapper<RaphaelEnvironmentConfig>(config));

            // Register current environment config
            var envConfig = config.Current;
            services.AddSingleton(envConfig);
            services.AddSingleton(Options.Create(envConfig));
            services.AddSingleton<IOptions<EnvironmentConfig>>(sp =>
                new OptionsWrapper<EnvironmentConfig>(envConfig));

            return services;
        }

        /// <summary>
        /// Get the current environment name
        /// </summary>
        public static string GetCurrentEnvironment()
        {
            var config = LoadConfiguration();
            return config.CurrentEnvironment;
        }

        /// <summary>
        /// Get environment-specific configuration
        /// </summary>
        public static EnvironmentConfig GetEnvironmentConfig(string? environmentName = null)
        {
            var config = LoadConfiguration();
            var env = environmentName ?? config.CurrentEnvironment;
            return config.GetEnvironment(env);
        }

        /// <summary>
        /// Get all environment names
        /// </summary>
        public static IEnumerable<string> GetEnvironments()
        {
            var config = LoadConfiguration();
            return config.Environments.Keys;
        }

        /// <summary>
        /// Check if environment exists
        /// </summary>
        public static bool EnvironmentExists(string name)
        {
            var config = LoadConfiguration();
            return config.HasEnvironment(name);
        }

        /// <summary>
        /// Reload configuration
        /// </summary>
        public static RaphaelEnvironmentConfig Reload()
        {
            lock (_lock)
            {
                _config = null;
                return LoadConfiguration();
            }
        }
    }
}