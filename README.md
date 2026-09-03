# Raphael

<p align="center">
  <img
    width="256"
    height="256"
    alt="Raphael"
    src="https://github.com/user-attachments/assets/fcf6da03-6d00-4d1c-bc8a-5b7e7d7f788a"
  />
</p>

**Raphael** is a lightweight real-time image processing pipeline for ASP.NET Core, built with .NET and SkiaSharp.

It provides a simple HTTP endpoint for resizing, cropping, watermarking, transforming, and processing images through query parameters.

## Usage

Register Raphael in your application:

```csharp
builder.Services.AddRaphael(builder.Configuration);

var app = builder.Build();

app.AddRaphaelRoutes();
```

Images can then be processed through the `/_raphael` endpoint:

```text
/_raphael?url=https://example.com/image.jpg&width=800
```

Multiple processing options can be combined:

```text
/_raphael?url=https://example.com/image.jpg&width=800&height=600
```

Control resize sampling quality per-request with `resizeQuality` (`Fast`, `Smooth`, or `High`):

```text
/_raphael?url=https://example.com/image.jpg&width=800&resizeQuality=High
```

`Fast` is nearest-neighbor (cheapest, blocky edges), `Smooth` is bilinear (balanced, smooth edges), and `High` is cubic (Mitchell) resampling (slowest, sharpest/smoothest). When omitted, the request falls back to `Processing.DefaultResizeQuality`.

The processing pipeline discovers available effects automatically and applies them according to the configured effect order.

## Effects

Raphael currently includes:

* **Resize** - Scale images to specific dimensions
* **Crop** - Crop images to a specific area
* **Color Matrix** - Apply color transformations
* **Watermark** - Add text or image watermarks

Effects are extensible through the `IEffect` interface and can be discovered automatically using the `Effect` attribute.

## Configuration

Raphael uses a hierarchical configuration system with environment-specific settings. Configuration is loaded from `appsettings.json` and supports environment-specific overrides via `appsettings.{Environment}.json`.

### Configuration Structure

```json
{
  "Raphael": {
    "CurrentEnvironment": "Development",
    "EnableAutoRegistration": true,
    "Environments": {
      "Development": { ... },
      "Staging": { ... },
      "Production": { ... }
    }
  }
}
```

### Core Settings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `CurrentEnvironment` | string | `"Development"` | Active environment name |
| `EnableAutoRegistration` | bool | `true` | Auto-discover and register loaders/effects |

### Environment Inheritance

Environments can inherit from a parent environment using the `InheritsFrom` property. Child environments override only the settings they specify.

```json
"Staging": {
  "InheritsFrom": "Development",
  "Security": {
    "RateLimitPerMinute": 100
  }
}
```

---

### Loaders Configuration

Loaders handle fetching images from various sources.

#### HTTP Loader (`Loaders.Http`)

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `MaxFileSizeMB` | int | `10` | Maximum download size in MB |
| `MaxWidth` | int | `8192` | Maximum image width |
| `MaxHeight` | int | `8192` | Maximum image height |
| `MaxPixels` | int | `67108864` | Maximum total pixels (width × height) |
| `TimeoutSeconds` | int | `30` | HTTP request timeout |
| `AllowAllDomains` | bool | `true` | Allow all domains (if true, ignores `AllowedDomains`) |
| `AllowedDomains` | string[] | `[]` | List of allowed domains (wildcards supported) |
| `ValidateContentType` | bool | `true` | Require `image/*` content type |
| `ValidateDimensions` | bool | `true` | Validate image dimensions against limits |
| `BlockInternalAddresses` | bool | `true` | Block localhost/private IPs (SSRF protection) |
| `UserAgent` | string | `"RaphaelImageProcessor/1.0"` | HTTP User-Agent header |
| `EnableCompression` | bool | `true` | Accept gzip/deflate responses |
| `MaxRedirects` | int | `3` | Maximum redirects to follow |
| `Headers` | object | `{}` | Custom headers to include in requests |
| `Proxy` | object | `null` | Proxy configuration |

