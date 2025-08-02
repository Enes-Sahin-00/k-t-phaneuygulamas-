using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace kütüphaneuygulaması.Services
{
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<CacheService> _logger;
        private readonly Dictionary<string, HashSet<string>> _categoryKeys = new();

        public CacheService(IMemoryCache memoryCache, ILogger<CacheService> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                if (_memoryCache.TryGetValue(key, out T? value))
                {
                    _logger.LogDebug("Cache hit for key: {Key}", key);
                    return value;
                }

                _logger.LogDebug("Cache miss for key: {Key}", key);
                return default(T);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache for key: {Key}", key);
                return default(T);
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            try
            {
                var options = new MemoryCacheEntryOptions();
                
                if (expiration.HasValue)
                {
                    options.AbsoluteExpirationRelativeToNow = expiration;
                }
                else
                {
                    // Default expiration: 30 minutes
                    options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
                }

                // Add sliding expiration for frequently accessed items
                options.SlidingExpiration = TimeSpan.FromMinutes(10);

                _memoryCache.Set(key, value, options);
                _logger.LogDebug("Cache set for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache for key: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                _memoryCache.Remove(key);
                _logger.LogDebug("Cache removed for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache for key: {Key}", key);
            }
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            try
            {
                // Note: IMemoryCache doesn't support pattern-based removal
                // This is a simplified implementation
                // In production, consider using Redis or a more sophisticated cache
                _logger.LogWarning("Pattern-based cache removal not fully supported with IMemoryCache");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache by pattern: {Pattern}", pattern);
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            return _memoryCache.TryGetValue(key, out _);
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            var cachedValue = await GetAsync<T>(key);
            if (cachedValue != null)
            {
                return cachedValue;
            }

            var value = await factory();
            await SetAsync(key, value, expiration);
            return value;
        }

        public async Task InvalidateCategoryAsync(string category)
        {
            try
            {
                if (_categoryKeys.TryGetValue(category, out var keys))
                {
                    foreach (var key in keys)
                    {
                        await RemoveAsync(key);
                    }
                    _categoryKeys.Remove(category);
                }
                _logger.LogDebug("Cache invalidated for category: {Category}", category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating cache for category: {Category}", category);
            }
        }

        public async Task InvalidateUserAsync(int userId)
        {
            try
            {
                var userKeys = new List<string>
                {
                    $"user:{userId}:orders",
                    $"user:{userId}:cart",
                    $"user:{userId}:favorites"
                };

                foreach (var key in userKeys)
                {
                    await RemoveAsync(key);
                }
                _logger.LogDebug("Cache invalidated for user: {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating cache for user: {UserId}", userId);
            }
        }

        private void AddToCategory(string category, string key)
        {
            if (!_categoryKeys.ContainsKey(category))
            {
                _categoryKeys[category] = new HashSet<string>();
            }
            _categoryKeys[category].Add(key);
        }
    }
} 