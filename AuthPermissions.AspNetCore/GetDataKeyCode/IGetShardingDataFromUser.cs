// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace AuthPermissions.AspNetCore.GetDataKeyCode;

/// <summary>
/// This is the interface provides both the DataKey and the connection string 
/// </summary>
public interface IGetShardingDataFromUser
{
    /// <summary>
    /// The DataKey to be used for multi-tenant applications
    /// </summary>
    string DataKey { get; }

    /// <summary>
    /// This contains the connection string to the database to use
    /// If null, then use the default connection string as defined at the time when your application's DbContext was registered
    /// </summary>
    string ConnectionString { get; }
}