// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace ExamplesCommonCode.IdentityCookieCode;

/// <summary>
/// This service handles the reading and writing of a a text file into a global directory shared by all instances of the application
/// </summary>
public class GlobalFileStoreManager 
{
    private readonly string _webRootPath;

    public GlobalFileStoreManager(IWebHostEnvironment environment)
    {
        _webRootPath = environment.WebRootPath;
    }

    /// <summary>
    /// This will write a text file to a global directory
    /// </summary>
    /// <param name="name">name of the file. NOTE: ".txt" will be added to the name</param>
    /// <param name="content">the string to be stored the text file</param>
    public void Set(string name, string content)
    {
        File.WriteAllText(GetFilePath(name), content);
    }

    /// <summary>
    /// This reads the text file in a global directory and returns the text in the file
    /// If no file is found, then it returns null
    /// </summary>
    /// <param name="name">name of the file. NOTE: ".txt" will be added to the name</param>
    /// <returns>The content saved to the global directory, or null if no file found</returns>
    public string Get(string name)
    {
        var filePath = GetFilePath(name);
        if (File.Exists(filePath))
            return File.ReadAllText(filePath);

        return null;
    }

    /// <summary>
    /// This ensures the text file in a global directory is deleted
    /// </summary>
    /// <param name="name"></param>
    public void Remove(string name)
    {
        var filePath = GetFilePath(name);
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    //----------------------------------------------------
    // private 

    private string GetFilePath(string name)
    {
        return Path.Combine(_webRootPath, name+".txt");
    }
}