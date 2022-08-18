// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Caching.Distributed;
using Net.DistributedFileStoreCache;
using Net.DistributedFileStoreCache.SupportCode;

namespace Test.TestHelpers;

public class StubFileStoreCacheString : IDistributedFileStoreCacheString
{
    private Dictionary<string, string> _cache = new Dictionary<string, string>();

    /// <summary>Gets a value with the given key.</summary>
    /// <param name="key">A string identifying the requested value.</param>
    /// <returns>The located value or null.</returns>
    public string? Get(string key)
    {
        return StaticCachePart.CacheContent.Cache.TryGetValue(key, out string? value) ? value : null;
    }

    /// <summary>Gets a value with the given key.</summary>
    /// <param name="key">A string identifying the requested value.</param>
    /// <param name="token">Optional. The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, containing the located value or null.</returns>
    public Task<string?> GetAsync(string key, CancellationToken token = new CancellationToken())
    {
        return Task.FromResult(StaticCachePart.CacheContent.Cache.TryGetValue(key, out string? value) ? value : null);
    }

    /// <summary>Sets a value with the given key.</summary>
    /// <param name="key">A string identifying the requested value.</param>
    /// <param name="value">The value to set in the cache.</param>
    /// <param name="options">The cache options for the value.</param>
    public void Set(string key, string value, DistributedCacheEntryOptions? options = null)
    {
        _cache[key] = value;
    }

    /// <summary>Sets the value with the given key.</summary>
    /// <param name="key">A string identifying the requested value.</param>
    /// <param name="value">The value to set in the cache.</param>
    /// <param name="options">The cache options for the value.</param>
    /// <param name="token">Optional. The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public Task SetAsync(string key, string value, DistributedCacheEntryOptions? options = null,
        CancellationToken token = new CancellationToken())
    {
        _cache[key] = value;
        return Task.CompletedTask;
    }

    /// <summary>Removes the value with the given key.</summary>
    /// <param name="key">A string identifying the requested value.</param>
    public void Remove(string key)
    {
        _cache.Remove(key);
    }

    /// <summary>Removes the value with the given key.</summary>
    /// <param name="key">A string identifying the requested value.</param>
    /// <param name="token">Optional. The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public Task RemoveAsync(string key, CancellationToken token = new CancellationToken())
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }

    /// <summary>
    /// This clears all the key/value pairs from the json cache file
    /// </summary>
    public void ClearAll()
    {
        _cache = new Dictionary<string, string>();
    }

    /// <summary>
    /// This return all the cached values as a dictionary
    /// </summary>
    /// <returns></returns>
    public IReadOnlyDictionary<string, string> GetAllKeyValues()
    {
        return new ReadOnlyDictionary<string, string>(_cache);
    }

    /// <summary>
    /// This return all the cached values as a dictionary
    /// </summary>
    /// <returns></returns>
    public Task<IReadOnlyDictionary<string, string>> GetAllKeyValuesAsync(CancellationToken token = new CancellationToken())
    {
        return Task.FromResult(new ReadOnlyDictionary<string, string>(_cache) as IReadOnlyDictionary<string, string>);
    }
}