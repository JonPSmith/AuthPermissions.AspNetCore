// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using Net.DistributedFileStoreCache.SupportCode;
using System.Text.Json;
using AuthPermissions.AspNetCore.ShardingServices;
using Net.DistributedFileStoreCache;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Encodings.Web;

namespace ConsoleApp.AuthP6Upgrade;
class Program
{
    static void Main(string[] args)
    {
        //The arguments needed are 
        //1. The name of the json file holding the AuthP version 5 sharding entries, e.g. shardingsettings.Production.json
        //2. The name for the new FileStore Cache file, e.g. FileStoreCacheFile.Production.json
        //3. The filepath to the the json file. This can be a relative or absolute


        if (args.Length != 3)
        {
            Console.WriteLine("This app expects three arguments: ");
            Console.WriteLine("   1. The filepath to the the json file. This can be a relative or absolute.");
            Console.WriteLine("   2. The name of the json file holding the AuthP version 5 sharding entries.");
            Console.WriteLine("      e.g. shardingsettings.Production.json");
            Console.WriteLine("   3. The name for the new FileStore Cache file used by AuthP version 6.");
            Console.WriteLine("      e.g. FileStoreCacheFile.Production.json");
            return;
        }

        var filePathToJsonFilePath = Path.Combine(args[0], args[1]);

        if (!File.Exists(filePathToJsonFilePath))
        {
            Console.WriteLine("No json file was found using the filepath and the json file name you provided.");
            Console.WriteLine("The full filePath you provided is");
            Console.WriteLine(filePathToJsonFilePath);
            return;
        }

        var jsonString = File.ReadAllText(filePathToJsonFilePath);
        //Console.WriteLine(jsonString);
        var shardingData = JsonSerializer.Deserialize<JsonFileFormat>(jsonString)?.ShardingDatabases;

        if (shardingData == null || !shardingData.Any())
        {
            Console.WriteLine("There aren't any sharding entries in your json file to place the the FileStore cache.");
            Console.WriteLine("In this case the FileStore cache will start empty, which is what you want.");
        }

        var breakDownCacheName = args[2].Split('.');
        if (!((breakDownCacheName.Length == 3 && breakDownCacheName[2] == "json") 
              || (breakDownCacheName.Length == 2 && breakDownCacheName[1] != "json")))
        {
            Console.WriteLine("The name of the FileStore Cache name doesn't have the correct format.");
            Console.WriteLine("The FileStore Cache should have three parts, e.g. FileStoreCacheFile.Production.json");
            Console.WriteLine(args[2]);
            return;
        }
        //Console.WriteLine(breakDownCacheName.ToString());

        //Now use Net.DistributedFileStoreCache to add the 
        string fileStoreName = null;
        var services = new ServiceCollection();
        services.AddDistributedFileStoreCache(options =>
        {
            options.WhichVersion = FileStoreCacheVersions.Class;
            options.PathToCacheFileDirectory = Path.GetFullPath(args[0]);
            options.FirstPartOfCacheFileName = breakDownCacheName[0];
            options.SecondPartOfCacheFileName = breakDownCacheName[1];

            options.JsonSerializerForCacheFile = new JsonSerializerOptions
            {
                //This will make the json in the FileStore json file will be easier to read
                //BUT it will be a bit slower and take up more characters
                WriteIndented = true,
                //This makes unicode chars smaller - especially useful for FileStoreCacheVersions.Class
                //see https://github.com/JonPSmith/Net.DistributedFileStoreCache/wiki/Tips-on-making-your-cache-fast#class-version---already-has-unsaferelaxedjsonescaping
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            fileStoreName = options.FormCacheFileName();
        });
        var serviceProvider = services.BuildServiceProvider();
        var readWriteService = serviceProvider.GetRequiredService<IDistributedFileStoreCacheClass>();

        foreach (var shardingEntry in shardingData)
        {
            var key = GetSetShardingEntriesFileStoreCache.ShardingEntryPrefix + shardingEntry.Name;
            readWriteService.SetClass(key, shardingEntry);
            Console.WriteLine($"Added the sharding entry with the name of '{shardingEntry.Name}' added to the FileStore");
        }

        Console.WriteLine($"Successfully copied {shardingData.Count} sharding entry to the FileStore Cache called '{fileStoreName}'.");
    }
}

public class JsonFileFormat 
{
    public List<ShardingEntry> ShardingDatabases { get; set; } 
}