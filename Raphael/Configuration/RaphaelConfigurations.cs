using Raphael.Effects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Raphael.Configuration
{
    /// <summary>
    /// Main configuration for Raphael image processing
    /// </summary>
    public class RaphaelConfig
    {
        /// <summary>
        /// Current environment name
        /// </summary>
        public string Environment { get; set; } = "Development";

        /// <summary>
        /// Enable/disable Raphael entirely
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Loader configurations
        /// </summary>
        public LoaderConfig Loaders { get; set; } = new();

        /// <summary>
        /// Cache configurations
        /// </summary>
        public CacheConfig Cache { get; set; } = new();

        /// <summary>
        /// Processing configurations
        /// </summary>
        public ProcessingConfig Processing { get; set; } = new();

        /// <summary>
        /// Security configurations
        /// </summary>
        public SecurityConfig Security { get; set; } = new();

        /// <summary>
        /// Effect configurations
        /// </summary>
        public EffectsConfig Effects { get; set; } = new();

        /// <summary>
        /// Logging configurations
        /// </summary>
        public LoggingConfig Logging { get; set; } = new();

        /// <summary>
        /// Performance configurations
        /// </summary>
        public PerformanceConfig Performance { get; set; } = new();

        /// <summary>
        /// Feature flags
        /// </summary>
        public FeatureConfig Features { get; set; } = new();

        /// <summary>
        /// Custom settings dictionary for extensibility
        /// </summary>
        public Dictionary<string, object> Custom { get; set; } = new();
    }

    /// <summary>
    /// Loader configurations
    /// </summary>
    public class LoaderConfig
    {
        /// <summary>
        /// Dynamic loader configurations keyed by name
        /// </summary>
        public Dictionary<string, LoaderEntry> Loaders { get; set; } = new();

        /// <summary>
        /// Default loader priority
        /// </summary>
        public int DefaultPriority { get; set; } = 0;

        /// <summary>
        /// Enable loader caching
        /// </summary>
        public bool EnableLoaderCaching { get; set; } = true;

        /// <summary>
        /// HTTP loader configuration
        /// </summary>
        public HttpLoaderConfig Http { get; set; } = new();

        /// <summary>
        /// Local file loader configuration
        /// </summary>
        public LocalLoaderConfig Local { get; set; } = new();

        /// <summary>
        /// Custom loaders configuration
        /// </summary>
        public List<CustomLoaderConfig> CustomLoaders { get; set; } = new();
    }

    /// <summary>
    /// Individual loader entry
    /// </summary>
    public class LoaderEntry
    {
        /// <summary>
        /// Loader type (full name)
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Assembly name containing the loader
        /// </summary>
        public string? Assembly { get; set; }

        /// <summary>
        /// Whether this loader is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Priority (higher = loaded first)
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// Loader-specific parameters
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();

        /// <summary>
        /// Loader-specific options
        /// </summary>
        public Dictionary<string, object> Options { get; set; } = new();
    }

    /// <summary>
    /// HTTP loader configuration
    /// </summary>
    public class HttpLoaderConfig
    {
        /// <summary>
        /// Maximum file size in MB
        /// </summary>
        public int MaxFileSizeMB { get; set; } = 10;

        /// <summary>
        /// Maximum image width
        /// </summary>
        public int MaxWidth { get; set; } = 8192;

        /// <summary>
        /// Maximum image height
        /// </summary>
        public int MaxHeight { get; set; } = 8192;

        /// <summary>
        /// Maximum total pixels (width * height)
        /// </summary>
        public int MaxPixels { get; set; } = 67108864; // 8192 x 8192

        /// <summary>
        /// HTTP timeout in seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Allowed domains (empty = all domains if AllowAllDomains is true)
        /// </summary>
        public List<string> AllowedDomains { get; set; } = new();

        /// <summary>
        /// Allow all domains (overrides AllowedDomains)
        /// </summary>
        public bool AllowAllDomains { get; set; } = true;

        /// <summary>
        /// Validate content type is image/*
        /// </summary>
        public bool ValidateContentType { get; set; } = true;

        /// <summary>
        /// Validate image dimensions
        /// </summary>
        public bool ValidateDimensions { get; set; } = true;

        /// <summary>
        /// Block internal/local addresses
        /// </summary>
        public bool BlockInternalAddresses { get; set; } = true;

        /// <summary>
        /// User agent string for HTTP requests
        /// </summary>
        public string UserAgent { get; set; } = "RaphaelImageProcessor/1.0";

        /// <summary>
        /// Enable HTTP compression
        /// </summary>
        public bool EnableCompression { get; set; } = true;

        /// <summary>
        /// Maximum redirects to follow
        /// </summary>
        public int MaxRedirects { get; set; } = 3;

        /// <summary>
        /// Enable automatic decompression
        /// </summary>
        public bool EnableAutomaticDecompression { get; set; } = true;

        /// <summary>
        /// Headers to include in HTTP requests
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new();

        /// <summary>
        /// Proxy configuration
        /// </summary>
        public ProxyConfig? Proxy { get; set; }
    }

    /// <summary>
    /// Proxy configuration
    /// </summary>
    public class ProxyConfig
    {
        public string? Address { get; set; }
        public int? Port { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public bool BypassProxyForLocal { get; set; } = true;
    }

    /// <summary>
    /// Local file loader configuration
    /// </summary>
    public class LocalLoaderConfig
    {
        /// <summary>
        /// Directories to search for images
        /// </summary>
        public List<string> Directories { get; set; } = new();

        /// <summary>
        /// Search recursively in directories
        /// </summary>
        public bool Recursive { get; set; } = true;

        /// <summary>
        /// Validate file extensions
        /// </summary>
        public bool ValidateExtensions { get; set; } = true;

        /// <summary>
        /// Allowed file extensions
        /// </summary>
        public List<string> AllowedExtensions { get; set; } = new();

        /// <summary>
        /// Block system files (e.g., /etc/passwd)
        /// </summary>
        public bool BlockSystemFiles { get; set; } = true;

        /// <summary>
        /// Maximum file size in MB for local files
        /// </summary>
        public int MaxFileSizeMB { get; set; } = 50;

        /// <summary>
        /// Enable file watching for changes
        /// </summary>
        public bool EnableFileWatching { get; set; } = false;
    }

    /// <summary>
    /// Custom loader configuration
    /// </summary>
    public class CustomLoaderConfig
    {
        /// <summary>
        /// Name of the custom loader
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Loader type (full name)
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Assembly name containing the loader
        /// </summary>
        public string? Assembly { get; set; }

        /// <summary>
        /// Whether this loader is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Priority (higher = loaded first)
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// Loader-specific parameters
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();

        /// <summary>
        /// Loader-specific options
        /// </summary>
        public Dictionary<string, object> Options { get; set; } = new();
    }

    /// <summary>
    /// Cache configuration
    /// </summary>
    public class CacheConfig
    {
        /// <summary>
        /// Enable memory cache
        /// </summary>
        public bool EnableMemoryCache { get; set; } = true;

        /// <summary>
        /// Enable file cache
        /// </summary>
        public bool EnableFileCache { get; set; } = true;

        /// <summary>
        /// File cache directory
        /// </summary>
        public string? FileCacheDirectory { get; set; }

        /// <summary>
        /// Subdirectory for processed image cache (within FileCacheDirectory)
        /// </summary>
        public string? ProcessedCacheDirectory { get; set; }

        /// <summary>
        /// Cache duration in minutes
        /// </summary>
        public int CacheDurationMinutes { get; set; } = 10;

        /// <summary>
        /// Memory cache size limit (number of items)
        /// </summary>
        public int MemoryCacheSizeLimit { get; set; } = 100;

        /// <summary>
        /// File cache max age in hours
        /// </summary>
        public int FileCacheMaxAgeHours { get; set; } = 168; // 7 days

        /// <summary>
        /// Enable automatic file cache cleanup
        /// </summary>
        public bool EnableFileCacheCleanup { get; set; } = true;

        /// <summary>
        /// Enable Redis cache
        /// </summary>
        public bool EnableRedisCache { get; set; } = false;

        /// <summary>
        /// Redis connection string
        /// </summary>
        public string? RedisConnectionString { get; set; }

        /// <summary>
        /// Redis cache duration in minutes
        /// </summary>
        public int RedisCacheDurationMinutes { get; set; } = 60;

        /// <summary>
        /// Enable cache compression
        /// </summary>
        public bool EnableCacheCompression { get; set; } = false;

        /// <summary>
        /// Cache key prefix
        /// </summary>
        public string? CacheKeyPrefix { get; set; } = "raphael:";

        /// <summary>
        /// Enable cache statistics
        /// </summary>
        public bool EnableCacheStatistics { get; set; } = false;
    }

    /// <summary>
    /// Processing configuration
    /// </summary>
    public class ProcessingConfig
    {
        /// <summary>
        /// Default image quality (1-100)
        /// </summary>
        public int DefaultQuality { get; set; } = 75;

        /// <summary>
        /// Default image format
        /// </summary>
        public string DefaultFormat { get; set; } = "Jpeg";

        /// <summary>
        /// Strip EXIF metadata from images
        /// </summary>
        public bool StripMetadata { get; set; } = true;

        /// <summary>
        /// Allow animated images (GIF, WebP, APNG)
        /// </summary>
        public bool AllowAnimatedImages { get; set; } = true;

        /// <summary>
        /// Maximum animation frames to process
        /// </summary>
        public int MaxAnimationFrames { get; set; } = 100;

        /// <summary>
        /// Enable parallel processing for batch operations
        /// </summary>
        public bool EnableParallelProcessing { get; set; } = true;

        /// <summary>
        /// Maximum degree of parallelism
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; } = 4;

        /// <summary>
        /// Default resize sampling quality (Fast/Smooth/High) used when a request doesn't specify one
        /// </summary>
        public ResizeQuality DefaultResizeQuality { get; set; } = ResizeQuality.Smooth;

        /// <summary>
        /// Enable image optimization
        /// </summary>
        public bool EnableOptimization { get; set; } = true;

        /// <summary>
        /// Enable progressive JPEG
        /// </summary>
        public bool EnableProgressiveJpeg { get; set; } = false;

        /// <summary>
        /// Default background color for transparent images
        /// </summary>
        public string? DefaultBackgroundColor { get; set; } = "#FFFFFF";

        /// <summary>
        /// Enable color profile conversion
        /// </summary>
        public bool EnableColorProfileConversion { get; set; } = true;

        /// <summary>
        /// Target color space
        /// </summary>
        public string? TargetColorSpace { get; set; } = "sRGB";

        /// <summary>
        /// Maximum image memory in MB per operation
        /// </summary>
        public int MaxMemoryPerOperationMB { get; set; } = 512;
    }

    /// <summary>
    /// Security configuration
    /// </summary>
    public class SecurityConfig
    {
        /// <summary>
        /// Validate image before processing
        /// </summary>
        public bool ValidateImageBeforeProcessing { get; set; } = true;

        /// <summary>
        /// Enable CORS
        /// </summary>
        public bool EnableCors { get; set; } = true;

        /// <summary>
        /// Allowed referers (empty = all)
        /// </summary>
        public List<string> AllowedReferers { get; set; } = new();

        /// <summary>
        /// Rate limit per minute per IP
        /// </summary>
        public int RateLimitPerMinute { get; set; } = 60;

        /// <summary>
        /// Rate limit per hour per IP
        /// </summary>
        public int RateLimitPerHour { get; set; } = 1000;

        /// <summary>
        /// Log security events
        /// </summary>
        public bool LogSecurityEvents { get; set; } = true;

        /// <summary>
        /// Enforce HTTPS only
        /// </summary>
        public bool EnableHttpsOnly { get; set; } = false;

        /// <summary>
        /// Maximum requests per IP
        /// </summary>
        public int MaxRequestsPerIp { get; set; } = 100;

        /// <summary>
        /// Enable request validation
        /// </summary>
        public bool EnableRequestValidation { get; set; } = true;

        /// <summary>
        /// Block suspicious user agents
        /// </summary>
        public bool BlockSuspiciousUserAgents { get; set; } = true;

        /// <summary>
        /// Allowed user agents (empty = all)
        /// </summary>
        public List<string> AllowedUserAgents { get; set; } = new();

        /// <summary>
        /// Enable CSRF protection
        /// </summary>
        public bool EnableCsrfProtection { get; set; } = false;

        /// <summary>
        /// Enable SQL injection protection (for queries)
        /// </summary>
        public bool EnableSqlInjectionProtection { get; set; } = true;
    }

    /// <summary>
    /// Effects configuration
    /// </summary>
    public class EffectsConfig
    {
        /// <summary>
        /// Automatically apply effects based on query parameters
        /// </summary>
        public bool AutoApplyEffects { get; set; } = true;

        /// <summary>
        /// Order of effects to apply (comma-separated)
        /// </summary>
        public string EffectOrder { get; set; } = "Resize,Crop,Watermark";

        /// <summary>
        /// Enable custom effects
        /// </summary>
        public bool EnableCustomEffects { get; set; } = true;

        /// <summary>
        /// Enable effect result caching
        /// </summary>
        public bool EnableEffectCaching { get; set; } = true;

        /// <summary>
        /// Effect cache duration in minutes
        /// </summary>
        public int EffectCacheDurationMinutes { get; set; } = 5;

        /// <summary>
        /// Effect mappings
        /// </summary>
        public List<EffectMapping> Mappings { get; set; } = new();

        /// <summary>
        /// Maximum effects per request
        /// </summary>
        public int MaxEffectsPerRequest { get; set; } = 10;

        /// <summary>
        /// Enable effect validation
        /// </summary>
        public bool EnableEffectValidation { get; set; } = true;

        /// <summary>
        /// Enable effect composition (combine multiple effects)
        /// </summary>
        public bool EnableEffectComposition { get; set; } = true;
    }

    /// <summary>
    /// Effect mapping configuration
    /// </summary>
    public class EffectMapping
    {
        /// <summary>
        /// Query parameter name(s) that trigger this effect
        /// </summary>
        public string QueryParameter { get; set; } = string.Empty;

        /// <summary>
        /// Effect type name
        /// </summary>
        public string EffectType { get; set; } = string.Empty;

        /// <summary>
        /// Order of the effect
        /// </summary>
        public int Order { get; set; } = 999;

        /// <summary>
        /// Whether this effect is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Description of the effect
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Default parameter values
        /// </summary>
        public Dictionary<string, string> Defaults { get; set; } = new();

        /// <summary>
        /// Query parameter to effect property mappings
        /// </summary>
        public Dictionary<string, string> Mappings { get; set; } = new();

        /// <summary>
        /// Effect category
        /// </summary>
        public string? Category { get; set; }
    }

    /// <summary>
    /// Logging configuration
    /// </summary>
    public class LoggingConfig
    {
        /// <summary>
        /// Enable logging
        /// </summary>
        public bool EnableLogging { get; set; } = true;

        /// <summary>
        /// Log level
        /// </summary>
        public string LogLevel { get; set; } = "Information";

        /// <summary>
        /// Log image requests
        /// </summary>
        public bool LogImageRequests { get; set; } = true;

        /// <summary>
        /// Log image processing details
        /// </summary>
        public bool LogImageProcessing { get; set; } = true;

        /// <summary>
        /// Log security events
        /// </summary>
        public bool LogSecurityEvents { get; set; } = true;

        /// <summary>
        /// Log performance metrics
        /// </summary>
        public bool LogPerformanceMetrics { get; set; } = false;

        /// <summary>
        /// Log file path
        /// </summary>
        public string? LogFilePath { get; set; }

        /// <summary>
        /// Enable structured logging (JSON)
        /// </summary>
        public bool StructuredLogging { get; set; } = false;

        /// <summary>
        /// Log cache hits
        /// </summary>
        public bool LogCacheHits { get; set; } = true;

        /// <summary>
        /// Log cache misses
        /// </summary>
        public bool LogCacheMisses { get; set; } = true;

        /// <summary>
        /// Maximum log file size in MB
        /// </summary>
        public int MaxLogFileSizeMB { get; set; } = 10;

        /// <summary>
        /// Maximum log files to keep
        /// </summary>
        public int MaxLogFiles { get; set; } = 5;

        /// <summary>
        /// Additional log fields
        /// </summary>
        public Dictionary<string, string> AdditionalFields { get; set; } = new();
    }

    /// <summary>
    /// Performance configuration
    /// </summary>
    public class PerformanceConfig
    {
        /// <summary>
        /// Enable performance monitoring
        /// </summary>
        public bool EnablePerformanceMonitoring { get; set; } = false;

        /// <summary>
        /// Enable request timing
        /// </summary>
        public bool EnableRequestTiming { get; set; } = true;

        /// <summary>
        /// Enable memory monitoring
        /// </summary>
        public bool EnableMemoryMonitoring { get; set; } = false;

        /// <summary>
        /// Slow request threshold in milliseconds
        /// </summary>
        public int SlowRequestThresholdMs { get; set; } = 5000;

        /// <summary>
        /// Enable response compression
        /// </summary>
        public bool EnableResponseCompression { get; set; } = true;

        /// <summary>
        /// Enable connection pooling
        /// </summary>
        public bool EnableConnectionPooling { get; set; } = true;

        /// <summary>
        /// Maximum connection pool size
        /// </summary>
        public int MaxConnectionPoolSize { get; set; } = 100;

        /// <summary>
        /// Connection idle timeout in seconds
        /// </summary>
        public int ConnectionIdleTimeoutSeconds { get; set; } = 60;

        /// <summary>
        /// Enable keep-alive for connections
        /// </summary>
        public bool EnableKeepAlive { get; set; } = true;
    }

    /// <summary>
    /// Feature flags configuration
    /// </summary>
    public class FeatureConfig
    {
        /// <summary>
        /// Enable experimental features
        /// </summary>
        public bool EnableExperimentalFeatures { get; set; } = false;

        /// <summary>
        /// Enable beta features
        /// </summary>
        public bool EnableBetaFeatures { get; set; } = false;

        /// <summary>
        /// Enable deprecated features
        /// </summary>
        public bool EnableDeprecatedFeatures { get; set; } = false;

        /// <summary>
        /// Enable WebP encoding
        /// </summary>
        public bool EnableWebP { get; set; } = true;

        /// <summary>
        /// Enable AVIF encoding
        /// </summary>
        public bool EnableAVIF { get; set; } = true;

        /// <summary>
        /// Enable HEIC encoding
        /// </summary>
        public bool EnableHEIC { get; set; } = false;

        /// <summary>
        /// Enable image filters
        /// </summary>
        public bool EnableImageFilters { get; set; } = true;

        /// <summary>
        /// Enable image watermarks
        /// </summary>
        public bool EnableWatermarks { get; set; } = true;

        /// <summary>
        /// Enable image transformations
        /// </summary>
        public bool EnableTransformations { get; set; } = true;

        /// <summary>
        /// Enable batch processing
        /// </summary>
        public bool EnableBatchProcessing { get; set; } = true;

        /// <summary>
        /// Enable webhooks for processing events
        /// </summary>
        public bool EnableWebhooks { get; set; } = false;

        /// <summary>
        /// Enable analytics
        /// </summary>
        public bool EnableAnalytics { get; set; } = false;
    }

    /// <summary>
    /// Environment-based configuration for Raphael image processing
    /// </summary>
    public class RaphaelEnvironmentConfig
    {
        /// <summary>
        /// Dictionary of environment configurations
        /// </summary>
        public Dictionary<string, EnvironmentConfig> Environments { get; set; } = new();

        /// <summary>
        /// Current active environment name
        /// </summary>
        public string CurrentEnvironment { get; set; } = "Development";

        /// <summary>
        /// Enable auto-registration of loaders on startup
        /// </summary>
        public bool EnableAutoRegistration { get; set; } = true;

        /// <summary>
        /// Default environment configuration (fallback)
        /// </summary>
        public EnvironmentConfig? Default { get; set; }

        /// <summary>
        /// Gets the configuration for the current environment
        /// </summary>
        [JsonIgnore]
        public EnvironmentConfig Current => GetEnvironment(CurrentEnvironment);

        /// <summary>
        /// Gets a specific environment configuration
        /// </summary>
        public EnvironmentConfig GetEnvironment(string name)
        {
            if (Environments.TryGetValue(name, out var config))
                return config;

            // Try case-insensitive match
            var match = Environments.Keys.FirstOrDefault(k =>
                k.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (match != null)
                return Environments[match];

            // Return default or create new
            if (Default != null)
                return Default;

            // Create default configuration
            var defaultConfig = CreateDefaultEnvironmentConfig();
            Environments[name] = defaultConfig;
            return defaultConfig;
        }

        /// <summary>
        /// Checks if an environment exists
        /// </summary>
        public bool HasEnvironment(string name)
        {
            return Environments.ContainsKey(name) ||
                   Environments.Keys.Any(k => k.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Adds or updates an environment configuration
        /// </summary>
        public void AddOrUpdateEnvironment(string name, EnvironmentConfig config)
        {
            Environments[name] = config;
        }

        /// <summary>
        /// Creates a default environment configuration
        /// </summary>
        private EnvironmentConfig CreateDefaultEnvironmentConfig()
        {
            return new EnvironmentConfig
            {
                Loaders = new LoaderConfig
                {
                    Http = new HttpLoaderConfig
                    {
                        MaxFileSizeMB = 10,
                        MaxWidth = 8192,
                        MaxHeight = 8192,
                        MaxPixels = 67108864,
                        TimeoutSeconds = 30,
                        AllowAllDomains = true,
                        AllowedDomains = new List<string>(),
                        ValidateContentType = true,
                        ValidateDimensions = true,
                        BlockInternalAddresses = true,
                        UserAgent = "RaphaelImageProcessor/1.0",
                        EnableCompression = true
                    },
                    Local = new LocalLoaderConfig
                    {
                        Directories = new List<string>
                        {
                            "images",
                            "uploads",
                            "wwwroot/images"
                        },
                        Recursive = true,
                        ValidateExtensions = true,
                        AllowedExtensions = new List<string>
                        {
                            ".jpg", ".jpeg", ".png", ".gif", ".bmp",
                            ".webp", ".tiff", ".tif", ".ico", ".avif"
                        },
                        BlockSystemFiles = true
                    },
                    CustomLoaders = new List<CustomLoaderConfig>()
                },
                Processing = new ProcessingConfig
                {
                    DefaultQuality = 75,
                    DefaultFormat = "Jpeg",
                    StripMetadata = true,
                    AllowAnimatedImages = true,
                    MaxAnimationFrames = 100,
                    EnableParallelProcessing = true,
                    MaxDegreeOfParallelism = 4,
                    DefaultResizeQuality = ResizeQuality.Smooth
                },
                Security = new SecurityConfig
                {
                    ValidateImageBeforeProcessing = true,
                    EnableCors = true,
                    AllowedReferers = new List<string>(),
                    RateLimitPerMinute = 60,
                    RateLimitPerHour = 1000,
                    LogSecurityEvents = true,
                    EnableHttpsOnly = false,
                    MaxRequestsPerIp = 100,
                    EnableRequestValidation = true
                },
                Cache = new CacheConfig
                {
                    EnableMemoryCache = true,
                    EnableFileCache = true,
                    FileCacheDirectory = null,
                    ProcessedCacheDirectory = "Processed",
                    CacheDurationMinutes = 10,
                    MemoryCacheSizeLimit = 100,
                    FileCacheMaxAgeHours = 168,
                    EnableFileCacheCleanup = true,
                    EnableRedisCache = false,
                    RedisConnectionString = null,
                    RedisCacheDurationMinutes = 60
                },
                Logging = new LoggingConfig
                {
                    EnableLogging = true,
                    LogLevel = "Information",
                    LogImageRequests = true,
                    LogImageProcessing = true,
                    LogSecurityEvents = true,
                    LogPerformanceMetrics = false,
                    LogFilePath = null,
                    StructuredLogging = false,
                    LogCacheHits = true,
                    LogCacheMisses = true
                },
                Effects = new EffectsConfig
                {
                    AutoApplyEffects = true,
                    EffectOrder = "Resize,Crop,Watermark",
                    EnableCustomEffects = true,
                    EnableEffectCaching = true,
                    EffectCacheDurationMinutes = 5,
                    Mappings = new List<EffectMapping>
                    {
                        new EffectMapping
                        {
                            EffectType = "Resize",
                            Order = 1,
                            Enabled = true,
                            Description = "Resize image to specified dimensions",
                            Defaults = new Dictionary<string, string>
                            {
                                ["Width"] = "0",
                                ["Height"] = "0",
                                ["PreserveAspectRatio"] = "true"
                            },
                            Mappings = new Dictionary<string, string>
                            {
                                ["Width"] = "Width",
                                ["Height"] = "Height",
                                ["PreserveAspectRatio"] = "PreserveAspectRatio"
                            }
                        },
                        new EffectMapping
                        {
                            EffectType = "Crop",
                            Order = 2,
                            Enabled = true,
                            Description = "Crop image to specified area",
                            Defaults = new Dictionary<string, string>
                            {
                                ["X"] = "0",
                                ["Y"] = "0",
                                ["Width"] = "0",
                                ["Height"] = "0",
                                ["UseAnchor"] = "false",
                                ["Anchor"] = "Center"
                            },
                            Mappings = new Dictionary<string, string>
                            {
                                ["CropX"] = "X",
                                ["CropY"] = "Y",
                                ["CropWidth"] = "Width",
                                ["CropHeight"] = "Height",
                                ["UseAnchor"] = "UseAnchor",
                                ["Anchor"] = "Anchor"
                            }
                        },
                        new EffectMapping
                        {
                            EffectType = "Watermark",
                            Order = 3,
                            Enabled = true,
                            Description = "Add text or image watermark",
                            Defaults = new Dictionary<string, string>
                            {
                                ["Type"] = "Text",
                                ["Position"] = "BottomRight",
                                ["Opacity"] = "0.7",
                                ["Text"] = "© Watermark",
                                ["FontSize"] = "48",
                                ["TextShadow"] = "true",
                                ["Scale"] = "0.25",
                                ["PreserveImageAspectRatio"] = "true"
                            },
                            Mappings = new Dictionary<string, string>
                            {
                                ["WatermarkText"] = "Text",
                                ["WatermarkImageUrl"] = "WatermarkImageUrl",
                                ["WatermarkPosition"] = "Position",
                                ["WatermarkOpacity"] = "Opacity",
                                ["WatermarkFontSize"] = "FontSize",
                                ["WatermarkShadow"] = "TextShadow",
                                ["WatermarkScale"] = "Scale",
                                ["WatermarkPreserveRatio"] = "PreserveImageAspectRatio"
                            }
                        }
                    }
                }
            };
        }
    }

    /// <summary>
    /// Environment-specific configuration
    /// </summary>
    public class EnvironmentConfig
    {
        public LoaderConfig Loaders { get; set; } = new();
        public ProcessingConfig Processing { get; set; } = new();
        public SecurityConfig Security { get; set; } = new();
        public CacheConfig Cache { get; set; } = new();
        public LoggingConfig Logging { get; set; } = new();
        public EffectsConfig Effects { get; set; } = new();

        /// <summary>
        /// Inherit from another environment
        /// </summary>
        public string? InheritsFrom { get; set; }

        /// <summary>
        /// Environment description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Environment-specific tags
        /// </summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// Merge this config with a parent config
        /// </summary>
        public EnvironmentConfig Merge(EnvironmentConfig parent)
        {
            return new EnvironmentConfig
            {
                Loaders = MergeLoaders(parent.Loaders, Loaders),
                Processing = MergeProcessing(parent.Processing, Processing),
                Security = MergeSecurity(parent.Security, Security),
                Cache = MergeCache(parent.Cache, Cache),
                Logging = MergeLogging(parent.Logging, Logging),
                Effects = MergeEffects(parent.Effects, Effects),
                InheritsFrom = parent.InheritsFrom ?? InheritsFrom,
                Description = Description ?? parent.Description,
                Tags = Tags.Any() ? Tags : parent.Tags
            };
        }

        private LoaderConfig MergeLoaders(LoaderConfig parent, LoaderConfig child)
        {
            return new LoaderConfig
            {
                Http = MergeHttpLoader(parent.Http, child.Http),
                Local = MergeLocalLoader(parent.Local, child.Local),
                CustomLoaders = child.CustomLoaders.Any() ? child.CustomLoaders : parent.CustomLoaders
            };
        }

        private HttpLoaderConfig MergeHttpLoader(HttpLoaderConfig parent, HttpLoaderConfig child)
        {
            return new HttpLoaderConfig
            {
                MaxFileSizeMB = child.MaxFileSizeMB > 0 ? child.MaxFileSizeMB : parent.MaxFileSizeMB,
                MaxWidth = child.MaxWidth > 0 ? child.MaxWidth : parent.MaxWidth,
                MaxHeight = child.MaxHeight > 0 ? child.MaxHeight : parent.MaxHeight,
                MaxPixels = child.MaxPixels > 0 ? child.MaxPixels : parent.MaxPixels,
                TimeoutSeconds = child.TimeoutSeconds > 0 ? child.TimeoutSeconds : parent.TimeoutSeconds,
                AllowedDomains = child.AllowedDomains.Any() ? child.AllowedDomains : parent.AllowedDomains,
                AllowAllDomains = child.AllowAllDomains,
                ValidateContentType = child.ValidateContentType,
                ValidateDimensions = child.ValidateDimensions,
                BlockInternalAddresses = child.BlockInternalAddresses,
                UserAgent = !string.IsNullOrEmpty(child.UserAgent) ? child.UserAgent : parent.UserAgent,
                EnableCompression = child.EnableCompression
            };
        }

        private LocalLoaderConfig MergeLocalLoader(LocalLoaderConfig parent, LocalLoaderConfig child)
        {
            return new LocalLoaderConfig
            {
                Directories = child.Directories.Any() ? child.Directories : parent.Directories,
                Recursive = child.Recursive,
                ValidateExtensions = child.ValidateExtensions,
                AllowedExtensions = child.AllowedExtensions.Any() ? child.AllowedExtensions : parent.AllowedExtensions,
                BlockSystemFiles = child.BlockSystemFiles
            };
        }

        private ProcessingConfig MergeProcessing(ProcessingConfig parent, ProcessingConfig child)
        {
            return new ProcessingConfig
            {
                DefaultQuality = child.DefaultQuality > 0 ? child.DefaultQuality : parent.DefaultQuality,
                DefaultFormat = !string.IsNullOrEmpty(child.DefaultFormat) ? child.DefaultFormat : parent.DefaultFormat,
                StripMetadata = child.StripMetadata,
                AllowAnimatedImages = child.AllowAnimatedImages,
                MaxAnimationFrames = child.MaxAnimationFrames > 0 ? child.MaxAnimationFrames : parent.MaxAnimationFrames,
                EnableParallelProcessing = child.EnableParallelProcessing,
                MaxDegreeOfParallelism = child.MaxDegreeOfParallelism > 0 ? child.MaxDegreeOfParallelism : parent.MaxDegreeOfParallelism,
                DefaultResizeQuality = child.DefaultResizeQuality
            };
        }

        private SecurityConfig MergeSecurity(SecurityConfig parent, SecurityConfig child)
        {
            return new SecurityConfig
            {
                ValidateImageBeforeProcessing = child.ValidateImageBeforeProcessing,
                EnableCors = child.EnableCors,
                AllowedReferers = child.AllowedReferers.Any() ? child.AllowedReferers : parent.AllowedReferers,
                RateLimitPerMinute = child.RateLimitPerMinute > 0 ? child.RateLimitPerMinute : parent.RateLimitPerMinute,
                RateLimitPerHour = child.RateLimitPerHour > 0 ? child.RateLimitPerHour : parent.RateLimitPerHour,
                LogSecurityEvents = child.LogSecurityEvents,
                EnableHttpsOnly = child.EnableHttpsOnly,
                MaxRequestsPerIp = child.MaxRequestsPerIp > 0 ? child.MaxRequestsPerIp : parent.MaxRequestsPerIp,
                EnableRequestValidation = child.EnableRequestValidation
            };
        }

        private CacheConfig MergeCache(CacheConfig parent, CacheConfig child)
        {
            return new CacheConfig
            {
                EnableMemoryCache = child.EnableMemoryCache,
                EnableFileCache = child.EnableFileCache,
                FileCacheDirectory = !string.IsNullOrEmpty(child.FileCacheDirectory) ? child.FileCacheDirectory : parent.FileCacheDirectory,
                ProcessedCacheDirectory = !string.IsNullOrEmpty(child.ProcessedCacheDirectory) ? child.ProcessedCacheDirectory : parent.ProcessedCacheDirectory,
                CacheDurationMinutes = child.CacheDurationMinutes > 0 ? child.CacheDurationMinutes : parent.CacheDurationMinutes,
                MemoryCacheSizeLimit = child.MemoryCacheSizeLimit > 0 ? child.MemoryCacheSizeLimit : parent.MemoryCacheSizeLimit,
                FileCacheMaxAgeHours = child.FileCacheMaxAgeHours > 0 ? child.FileCacheMaxAgeHours : parent.FileCacheMaxAgeHours,
                EnableFileCacheCleanup = child.EnableFileCacheCleanup,
                EnableRedisCache = child.EnableRedisCache,
                RedisConnectionString = !string.IsNullOrEmpty(child.RedisConnectionString) ? child.RedisConnectionString : parent.RedisConnectionString,
                RedisCacheDurationMinutes = child.RedisCacheDurationMinutes > 0 ? child.RedisCacheDurationMinutes : parent.RedisCacheDurationMinutes
            };
        }

        private LoggingConfig MergeLogging(LoggingConfig parent, LoggingConfig child)
        {
            return new LoggingConfig
            {
                EnableLogging = child.EnableLogging,
                LogLevel = !string.IsNullOrEmpty(child.LogLevel) ? child.LogLevel : parent.LogLevel,
                LogImageRequests = child.LogImageRequests,
                LogImageProcessing = child.LogImageProcessing,
                LogSecurityEvents = child.LogSecurityEvents,
                LogPerformanceMetrics = child.LogPerformanceMetrics,
                LogFilePath = !string.IsNullOrEmpty(child.LogFilePath) ? child.LogFilePath : parent.LogFilePath,
                StructuredLogging = child.StructuredLogging,
                LogCacheHits = child.LogCacheHits,
                LogCacheMisses = child.LogCacheMisses
            };
        }

        private EffectsConfig MergeEffects(EffectsConfig parent, EffectsConfig child)
        {
            return new EffectsConfig
            {
                AutoApplyEffects = child.AutoApplyEffects,
                EffectOrder = !string.IsNullOrEmpty(child.EffectOrder) ? child.EffectOrder : parent.EffectOrder,
                EnableCustomEffects = child.EnableCustomEffects,
                EnableEffectCaching = child.EnableEffectCaching,
                EffectCacheDurationMinutes = child.EffectCacheDurationMinutes > 0 ? child.EffectCacheDurationMinutes : parent.EffectCacheDurationMinutes,
                Mappings = child.Mappings.Any() ? child.Mappings : parent.Mappings
            };
        }
    }
}