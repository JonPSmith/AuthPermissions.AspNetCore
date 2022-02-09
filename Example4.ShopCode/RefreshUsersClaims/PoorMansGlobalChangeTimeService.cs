// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.IO;
using ExamplesCommonCode.IdentityCookieCode;
using Microsoft.AspNetCore.Hosting;

namespace Example4.ShopCode.RefreshUsersClaims;

/// <summary>
/// This service handles the reading and writing of a DateTime to a place that all instances of the application
/// I call this a "Poor Mans" version as it uses a File - that works but a common cache like Redis would be better
/// </summary>
public class PoorMansGlobalChangeTimeService : IGlobalChangeTimeService
{
    private const string ChangedTimeFileName = "GlobalChangedTimeUtc.txt";

    private readonly string _filePathToGlobalTimeFile;

    public PoorMansGlobalChangeTimeService(IWebHostEnvironment environment)
    {
        _filePathToGlobalTimeFile = Path.Combine(environment.WebRootPath, ChangedTimeFileName);
    }

    /// <summary>
    /// This will write a file to a global directory. The file contains the <see cref="DateTime.UtcNow"/> as a string
    /// </summary>
    public void SetGlobalChangeTimeToNowUtc()
    { 
        File.WriteAllText(_filePathToGlobalTimeFile, DateTime.UtcNow.ToString("O"));
    }

    /// <summary>
    /// This reads the File in a global directory and returns the DateTime of the in the file
    /// If no file is found, then it returns <see cref="DateTime.MaxValue"/>, which says no change has happened
    /// </summary>
    /// <returns></returns>
    public DateTime GetGlobalChangeTimeUtc()
    {
        try
        {
            var fileContent = File.ReadAllText(_filePathToGlobalTimeFile);
            return DateTime.SpecifyKind(DateTime.Parse(fileContent), DateTimeKind.Utc);
        }
        catch (Exception)
        {
            return DateTime.MinValue; //if no file, then there is no change
        }
    }

    public void DeleteGlobalFile()
    {
        if (File.Exists(_filePathToGlobalTimeFile))
            File.Delete(_filePathToGlobalTimeFile);
    }
}