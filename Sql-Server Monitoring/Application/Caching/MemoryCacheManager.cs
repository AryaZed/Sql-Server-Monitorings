using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace Sql_Server_Monitoring.Application.Caching
{
    public interface ICacheManager
    {
        T Get<T>(string key);
        void Set<T>(string key, T value, TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null);
        void Remove(string key);
        void Clear();
    }

    public class MemoryCacheManager : ICacheManager
    {
        private readonly IMemoryCache _cache;
        private readonly ConcurrentDictionary<string, bool> _keys = new ConcurrentDictionary<string, bool>();
        private readonly ILogger<MemoryCacheManager> _logger;

        public MemoryCacheManager(IMemoryCache cache, ILogger<MemoryCacheManager> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public T Get<T>(string key)
        {
            try
            {
                if (_cache.TryGetValue(key, out T value))
                {
                    _logger.LogDebug("Cache hit: {Key}", key);
                    return value;
                }

                _logger.LogDebug("Cache miss: {Key}", key);
                return default;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting item from cache: {Key}", key);
                return default;
            }
        }

        public void Set<T>(string key, T value, TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null)
        {
            try
            {
                // Set default expiration times if not provided
                absoluteExpiration ??= TimeSpan.FromMinutes(30);
                slidingExpiration ??= TimeSpan.FromMinutes(10);

                var options = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = absoluteExpiration,
                    SlidingExpiration = slidingExpiration
                };

                // Add post-eviction callback to clean up keys dictionary
                options.RegisterPostEvictionCallback((k, v, r, s) =>
                {
                    _keys.TryRemove(k.ToString(), out _);
                    _logger.LogDebug("Removed from cache: {Key}, Reason: {Reason}", k, r);
                });

                // Set value in cache
                _cache.Set(key, value, options);
                _keys.TryAdd(key, true);

                _logger.LogDebug("Added to cache: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting item in cache: {Key}", key);
            }
        }

        public void Remove(string key)
        {
            try
            {
                _cache.Remove(key);
                _keys.TryRemove(key, out _);
                _logger.LogDebug("Manually removed from cache: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from cache: {Key}", key);
            }
        }

        public void Clear()
        {
            try
            {
                foreach (var key in _keys.Keys.ToList())
                {
                    _cache.Remove(key);
                    _keys.TryRemove(key, out _);
                }
                _logger.LogInformation("Cache cleared");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache");
            }
        }
    }
} 