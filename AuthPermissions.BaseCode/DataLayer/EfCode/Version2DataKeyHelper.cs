// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.CommonCode;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AuthPermissions.BaseCode.DataLayer.EfCode;


/// <summary>
/// Extension method to add to your application's DbContext when moving a multi-tenant application to Version2 of the AuthP library.
/// </summary>
public static class Version2DataKeyHelper
{
    /// <summary>
    /// This migration extension method should be applied to all the entities in your application that
    /// uses the the <see cref="IDataKeyFilterReadOnly"/> or <see cref="IDataKeyFilterReadWrite"/>.
    /// This fixes the bug in Version 1 of the AuthP library where hierarchical multi-tenant application
    /// could get data from another tenant - rare but possible. 
    /// NOTE: This migration is impotent, i.e. it will only change DataKey in the version 1 format
    /// </summary>
    /// <param name="migrationBuilder"></param>
    /// <param name="tableName">Table name with any schema at the start if required</param>
    /// <param name="dataKeyColumnName"></param>
    /// <exception cref="NotImplementedException"></exception>
    public static void UpdateToVersion2DataKeyFormat(
        this MigrationBuilder migrationBuilder,
        string tableName,
        string dataKeyColumnName = "DataKey")
    {
        if (tableName == null) throw new ArgumentNullException(nameof(tableName));

        if (!migrationBuilder.IsSqlServer())
            throw new NotImplementedException("This only tried with SQL Server");

        migrationBuilder.Sql(CreateVersion2DataKeyUpdateSql(tableName, dataKeyColumnName));
    }

    /// <summary>
    /// Method to create SQL for <see cref="UpdateToVersion2DataKeyFormat"/>
    /// Provided so that I can test this SQL
    /// </summary>
    /// <param name="tableName">Table name with any schema at the start if required</param>
    /// <param name="dataKeyColumnName"></param>
    /// <returns></returns>
    public static string CreateVersion2DataKeyUpdateSql(this string tableName,
        string dataKeyColumnName = "DataKey")
    {
        return $"UPDATE {tableName} " +
               $"SET {dataKeyColumnName} = RIGHT({dataKeyColumnName}, LEN({dataKeyColumnName})-1) + '.' " +
               $"WHERE {dataKeyColumnName} IS NOT NULL AND LEFT({dataKeyColumnName},1) = '.' ";
    }
}