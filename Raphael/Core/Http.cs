namespace Raphael.Core
{
    public static class Http
    {
        static Dictionary<string, byte[]> _cached = [];
        public static async Task<byte[]> FetchBytesAsync(string url)
        {
            if (!_cached.ContainsKey(url))
            {
                using HttpClient client = new();
                var bytes = await client.GetByteArrayAsync(url);
                _cached[url] = bytes;
            }

            return _cached[url];
        }
    }
}
