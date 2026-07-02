using Core.Models;
using Microsoft.Extensions.Caching.Memory;

namespace CampaignsApi.Service.MemCache;

public class MemCacheService(IMemoryCache cache) : ICacheService
{
    private readonly IMemoryCache _cache = cache;
    
    public object? Get(string key) => _cache.TryGetValue(key, out var cachedValue) ? cachedValue : null;

    public void Set(string key, object value) => _cache.Set(key, value, TimeSpan.FromMinutes(15));

    public void Remove(string key) => _cache.Remove(key);
    public Task<T?> GetAsync<T>(string key)
    {
        var cached = Get(key);
        
        return cached as Task<T?> ?? default;
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        Set(key, value);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        Remove(key);
        return Task.CompletedTask;
    }
}