# Updating your code from AuthPermissions.AspNetCore 5.* to 6.0

Version 6.0.0 of the AuthPermissions.AspNetCore library (shortened to **AuthP** from now on) contains improved sharding features, but they contain **BREAKING CHANGES**. These breaking changes came about because of a limitation found in the ASP.NET Core `IOptions` feature (see this section ????). These changes ONLY EFFECT multi-tenant applications that use sharding, i.e. you have the `SetupMultiTenantSharding` extension method in your AuthP registration code.

However, when I was working on the data that links tenants to databases (now called _sharding entries_) I found my previous code didn't use _nice names_. For instance:

- The name of a class, method, etc didn't match what it was used for. For instance the class that links a tenant to a database was called `DatabaseInformation`, but its name is now its called `ShardingEntry`. 
- Some method names were too long. For instance the method that updated a `ShardingEntry` was called `UpdateDatabaseInfoToShardingInformation` and now called `UpdateShardingEntry`.

Of course this creates more breaking changes, but the code will be easier to understand. Also on upgrading to AuthP 6 it will be very obvious, as each change will become an error.  

## TABLE OF CONTENT

1. Changing to the new IGetSetShardingEntries service.
2. Updating the IGetSetShardingEntries method names
3. Make sure that distributed FileStore Cache is registered
4. Move your AuthP 5 sharding entries to the AuthP 5 FileStore Cache

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
| `ShardingDatabaseProviders.Keys.ToArray()`    | `PossibleDatabaseProviders()` |  |

Other `IShardingConnections` methods that haven't changed are listed below.
| Old name          | new name      | Notes |
| ----------------- | ------------- | ----- |
| `GetConnectionStringNames` | `GetConnectionStringNames` | No change  |

## Move your AuthP 5 sharding entries to the AuthP 5 FileStore Cache

Once your code complies with AuthP version 6 and **before you run your application** you need to move your sharding entries from the json file to the distributed FileStore Cache.



END
