// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace ExamplesCommonCode.CommonServices;

public interface IPoorMansGlobalCache
{
    /// <summary>
    /// This will write a text file to a global directory
    /// </summary>
    /// <param name="name">name of the file. NOTE: ".txt" will be added to the name</param>
    /// <param name="content">the string to be stored the text file</param>
    void Set(string name, string content);

    /// <summary>
    /// This reads the text file in a global directory and returns the text in the file
    /// If no file is found, then it returns null
    /// </summary>
    /// <param name="name">name of the file. NOTE: ".txt" will be added to the name</param>
    /// <returns>The content saved to the global directory, or null if no file found</returns>
    string Get(string name);

    /// <summary>
    /// This ensures the text file in a global directory is deleted
    /// </summary>
    /// <param name="name"></param>
    void Remove(string name);
}