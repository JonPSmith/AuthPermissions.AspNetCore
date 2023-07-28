// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Net.DistributedFileStoreCache;
using Test.TestHelpers;

namespace Test.StubClasses;

public class StubFileStoreCacheClass : StubFileStoreCacheString, IDistributedFileStoreCacheClass
{
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// This method is useful if you want to decode a cache value via the <see cref="DistributedFileStoreCacheString.GetAllKeyValues"/>
    /// or the <see cref="DistributedFileStoreCacheString.GetAllKeyValuesAsync"/> methods
    /// </summary>
    /// <typeparam name="T">A class which can be created</typeparam>
    /// <param name="jsonString"></param>
    /// <returns>The deserialize class or null.</returns>
    public T? GetClassFromString<T>(string? jsonString) where T : class, new()
    {
        return jsonString == null ? null : JsonSerializer.Deserialize<T>(jsonString);
    }

    /// <summary>Gets a class stored as json linked to the given key.</summary>
    /// <param name="key">A string identifying the requested stored class.</param>
    /// <typeparam name="T">A class which can be created</typeparam>
    /// <returns>The deserialize class or null.</returns>
    public T? GetClass<T>(string key) where T : class, new()
    {
        var stringValue = Get(key);
        return stringValue == null ? null : JsonSerializer.Deserialize<T>(stringValue);
    }

    /// <summary>Gets a class stored as json linked to the given key.</summary>
    /// <param name="key">A string identifying the requested stored class.</param>
    /// <param name="token">Optional. The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <typeparam name="T">A class which can be created</typeparam>
    /// <returns>The located class or null withing a Task result.</returns>
    public async Task<T?> GetClassAsync<T>(string key, CancellationToken token = new CancellationToken()) where T : class, new()
    {
        var stringValue = await GetAsync(key, token);
        return stringValue == null ? null : JsonSerializer.Deserialize<T>(stringValue);
    }

    /// <summary>Serializers the class and stores the json against the given key.</summary>
    /// <param name="key">A string identifying the requested value.</param>
    /// <param name="yourClass">The class that you wanted to be stored in the cache.</param>
    /// <param name="options">The cache options for the value.</param>
    /// <typeparam name="T">A class which can be created</typeparam>
    public void SetClass<T>(string key, T yourClass, DistributedCacheEntryOptions? options = null) where T : class, new()
    {
        var jsonString = JsonSerializer.Serialize(yourClass, _jsonOptions);
        Set(key, jsonString, options);
    }

    /// <summary>Serializers the class and stores the json against the given key.</summary>
    /// <param name="key">A string identifying the requested value.</param>
    /// <param name="yourClass">The class that you wanted to be stored in the cache.</param>
    /// <param name="options">The cache options for the value.</param>
    /// <param name="token">Optional. The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <typeparam name="T">A class which can be created</typeparam>
    public async Task SetClassAsync<T>(string key, T yourClass, DistributedCacheEntryOptions? options = null,
        CancellationToken token = new()) where T : class, new()
    {
        var jsonString = JsonSerializer.Serialize(yourClass, _jsonOptions);
        await SetAsync(key, jsonString, options, token);
    }

    /// <summary>Serializes all the values in each KeyValue using the T type and save each into the cache</summary>
    /// <param name="manyEntries">List of KeyValuePairs to be added to the cache, with the values being serialized.</param>
    /// <param name="options">Optional: The cache options for the value.</param>
    /// <typeparam name="T">A class which contains the data to stored as JSON in the cache</typeparam>
    public void SetManyClass<T>(List<KeyValuePair<string, T>> manyEntries, DistributedCacheEntryOptions? options) where T : class, new()
    {
        foreach (var keyValue in manyEntries)
        {
            var jsonString = JsonSerializer.Serialize(keyValue.Value, _jsonOptions);
            Set(keyValue.Key, jsonString, options);
        }
    }

    /// <summary>Serializes all the values in each KeyValue using the T type and save each into the cache</summary>
    /// <param name="manyEntries">List of KeyValuePairs to be added to the cache, with the values being serialized.</param>
    /// <param name="options">Optional: The cache options for the value.</param>
    /// <param name="token">Optional. The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <typeparam name="T">A class which contains the data to stored as JSON in the cache</typeparam>
    public async Task SetManyClassAsync<T>(List<KeyValuePair<string, T>> manyEntries, DistributedCacheEntryOptions? options, CancellationToken token) where T : class, new()
    {
        foreach (var keyValue in manyEntries)
        {
            var jsonString = JsonSerializer.Serialize(keyValue.Value, _jsonOptions);
            await SetAsync(keyValue.Key, jsonString, options, token);
        }
    }
}