// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AuthPermissions.BaseCode.CommonCode;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace AuthPermissions.AspNetCore.Services;

/// <summary>
/// This service reads in connection strings  
/// </summary>
public class ShardingConnectionStringsJson
{
    private readonly IWebHostEnvironment _env;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="env"></param>
    public ShardingConnectionStringsJson(IWebHostEnvironment env)
    {
        _env = env;
    }

    /// <summary>
    /// Not sorted out yet
    /// </summary>
    /// <returns></returns>
    /// <exception cref="AuthPermissionsException"></exception>
    public IEnumerable<(string name, string connectionString)> GetAllConnectionStrings()
    {
        var fileDirectory = _env.ContentRootPath;
        var appsettingsName = _env.IsDevelopment()
            ? "appsettings.Development.json"
            : "appsettings.Production.json";
        var filepath = Path.Combine(fileDirectory, appsettingsName);

        if (!File.Exists(filepath))
            throw new AuthPermissionsException(
                $"When using sharding you must have a {appsettingsName} file to contain the connection strings.");

        //thanks to https://kevsoft.net/2021/12/19/traversing-json-with-jsondocument.html
        using var jsonDocument = JsonDocument.Parse(File.ReadAllText(filepath));
        JsonElement connectionStringsElement;
        try
        {
            connectionStringsElement = jsonDocument.RootElement.GetProperty("ConnectionStrings");
        }
        catch (Exception)
        {
            throw new AuthPermissionsException(
                $"Could not find a ConnectionStrings section in the {appsettingsName} file.");
        }

        foreach (var jsonProperty in connectionStringsElement.EnumerateObject())
        {
            yield return (jsonProperty.Name, jsonProperty.Value.ToString());
        }
    }
}