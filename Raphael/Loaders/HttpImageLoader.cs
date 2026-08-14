using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using SkiaSharp;

namespace Raphael.Loaders
{
    /// <summary>
    /// Secure HTTP image loader with SKCodec-based validation
    /// </summary>
    public class HttpImageLoader : IImageLoader
    {
        private readonly HttpClient _httpClient;
        private readonly int _maxFileSize;
        private readonly int _maxWidth;
        private readonly int _maxHeight;
        private readonly long _maxPixels;
        private readonly TimeSpan _timeout;
        private readonly List<string> _allowedDomains;
        private readonly bool _validateContentType;
        private readonly bool _validateDimensions;
        private readonly bool _blockInternalAddresses;

        public HttpImageLoader() : this(new HttpClient())
        {
        }

        public HttpImageLoader(HttpClient httpClient, HttpLoaderOptions? options = null)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

            options ??= new HttpLoaderOptions();

            _maxFileSize = options.MaxFileSize ?? 50 * 1024 * 1024; // Default 50MB
            _maxWidth = options.MaxWidth ?? 16384;
            _maxHeight = options.MaxHeight ?? 16384;
            _maxPixels = options.MaxPixels ?? (long)_maxWidth * _maxHeight;
            _timeout = options.Timeout ?? TimeSpan.FromSeconds(30);
            _allowedDomains = options.AllowedDomains ?? new List<string>();
            _validateContentType = options.ValidateContentType ?? true;
            _validateDimensions = options.ValidateDimensions ?? true;
            _blockInternalAddresses = options.BlockInternalAddresses ?? true;

            // Configure HttpClient
            _httpClient.Timeout = _timeout;
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("RaphaelImageProcessor/1.0");
            _httpClient.DefaultRequestHeaders.Accept.ParseAdd("image/*");
            _httpClient.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate");
        }