#### Local File Loader (`Loaders.Local`)

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `Directories` | string[] | `["images", "uploads", "wwwroot/images"]` | Root directories to search |
| `Recursive` | bool | `true` | Search subdirectories |
| `ValidateExtensions` | bool | `true` | Only allow configured extensions |
| `AllowedExtensions` | string[] | `[.jpg, .png, .gif, ...]` | Permitted file extensions |
| `BlockSystemFiles` | bool | `true` | Block access to system files |
| `MaxFileSizeMB` | int | `50` | Maximum local file size |
| `EnableFileWatching` | bool | `false` | Watch for file changes |

#### Custom Loaders (`Loaders.CustomLoaders`)

```json
"CustomLoaders": [
  {
    "Name": "S3Loader",
    "Type": "MyApp.Loaders.S3ImageLoader",
    "Assembly": "MyApp",
    "Enabled": true,
    "Priority": 10,
    "Parameters": {
      "Bucket": "my-images",
      "Region": "us-east-1"
    }
  }
]
```

---

### Processing Configuration (`Processing`)

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `DefaultQuality` | int | `75` | Default JPEG/WebP quality (1-100) |
| `DefaultFormat` | string | `"Jpeg"` | Output format (Jpeg, Png, WebP, Avif) |
| `StripMetadata` | bool | `true` | Remove EXIF/ICC profiles |
| `AllowAnimatedImages` | bool | `true` | Process GIF/WebP/APNG animations |
| `MaxAnimationFrames` | int | `100` | Maximum frames to process |
| `EnableParallelProcessing` | bool | `true` | Use parallelism for batch ops |
| `MaxDegreeOfParallelism` | int | `4` | Max concurrent operations |
| `DefaultResizeQuality` | string | `"Smooth"` | Default resize sampling quality (`Fast`, `Smooth`, `High`) when a request doesn't specify `resizeQuality` |
| `EnableOptimization` | bool | `true` | Enable encoder optimizations |
| `EnableProgressiveJpeg` | bool | `false` | Progressive JPEG encoding |
| `DefaultBackgroundColor` | string | `"#FFFFFF"` | Background for transparent images |
| `EnableColorProfileConversion` | bool | `true` | Convert to target color space |
| `TargetColorSpace` | string | `"sRGB"` | Target ICC profile |
| `MaxMemoryPerOperationMB` | int | `512` | Memory limit per operation |

---

### Security Configuration (`Security`)

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `ValidateImageBeforeProcessing` | bool | `true` | Verify image validity first |
| `EnableCors` | bool | `true` | Enable CORS headers |
| `AllowedReferers` | string[] | `[]` | Allowed Referer headers (empty = all) |
| `RateLimitPerMinute` | int | `60` | Requests per minute per IP |
| `RateLimitPerHour` | int | `1000` | Requests per hour per IP |
| `LogSecurityEvents` | bool | `true` | Log blocked/suspicious requests |
| `EnableHttpsOnly` | bool | `false` | Require HTTPS for image URLs |
| `MaxRequestsPerIp` | int | `100` | Max concurrent requests per IP |
| `EnableRequestValidation` | bool | `true` | Validate query parameters |
| `BlockSuspiciousUserAgents` | bool | `true` | Block known bad user agents |
| `AllowedUserAgents` | string[] | `[]` | Allowlist for user agents |
| `EnableCsrfProtection` | bool | `false` | Require CSRF tokens |
| `EnableSqlInjectionProtection` | bool | `true` | Sanitize query parameters |

---

