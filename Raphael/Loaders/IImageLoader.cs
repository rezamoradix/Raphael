namespace Raphael.Loaders
{
    /// <summary>
    /// Interface for image loaders that can load image data from various sources
    /// </summary>
    public interface IImageLoader
    {
        /// <summary>
        /// Determines if this loader can handle the given source
        /// </summary>
        /// <param name="source">The source string (URL, file path, etc.)</param>
        /// <returns>True if this loader can load from the source</returns>
        bool CanLoad(string source);

        /// <summary>
        /// Loads image data from the source
        /// </summary>
        /// <param name="source">The source string (URL, file path, etc.)</param>
        /// <returns>The image data as a byte array</returns>
        /// <exception cref="Exception">Thrown when loading fails</exception>
        Task<byte[]> LoadAsync(string source);

        /// <summary>
        /// Gets the priority of this loader (higher priority = loaded first)
        /// </summary>
        int Priority => 0;

        /// <summary>
        /// Gets the name of the loader
        /// </summary>
        string Name => GetType().Name;
    }

    /// <summary>
    /// Base abstract class for image loaders with common functionality
    /// </summary>
    public abstract class ImageLoaderBase : IImageLoader
    {
        public abstract bool CanLoad(string source);
        public abstract Task<byte[]> LoadAsync(string source);

        public virtual int Priority => 0;
        public virtual string Name => GetType().Name;

        protected void ValidateSource(string source)
        {
            if (string.IsNullOrEmpty(source))
                throw new ArgumentException("Source cannot be null or empty", nameof(source));
        }

        protected void ValidateData(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new InvalidOperationException("Loaded data is null or empty");
        }
    }
}