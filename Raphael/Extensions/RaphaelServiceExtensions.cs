using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raphael.Configuration;
using Raphael.Effects;
using Raphael.Loaders;
using System.Reflection;

namespace Raphael.Extensions
{
    public static class RaphaelServiceExtensions
    {
        public static IServiceCollection AddRaphael(this IServiceCollection services, IConfiguration configuration)
        {
            // Load configuration
            var config = new RaphaelConfig();
            configuration.GetSection("Raphael").Bind(config);

            if (config == null)
                throw new InvalidOperationException("Raphael configuration not found in appsettings.json");

            // Register configuration
            services.AddSingleton(config);
            services.AddSingleton(Options.Create(config));

            // Add memory caching
            if (config.Cache.EnableMemoryCache)
            {
                services.AddMemoryCache();
            }

            // Register loader factory. ILoaderRegistry itself is registered inside
            // RegisterLoadersFromConfig below, not here - see that method's comment.
            services.AddSingleton<LoaderFactory>();

            // Create and register ImageLoaderOptions
            var loaderOptions = new ImageLoaderOptions
            {
                EnableMemoryCache = config.Cache.EnableMemoryCache,
                EnableFileCache = config.Cache.EnableFileCache,
                FileCacheDirectory = config.Cache.FileCacheDirectory,
                MemoryCacheSlidingExpiration = TimeSpan.FromMinutes(config.Cache.CacheDurationMinutes),
                FileCacheMaxAge = TimeSpan.FromHours(config.Cache.FileCacheMaxAgeHours),
                EnableFileCacheCleanup = config.Cache.EnableFileCacheCleanup,
                MemoryCacheSizeLimit = config.Cache.MemoryCacheSizeLimit
            };
            services.AddSingleton(loaderOptions);

            // Register loaders from configuration
            RegisterLoadersFromConfig(services, config);

            // Register main services
            services.AddSingleton<IImageLoaderService, ImageLoaderService>();
            services.AddSingleton<ICachedImageProcessor, CachedImageProcessor>();
            services.AddSingleton<IEffectProcessor, EffectProcessor>();

            return services;
        }

        private static void RegisterLoadersFromConfig(IServiceCollection services, RaphaelConfig config)
        {
            // The sole ILoaderRegistry registration (AddRaphael no longer registers one
            // separately - see its own comment). Building the concrete LoaderRegistry directly
            // here, rather than resolving ILoaderRegistry from the container, matters: the old
            // code called sp.GetRequiredService<ILoaderRegistry>() from *inside* this same
            // factory, which - now that this is also the only descriptor for that service type -
            // would deadlock resolving itself (the singleton lock for ILoaderRegistry isn't
            // re-entrant), rather than recursing or throwing a circular-dependency error.
            services.AddSingleton<ILoaderRegistry>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<LoaderRegistry>>();
                var registry = new LoaderRegistry(logger);
                var factory = sp.GetRequiredService<LoaderFactory>();

                // Register each loader from config
                foreach (var (name, loaderEntry) in config.Loaders.Loaders)
                {
                    try
                    {
                        var loader = factory.CreateLoader(name, loaderEntry);
                        if (loader != null)
                        {
                            registry.RegisterLoader(name, loader);

                            // Register as singleton in DI for injection
                            services.AddSingleton(loader);
                            services.AddSingleton<IImageLoader>(loader);
                        }
                        else
                        {
                            logger.LogWarning("Failed to create loader: {Name}", name);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to register loader: {Name}", name);
                    }
                }

                return registry;
            });

            // Register the loader service
            services.AddSingleton<IImageLoaderService>(sp =>
            {
                var registry = sp.GetRequiredService<ILoaderRegistry>();
                var memoryCache = config.Cache.EnableMemoryCache ? sp.GetService<IMemoryCache>() : null;
                var logger = sp.GetRequiredService<ILogger<ImageLoaderService>>();
                var options = sp.GetRequiredService<ImageLoaderOptions>();

                return new ImageLoaderService(registry, memoryCache, logger, options);
            });
        }

        /// <summary>
        /// Register a custom loader programmatically
        /// </summary>
        public static IServiceCollection RegisterLoader<T>(this IServiceCollection services, string name)
            where T : class, IImageLoader
        {
            services.AddSingleton<T>();
            services.AddSingleton<IImageLoader>(sp => sp.GetRequiredService<T>());

            // Register in registry after build
            services.AddSingleton(sp =>
            {
                var registry = sp.GetRequiredService<ILoaderRegistry>();
                var loader = sp.GetRequiredService<T>();
                registry.RegisterLoader(name, loader);
                return registry;
            });

            return services;
        }

        /// <summary>
        /// Register a custom loader programmatically
        /// </summary>
        public static IServiceCollection RegisterLoader(this IServiceCollection services, string name, IImageLoader loader)
        {
            services.AddSingleton(loader);
            services.AddSingleton<IImageLoader>(loader);

            services.AddSingleton(sp =>
            {
                var registry = sp.GetRequiredService<ILoaderRegistry>();
                registry.RegisterLoader(name, loader);
                return registry;
            });

            return services;
        }
    }
}