### Cache Configuration (`Cache`)

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `EnableMemoryCache` | bool | `true` | Use in-memory cache |
| `EnableFileCache` | bool | `true` | Use disk cache |
| `FileCacheDirectory` | string | `null` | Cache directory (null = temp) |
| `ProcessedCacheDirectory` | string | `"Processed"` | Subdirectory for processed images |
| `CacheDurationMinutes` | int | `10` | Default cache TTL |
| `MemoryCacheSizeLimit` | int | `100` | Max items in memory cache |
| `FileCacheMaxAgeHours` | int | `168` | Max age for file cache entries |
| `EnableFileCacheCleanup` | bool | `true` | Auto-clean expired entries |
| `EnableRedisCache` | bool | `false` | Use Redis distributed cache |
| `RedisConnectionString` | string | `null` | Redis connection string |
| `RedisCacheDurationMinutes` | int | `60` | Redis cache TTL |
| `EnableCacheCompression` | bool | `false` | Compress cached data |
| `CacheKeyPrefix` | string | `"raphael:"` | Prefix for all cache keys |
| `EnableCacheStatistics` | bool | `false` | Track hit/miss ratios |

---

### Logging Configuration (`Logging`)

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `EnableLogging` | bool | `true` | Enable Raphael logging |
| `LogLevel` | string | `"Information"` | Minimum log level |
| `LogImageRequests` | bool | `true` | Log incoming requests |
| `LogImageProcessing` | bool | `true` | Log processing steps |
| `LogSecurityEvents` | bool | `true` | Log security violations |
| `LogPerformanceMetrics` | bool | `false` | Log timing/memory stats |
| `LogFilePath` | string | `null` | Log file path (null = console) |
| `StructuredLogging` | bool | `false` | Output JSON logs |
| `LogCacheHits` | bool | `true` | Log cache hits |
| `LogCacheMisses` | bool | `true` | Log cache misses |
| `MaxLogFileSizeMB` | int | `10` | Max log file size |
| `MaxLogFiles` | int | `5` | Number of rotated logs |

---

### Effects Configuration (`Effects`)

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `AutoApplyEffects` | bool | `true` | Apply effects from query params |
| `EffectOrder` | string | `"Resize,Crop,Watermark"` | Comma-separated effect execution order |
| `EnableCustomEffects` | bool | `true` | Allow custom IEffect implementations |
| `EnableEffectCaching` | bool | `true` | Cache effect results |
| `EffectCacheDurationMinutes` | int | `5` | Effect cache TTL |
| `MaxEffectsPerRequest` | int | `10` | Max effects per request |
| `EnableEffectValidation` | bool | `true` | Validate effect parameters |
| `EnableEffectComposition` | bool | `true` | Combine multiple effects |

#### Effect Mappings (`Effects.Mappings`)

Map query parameters to effect properties:

```json
"Effects": {
  "Mappings": [
    {
      "QueryParameter": "width,height",
      "EffectType": "Resize",
      "Order": 1,
      "Enabled": true,
      "Description": "Resize image",
      "Defaults": {
        "Width": "0",
        "Height": "0",
        "PreserveAspectRatio": "true"
      },
      "Mappings": {
        "Width": "Width",
        "Height": "Height",
        "PreserveAspectRatio": "PreserveAspectRatio"
      }
    }
  ]
}
```

---

### Performance Configuration (`Performance`)

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `EnablePerformanceMonitoring` | bool | `false` | Enable internal metrics |
| `EnableRequestTiming` | bool | `true` | Track request duration |
| `EnableMemoryMonitoring` | bool | `false` | Track memory usage |
| `SlowRequestThresholdMs` | int | `5000` | Threshold for slow request warnings |
| `EnableResponseCompression` | bool | `true` | Compress HTTP responses |
| `EnableConnectionPooling` | bool | `true` | Reuse HTTP connections |
| `MaxConnectionPoolSize` | int | `100` | Max pooled connections |
| `ConnectionIdleTimeoutSeconds` | int | `60` | Idle connection timeout |
| `EnableKeepAlive` | bool | `true` | HTTP keep-alive |

---

