# Updating your code from AuthPermissions.AspNetCore 5.* to 6.0

Version 6.0.0 of the AuthPermissions.AspNetCore library (shortened to _AuthP_ from now on) contains improved sharding features, but these changes contain **BREAKING CHANGES**, but they ONLY EFFECT multi-tenant applications that use sharding, i.e. you have the `SetupMultiTenantSharding` extension method in your AuthP registration code.

These breaking changes came about because of a limitation found in the ASP.NET Core `IOptions` feature. However, when I was working on the data that links tenants to databases (now called _sharding entries_) I found my previous code didn't use _nice names_. For instance:

- The name of a class, method, etc didn't match what it was used for. For instance the class that links a tenant to a database was called `DatabaseInformation`, but its name is now its called `ShardingEntry`. 
- Some method names were too long. For instance the method that updated a `ShardingEntry` was called `UpdateDatabaseInfoToShardingInformation` and now called `UpdateShardingEntry`.

Of course this creates more breaking changes, but the code will be easier to understand. Also on upgrading to AuthP 6 it will be very obvious, as each change will become an error.  

## TABLE OF CONTENT

1. Changing to the new IGetSetShardingEntries service.
2. Updating the IGetSetShardingEntries method names
3. Make sure that distributed FileStore Cache is registered
4. Move your AuthP 5 sharding entries to the AuthP 6 FileStore Cache
5. Register the FileStore Cache (if not already there)

The subsections below the items listed in the table of content.

## 1. Changing to the new [IGetSetShardingEntries](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/AuthPermissions.AspNetCore/ShardingServices/IGetSetShardingEntries.cs) service

Before AuthP version 6 you had two services called `IAccessDatabaseInformationVer5` and `IShardingConnections`. Now you have one service called `IGetSetShardingEntries` that contains all the methods, properties, etc. that the two services it replaces.

| Old name          | new name      | Notes |
| ----------------- | ------------- | ----- |
| `IAccessDatabaseInformationVer5`    | `IGetSetShardingEntries` | I use `shardingService` |
| `IShardingConnections`    | `IGetSetShardingEntries` | I use `shardingService` |

The other big change is `IGetSetShardingEntries` changes the name of a class

| Old name          | new name      | Notes |
| ----------------- | ------------- | ----- |
| `DatabaseInformation`    | `ShardingEntry` |  |

## 2. Updating the IGetSetShardingEntries method names

The table below gives the old and new.
| Old name          | new name      | Notes |
| ----------------- | ------------- | ----- |
| `ReadAllShardingInformation`    | `GetAllShardingEntries` |  |
| `GetDatabaseInformationByName`    | `GetSingleShardingEntry` |  |
| `AddDatabaseInfoToShardingInformation`    | `AddNewShardingEntry` |  |
| `UpdateDatabaseInfoToShardingInformation`    | `UpdateShardingEntry` |  |
| `RemoveDatabaseInfoFromShardingInformationAsync`    | `RemoveShardingEntry` |  |
| `GetAllPossibleShardingData` | `GetAllShardingEntries` |  |
| `GetDatabaseInfoNamesWithTenantNamesAsync` | `GetShardingsWithTenantNamesAsync` |  |

A properly that has been simplified.
| Old name          | new name      | Notes |
| ----------------- | ------------- | ----- |
| `ShardingDatabaseProviders.Keys.ToArray()`    | `PossibleDatabaseProviders` |  |

Other `IShardingConnections` methods that haven't changed are listed below.
| Old name          | new name      | Notes |
| ----------------- | ------------- | ----- |
| `GetConnectionStringNames` | `GetConnectionStringNames` | No change  |

## Move your AuthP 5 sharding entries to the AuthP 6 FileStore Cache

Once your code complies with AuthP version 6 and **before you run your application** you need to move your sharding entries from the AuthP's version 5 json file to AuthP's version 6 distributed FileStore Cache. There is a Console app called [`ConsoleApp.AuthP6Upgrade`](https://github.com/JonPSmith/AuthPermissions.AspNetCore/tree/main/ConsoleApp.AuthP6Upgrade) within the AuthP's repo that will copy any sharding entries from the AuthP's version 5's json file into the AuthP version 6's Distributed FileStore Cache.

To use the the ConsoleApp.AuthP6Upgrade you could:

- Clone the AuthP's repo and to gain access to the console app that way.
- You could create a ConsoleApp and copy the [`ConsoleApp.AuthP6Upgrade.Program` code](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/ConsoleApp.AuthP6Upgrade/Program.cs) into your Program class. You will need the following NuGet packages:
  - AuthPermissions.AspNetCore
  - Net.DistributedFileStoreCache

### Running the ConsoleApp.AuthP6Upgrade

This console app needs three parameters:

1. The filepath to the the json file. This can be a relative or absolute.
2. The name of the json file holding the AuthP version 5 sharding entries,   e.g. "shardingsettings.Production.json"
3. The name for the new FileStore Cache file used by AuthP version 6, e.g. "FileStoreCacheFile.Production.json".

Here is an example of running this on a Window's command prompt. 

```text
C:>  dotnet run ...Example6.MvcWebApp.Sharding shardingsettings.Development.json FileStoreCacheFile.Production.json
Added the sharding entry with the name of 'Default Database' added to the FileStore
Added the sharding entry with the name of 'DatabaseWest1' added to the FileStore
Added the sharding entry with the name of 'DatabaseEast1' added to the FileStore
Successfully copied 3 sharding entry to the FileStore Cache called 'FileStoreCacheFile.Production.json'.
C:> 
```

This console app will store the sharding entries in the FileStore Cache using a format which makes it easier to to read (but is slightly slower). But when your application adds / changes an sharding entry it will use your applications's setup of the DistributedFileStoreCache will take over and the default settings means it stored more compactly, which is faster. 

## 5. Register the FileStore Cache (if not already there)

The default implementation of the `IGetSetShardingEntries service uses the [Net.DistributedFileStoreCache](https://github.com/JonPSmith/Net.DistributedFileStoreCache). Therefore you need to make sure that you have registered the DistributedFileStoreCache during startup - see the [Register the DistributedFileStoreCache](https://github.com/JonPSmith/AuthPermissions.AspNetCore/wiki/Configuring-sharding#2-register-the-distributedfilestorecache) document.

_NOTE: You might already registered for another use, like ["down for maintenance"](https://www.thereformedprogrammer.net/how-to-take-an-asp-net-core-web-site-down-for-maintenance/) feature._

END
