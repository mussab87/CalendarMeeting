using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace App.Application.Contracts.Repositories.CacheService
{ }
public interface ICacheService
{
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? slidingExpiration = null, TimeSpan? absoluteExpiration = null);
    Task<T> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? slidingExpiration = null, TimeSpan? absoluteExpiration = null);
    Task RemoveAsync(string key);
}

// Implementation of the cache service
public class DistributedCacheService : ICacheService
{
    private readonly IDistributedCache _cache;

    public DistributedCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    // Get item from cache or create it if it doesn't exist
    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? slidingExpiration = null, TimeSpan? absoluteExpiration = null)
    {
        T cachedItem = await GetAsync<T>(key);

        if (cachedItem != null)
        {
            return cachedItem;
        }

        // Cache miss - get data from factory
        T item = await factory();

        // Cache the item
        await SetAsync(key, item, slidingExpiration, absoluteExpiration);

        return item;
    }

    // Get item from cache
    public async Task<T> GetAsync<T>(string key)
    {
        byte[] cachedData = await _cache.GetAsync(key);

        if (cachedData == null)
        {
            return default;
        }

        string cachedString = Encoding.UTF8.GetString(cachedData);
        return JsonSerializer.Deserialize<T>(cachedString);
    }

    // Set item in cache
    public async Task SetAsync<T>(string key, T value, TimeSpan? slidingExpiration = null, TimeSpan? absoluteExpiration = null)
    {
        string serializedData = JsonSerializer.Serialize(value);
        byte[] encodedData = Encoding.UTF8.GetBytes(serializedData);

        var options = new DistributedCacheEntryOptions();

        if (slidingExpiration.HasValue)
        {
            options.SetSlidingExpiration(slidingExpiration.Value);
        }
        else
        {
            options.SetSlidingExpiration(TimeSpan.FromMinutes(5));
        }

        if (absoluteExpiration.HasValue)
        {
            options.SetAbsoluteExpiration(absoluteExpiration.Value);
        }
        else
        {
            options.SetAbsoluteExpiration(TimeSpan.FromHours(1));
        }

        await _cache.SetAsync(key, encodedData, options);
    }

    // Remove item from cache
    public async Task RemoveAsync(string key)
    {
        await _cache.RemoveAsync(key);
    }
}