### Feature Flags (`Features`)

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `EnableExperimentalFeatures` | bool | `false` | Unstable features |
| `EnableBetaFeatures` | bool | `false` | Beta features |
| `EnableDeprecatedFeatures` | bool | `false` | Deprecated features |
| `EnableWebP` | bool | `true` | WebP encoding support |
| `EnableAVIF` | bool | `true` | AVIF encoding support |
| `EnableHEIC` | bool | `false` | HEIC encoding support |
| `EnableImageFilters` | bool | `true` | Filter effects (blur, sharpen, etc.) |
| `EnableWatermarks` | bool | `true` | Watermark support |
| `EnableTransformations` | bool | `true` | Transform effects (rotate, flip) |
| `EnableBatchProcessing` | bool | `true` | Batch operations |
| `EnableWebhooks` | bool | `false` | Webhook notifications |
| `EnableAnalytics` | bool | `false` | Usage analytics |

---

## Complete Example: Production Configuration

```json
{
  "Raphael": {
    "CurrentEnvironment": "Production",
    "EnableAutoRegistration": true,
    "Environments": {
      "Production": {
        "Description": "Production environment with security and performance optimizations",
        "Tags": ["production", "live"],
        "Loaders": {
          "Http": {
            "MaxFileSizeMB": 20,
            "MaxWidth": 4096,
            "MaxHeight": 4096,
            "MaxPixels": 16777216,
            "TimeoutSeconds": 15,
            "AllowAllDomains": false,
            "AllowedDomains": [
              "example.com",
              "*.cloudfront.net",
              "cdn.example.com"
            ],
            "BlockInternalAddresses": true,
            "EnableCompression": true
          },
          "Local": {
            "Directories": [
              "/var/www/images",
              "/var/data/uploads"
            ],
            "Recursive": true,
            "BlockSystemFiles": true
          }
        },
        "Processing": {
          "DefaultQuality": 85,
          "DefaultFormat": "WebP",
          "StripMetadata": true,
          "AllowAnimatedImages": true,
          "MaxAnimationFrames": 50,
          "DefaultResizeQuality": "High"
        },
        "Security": {
          "ValidateImageBeforeProcessing": true,
          "EnableCors": true,
          "RateLimitPerMinute": 30,
          "RateLimitPerHour": 500,
          "LogSecurityEvents": true,
          "EnableHttpsOnly": true,
          "MaxRequestsPerIp": 50,
          "EnableRequestValidation": true
        },
        "Cache": {
          "EnableMemoryCache": true,
          "EnableFileCache": true,
          "FileCacheDirectory": "/var/cache/raphael",
          "CacheDurationMinutes": 30,
          "MemoryCacheSizeLimit": 500,
          "FileCacheMaxAgeHours": 720,
          "EnableFileCacheCleanup": true,
          "EnableRedisCache": true,
          "RedisConnectionString": "localhost:6379,password=securepass,ssl=True",
          "RedisCacheDurationMinutes": 60
        },
        "Logging": {
          "EnableLogging": true,
          "LogLevel": "Warning",
          "LogImageRequests": true,
          "LogImageProcessing": true,
          "LogSecurityEvents": true,
          "LogPerformanceMetrics": false,
          "LogFilePath": "/var/log/raphael.log",
          "StructuredLogging": true,
          "LogCacheHits": false,
          "LogCacheMisses": true
        },
        "Effects": {
          "AutoApplyEffects": true,
          "EffectOrder": "Resize,Crop,Watermark",
          "EnableCustomEffects": true,
          "EnableEffectCaching": true,
          "EffectCacheDurationMinutes": 10
        }
      }
    }
  }
}
```

## Environment Variable Override

Set the active environment at runtime:

```bash
export RAPHAEL_ENV=Production
```

## Why Raphael?

Raphael is designed to make image processing simple to integrate into web applications. Instead of creating individual image processing endpoints, applications can use a single configurable pipeline driven by URL parameters.

## Built With

* .NET
* ASP.NET Core
* SkiaSharp

## License

MIT