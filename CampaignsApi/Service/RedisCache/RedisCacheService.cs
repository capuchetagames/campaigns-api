using System.Text.Json;
using Core.Models;
using Microsoft.Extensions.Caching.Distributed;

namespace CampaignsApi.Service.RedisCache;


public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(10);

    public RedisCacheService(IDistributedCache cache) => _cache = cache;

    public object? Get(string key)
    {
        throw new NotImplementedException();
    }

    public void Set(string key, object value)
    {
        throw new NotImplementedException();
    }

    public void Remove(string key)
    {
        throw new NotImplementedException();
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var data = await _cache.GetStringAsync(key);
        return data is null ? default : JsonSerializer.Deserialize<T>(data);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry ?? DefaultExpiry
        };
        await _cache.SetStringAsync(key, JsonSerializer.Serialize(value), options);
    }

    public Task RemoveAsync(string key) => _cache.RemoveAsync(key);
}