namespace Core.Models;

public interface ICacheService
{
    object? Get(string key);
    void Set(string key, object value);
    void Remove(string key);
    
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task RemoveAsync(string key);
}