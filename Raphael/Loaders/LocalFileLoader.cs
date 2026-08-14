using System;
using System.IO;
using System.Threading.Tasks;

namespace Raphael.Loaders
{
    /// <summary>
    /// Secure local file image loader with path traversal protection
    /// </summary>
    public class LocalFileLoader : IImageLoader
    {
        private readonly string[] _searchDirectories;
        private readonly bool _recursive;
        private readonly StringComparison _pathComparison =
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        public LocalFileLoader(string[] searchDirectories, bool recursive = false)
        {
            if (searchDirectories == null || searchDirectories.Length == 0)
                throw new ArgumentException("At least one search directory must be specified", nameof(searchDirectories));

            // Normalize and validate directories
            var normalizedDirectories = new List<string>();
            foreach (var dir in searchDirectories)
            {
                var normalized = NormalizePath(dir);
                if (!Directory.Exists(normalized))
                    throw new DirectoryNotFoundException($"Directory not found: {normalized}");

                normalizedDirectories.Add(normalized);
            }

            _searchDirectories = normalizedDirectories.ToArray();
            _recursive = recursive;
        }

        public bool CanLoad(string source)
        {
            if (string.IsNullOrEmpty(source))
                return false;

            // Check if it's a file:// URL
            if (source.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                return true;

            // Check if it's a local file path
            try
            {
                // Check if the file exists directly
                if (File.Exists(source))
                {
                    // Verify it's within allowed directories
                    var fullPath = Path.GetFullPath(source);
                    return IsPathAllowed(fullPath);
                }

                // Check if it's a filename that might exist in our search directories
                var fileName = Path.GetFileName(source);
                if (!string.IsNullOrEmpty(fileName) && IsValidImageExtension(fileName))
                {
                    return true;
                }

                // Check if it's a relative path
                if (!source.Contains("://") && !source.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    return IsValidImageExtension(source);
                }
            }
            catch
            {
                // If we can't parse it, it's not a valid file
            }

            return false;
        }

        public async Task<byte[]> LoadAsync(string source)
        {
            if (string.IsNullOrEmpty(source))
                throw new ArgumentException("Source cannot be null or empty", nameof(source));

            try
            {
                string filePath = source;

                // Handle file:// protocol
                if (source.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                {
                    filePath = source.Substring("file://".Length);

                    // Handle Windows paths (file:///C:/path)
                    if (filePath.StartsWith("/") && filePath.Length > 2 && filePath[2] == ':')
                    {
                        filePath = filePath.Substring(1);
                    }
                }

                // Normalize the path
                var normalizedPath = NormalizePath(filePath);

                // Check for path traversal attempts
                if (IsPathTraversalAttempt(normalizedPath))
                {
                    throw new SecurityException($"Path traversal attempt detected: {source}");
                }

                // Check if file exists at the given path and is within allowed directories
                if (File.Exists(normalizedPath))
                {
                    var fullPath = Path.GetFullPath(normalizedPath);
                    if (IsPathAllowed(fullPath))
                    {
                        return await File.ReadAllBytesAsync(fullPath);
                    }
                    throw new SecurityException($"Access denied: Path is outside allowed directories: {source}");
                }

                // Search in configured directories
                var foundPath = FindFileInDirectories(normalizedPath);
                if (foundPath != null)
                {
                    return await File.ReadAllBytesAsync(foundPath);
                }

                throw new FileNotFoundException($"File not found: {source}. Searched in: {string.Join(", ", _searchDirectories)}");
            }
            catch (SecurityException)
            {
                throw;
            }
            catch (Exception ex) when (ex is not FileNotFoundException && ex is not SecurityException)
            {
                throw new IOException($"Failed to load file from {source}: {ex.Message}", ex);
            }
        }

        private string? FindFileInDirectories(string filePath)
        {
            // Get just the filename
            var fileName = Path.GetFileName(filePath);
            if (string.IsNullOrEmpty(fileName))
                return null;

            // Validate filename doesn't contain path traversal
            if (IsPathTraversalAttempt(fileName))
            {
                return null;
            }

            // Search in all configured directories
            foreach (var directory in _searchDirectories)
            {
                if (!Directory.Exists(directory))
                    continue;

                try
                {
                    // Build safe path
                    var safePath = Path.Combine(directory, fileName);
                    var fullPath = Path.GetFullPath(safePath);

                    // Verify the resolved path is still within the allowed directory
                    if (!IsPathAllowed(fullPath))
                        continue;

                    if (File.Exists(fullPath))
                        return fullPath;

                    // If recursive search is enabled, search subdirectories
                    if (_recursive)
                    {
                        var found = SearchDirectoryRecursive(directory, fileName);
                        if (found != null)
                            return found;
                    }
                }
                catch
                {
                    // Skip directories that can't be accessed
                }
            }

            return null;
        }

        private string? SearchDirectoryRecursive(string directory, string fileName)
        {
            try
            {
                // Search in subdirectories
                var subDirectories = Directory.GetDirectories(directory);
                foreach (var subDir in subDirectories)
                {
                    try
                    {
                        // Verify the subdirectory is still within allowed paths
                        var fullSubDir = Path.GetFullPath(subDir);
                        if (!IsPathAllowed(fullSubDir))
                            continue;

                        var fullPath = Path.Combine(subDir, fileName);
                        var resolvedPath = Path.GetFullPath(fullPath);

                        if (!IsPathAllowed(resolvedPath))
                            continue;

                        if (File.Exists(resolvedPath))
                            return resolvedPath;

                        // Continue searching deeper
                        var found = SearchDirectoryRecursive(subDir, fileName);
                        if (found != null)
                            return found;
                    }
                    catch
                    {
                        // Skip directories that can't be accessed
                    }
                }
            }
            catch
            {
                // Skip directories that can't be accessed
            }

            return null;
        }

        private bool IsPathTraversalAttempt(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            // Normalize path separators
            path = path.Replace('\\', '/');

            // Check for common path traversal patterns
            var traversalPatterns = new[]
            {
                "../",
                "..\\",
                "/../",
                "\\..\\",
                "..",
                "/etc/passwd",
                "/etc/shadow",
                "/boot.ini",
                "/windows/win.ini",
                "C:\\",
                "/root/",
                "/var/log/",
                "/proc/",
                "/sys/"
            };

            foreach (var pattern in traversalPatterns)
            {
                if (path.Contains(pattern, _pathComparison))
                    return true;
            }

            // Check for encoded traversal attempts
            var encodedPatterns = new[]
            {
                "%2e%2e",  // ..
                "%2E%2E",  // ..
                "%252e",   // encoded %
                "..%252f", // ../ encoded
                "..%5c",   // ..\ encoded
                "%c0%ae",  // Unicode encoded
                "%c1%9c"
            };

            foreach (var pattern in encodedPatterns)
            {
                if (path.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private bool IsPathAllowed(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
                return false;

            try
            {
                fullPath = Path.GetFullPath(fullPath);

                foreach (var allowedDir in _searchDirectories)
                {
                    var normalizedAllowedDir = Path.GetFullPath(allowedDir);

                    // Check if the path starts with the allowed directory
                    if (fullPath.StartsWith(normalizedAllowedDir, _pathComparison))
                    {
                        // Additional safety: ensure we're not just matching a prefix incorrectly
                        // e.g., /var/www and /var/www2
                        if (fullPath.Length > normalizedAllowedDir.Length)
                        {
                            var nextChar = fullPath[normalizedAllowedDir.Length];
                            if (nextChar != Path.DirectorySeparatorChar && nextChar != Path.AltDirectorySeparatorChar)
                            {
                                continue; // Not a proper subdirectory match
                            }
                        }
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            // Replace alternate separators with the system separator
            if (Path.DirectorySeparatorChar != Path.AltDirectorySeparatorChar)
            {
                path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            // Remove any trailing separators
            path = path.TrimEnd(Path.DirectorySeparatorChar);

            // Get full path to resolve any relative segments
            try
            {
                path = Path.GetFullPath(path);
            }
            catch
            {
                // If we can't get full path, keep as-is but it will be validated later
            }

            return path;
        }

        private static bool IsValidImageExtension(string path)
        {
            try
            {
                var extension = Path.GetExtension(path);
                if (string.IsNullOrEmpty(extension))
                    return false;

                var validExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    ".jpg", ".jpeg", ".png", ".gif", ".bmp",
                    ".webp", ".tiff", ".tif", ".ico", ".avif",
                    ".heic", ".heif", ".svg", ".psd", ".raw"
                };

                // Validate the extension doesn't contain path traversal
                if (extension.Contains("..") || extension.Contains('/') || extension.Contains('\\'))
                    return false;

                return validExtensions.Contains(extension);
            }
            catch
            {
                return false;
            }
        }

        // Security exception
        public class SecurityException : Exception
        {
            public SecurityException(string message) : base(message) { }
            public SecurityException(string message, Exception innerException) : base(message, innerException) { }
        }
    }
}