// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.ObjectModel;
using Microsoft.Extensions.Caching.Distributed;
using Net.DistributedFileStoreCache;

namespace Test.StubClasses;

public class StubFileStoreCacheString : IDistributedFileStoreCacheString
{
    private Dictionary<string, string> _cache = new Dictionary<string, string>();

    /// <summary>Gets a value with the given key.</summary>
    /// <param name="key">A string identifying the requested value.</param>
    /// <returns>The located value or null.</returns>
    public string? Get(string key)
    {
        return _cache.TryGetValue(key, out string? value) ? value : null;
    }

    /// <summary>Gets a value with the given key.</summary>
    /// <param name="key">A string identifying the requested value.</param>
    /// <param name="token">Optional. The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, containing the located value or null.</returns>
    public Task<string?> GetAsync(string key, CancellationToken token = new CancellationToken())
    {
        return Task.FromResult(_cache.TryGetValue(key, out string? value) ? value : null);
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

    /// <summary>Sets many entries via a list of KeyValues</summary>
    /// <param name="manyEntries">List of KeyValuePairs to be added to the cache.</param>
    /// <param name="options">Optional: The cache options for the value.</param>
    public void SetMany(List<KeyValuePair<string, string>> manyEntries, DistributedCacheEntryOptions? options = null)
    {
        foreach (var keyValuePair in manyEntries)
        {
            _cache[keyValuePair.Key] = keyValuePair.Value;
        }
    }

    /// <summary>Sets many entries via a list of KeyValues</summary>
    /// <param name="manyEntries">List of KeyValuePairs to be added to the cache.</param>
    /// <param name="options">Optional: The cache options for the value.</param>
    /// <param name="token">Optional. The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    public Task SetManyAsync(List<KeyValuePair<string, string>> manyEntries, DistributedCacheEntryOptions? options = null,
        CancellationToken token = new CancellationToken())
    {
        foreach (var keyValuePair in manyEntries)
        {
            _cache[keyValuePair.Key] = keyValuePair.Value;
        }
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
    /// This clears all the key/value pairs from the json cache file, with option to add entries after the cache is cleared.
    /// </summary>
    /// <param name="manyEntries">Optional: After of the clearing the cache these KeyValues will written into the cache</param>
    /// <param name="entryOptions">Optional: If there are entries to add to the cache, this will set the timeout time.</param>
    public void ClearAll(List<KeyValuePair<string, string>>? manyEntries = null, DistributedCacheEntryOptions? entryOptions = null)
    {
        _cache = new Dictionary<string, string>();
        if (manyEntries == null)
            return;

        foreach (var keyValuePair in manyEntries)
        {
            _cache[keyValuePair.Key] = keyValuePair.Value;
        }
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