        public bool CanLoad(string source)
        {
            return !string.IsNullOrEmpty(source) &&
                   (source.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    source.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
        }

        public async Task<byte[]> LoadAsync(string source)
        {
            if (string.IsNullOrEmpty(source))
                throw new ArgumentException("Source cannot be null or empty", nameof(source));

            // Validate URL
            if (!Uri.IsWellFormedUriString(source, UriKind.Absolute))
                throw new SecurityException($"Invalid URL format: {source}");

            var uri = new Uri(source);

            // Validate domain if restrictions are set
            if (_allowedDomains.Count > 0)
            {
                if (!IsDomainAllowed(uri.Host))
                {
                    throw new SecurityException($"Domain not allowed: {uri.Host}");
                }
            }

            // Prevent localhost/internal IP access
            if (_blockInternalAddresses && IsInternalOrLocalAddress(uri))
            {
                throw new SecurityException($"Access to internal/local addresses is not allowed: {uri.Host}");
            }

            try
            {
                // Send request with timeout
                using var cts = new System.Threading.CancellationTokenSource(_timeout);
                var response = await _httpClient.GetAsync(source, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                response.EnsureSuccessStatusCode();

                // Validate content type
                if (_validateContentType)
                {
                    var contentType = response.Content.Headers.ContentType?.MediaType;
                    if (!IsValidImageContentType(contentType))
                    {
                        throw new SecurityException($"Invalid content type: {contentType}");
                    }
                }

                // Check content length before downloading
                if (response.Content.Headers.ContentLength.HasValue)
                {
                    if (response.Content.Headers.ContentLength.Value > _maxFileSize)
                    {
                        throw new SecurityException($"File too large: {response.Content.Headers.ContentLength.Value} bytes (max: {_maxFileSize} bytes)");
                    }
                }

                // Download image data
                var data = await response.Content.ReadAsByteArrayAsync(cts.Token);

                if (data == null || data.Length == 0)
                    throw new SecurityException("Downloaded data is null or empty");

                if (data.Length > _maxFileSize)
                    throw new SecurityException($"File too large: {data.Length} bytes (max: {_maxFileSize} bytes)");

                // Validate using SKCodec
                ValidateImageWithSKCodec(data);

                return data;
            }
            catch (HttpRequestException ex)
            {
                throw new IOException($"Failed to download image from {source}: {ex.Message}", ex);
            }
            catch (SecurityException)
            {
                throw;
            }
            catch (Exception ex) when (ex is not SecurityException)
            {
                throw new IOException($"Failed to load image from {source}: {ex.Message}", ex);
            }
        }

        private void ValidateImageWithSKCodec(byte[] data)
        {
            try
            {
                using var stream = new MemoryStream(data);
                using var codec = SKCodec.Create(stream);

                if (codec == null)
                {
                    throw new SecurityException("Invalid image format: SKCodec could not decode the image");
                }

                // Get image information
                var info = codec.Info;

                // Validate dimensions
                if (_validateDimensions)
                {
                    if (info.Width <= 0 || info.Height <= 0)
                    {
                        throw new SecurityException($"Invalid image dimensions: {info.Width}x{info.Height}");
                    }

                    if (info.Width > _maxWidth || info.Height > _maxHeight)
                    {
                        throw new SecurityException($"Image dimensions too large: {info.Width}x{info.Height} (max: {_maxWidth}x{_maxHeight})");
                    }

                    // Check pixel count to prevent memory attacks
                    long pixelCount = (long)info.Width * info.Height;
                    if (pixelCount > _maxPixels)
                    {
                        throw new SecurityException($"Image pixel count too large: {pixelCount:N0} pixels (max: {_maxPixels:N0} pixels)");
                    }
                }

                // Additional validation: try to get a valid frame
                // This ensures the image is fully valid without decoding all pixels
                var frameCount = codec.FrameCount;
                if (frameCount <= 0)
                {
                    throw new SecurityException("Invalid image: No valid frames found");
                }

                // Validate the first frame
                using var bitmap = new SKBitmap();
                var result = codec.GetPixels(info, bitmap.GetPixels());

                if (result != SKCodecResult.Success && result != SKCodecResult.IncompleteInput)
                {
                    throw new SecurityException($"Failed to decode image: {result}");
                }

                // Optional: Log image info for debugging
                System.Diagnostics.Debug.WriteLine($"Image validated: {info.Width}x{info.Height}, Format: {codec.EncodedFormat}, FrameCount: {frameCount}");
            }
            catch (SecurityException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SecurityException($"Image validation failed: {ex.Message}", ex);
            }
        }

        private bool IsDomainAllowed(string domain)
        {
            if (_allowedDomains == null || _allowedDomains.Count == 0)
                return true;

            foreach (var allowed in _allowedDomains)
            {
                if (domain.Equals(allowed, StringComparison.OrdinalIgnoreCase))
                    return true;

                // Allow subdomains
                if (allowed.StartsWith(".") && domain.EndsWith(allowed, StringComparison.OrdinalIgnoreCase))
                    return true;

                // Allow with wildcard
                if (allowed.StartsWith("*.") && domain.EndsWith(allowed.Substring(1), StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private bool IsInternalOrLocalAddress(Uri uri)
        {
            var host = uri.Host.ToLowerInvariant();

            // Check localhost variants
            if (host == "localhost" || host == "127.0.0.1" || host == "::1")
                return true;

            // Check for private IP ranges
            if (System.Net.IPAddress.TryParse(host, out var ipAddress))
            {
                var bytes = ipAddress.GetAddressBytes();
                if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    // IPv4 private ranges
                    if (bytes[0] == 10) // 10.0.0.0/8
                        return true;
                    if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) // 172.16.0.0/12
                        return true;
                    if (bytes[0] == 192 && bytes[1] == 168) // 192.168.0.0/16
                        return true;
                    if (bytes[0] == 169 && bytes[1] == 254) // 169.254.0.0/16
                        return true;
                    if (bytes[0] == 127) // 127.0.0.0/8
                        return true;
                }
                else if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                {
                    // IPv6 private ranges
                    if (bytes[0] == 0xFE && bytes[1] == 0x80) // fe80::/10 (link-local)
                        return true;
                    if (bytes[0] == 0xFC || bytes[0] == 0xFD) // fc00::/7 (unique local)
                        return true;
                    if (host == "::1") // loopback
                        return true;
                }
            }

            // Check for internal TLDs
            var internalTlds = new[] { ".local", ".internal", ".lan", ".localhost" };
            foreach (var tld in internalTlds)
            {
                if (host.EndsWith(tld, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private bool IsValidImageContentType(string? contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return false;

            var validTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "image/jpeg",
                "image/jpg",
                "image/png",
                "image/gif",
                "image/bmp",
                "image/webp",
                "image/tiff",
                "image/tif",
                "image/svg+xml",
                "image/x-icon",
                "image/vnd.microsoft.icon",
                "image/avif",
                "image/heic",
                "image/heif",
                "image/psd",
                "image/raw"
            };

            return validTypes.Contains(contentType) ||
                   contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        }

        // Security exception
        public class SecurityException : Exception
        {
            public SecurityException(string message) : base(message) { }
            public SecurityException(string message, Exception innerException) : base(message, innerException) { }
        }
    }

    public class HttpLoaderOptions
    {
        public int? MaxFileSize { get; set; }
        public int? MaxWidth { get; set; }
        public int? MaxHeight { get; set; }
        public long? MaxPixels { get; set; }
        public TimeSpan? Timeout { get; set; }
        public List<string>? AllowedDomains { get; set; }
        public bool? ValidateContentType { get; set; }
        public bool? ValidateDimensions { get; set; }
        public bool? BlockInternalAddresses { get; set; }
    }
}