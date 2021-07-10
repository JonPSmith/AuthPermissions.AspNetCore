// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace AuthPermissions.DataLayer.Classes.SupportTypes
{
    /// <summary>
    /// Add this to a AuthP's entity classes to define a name for show when a database exception happens
    /// </summary>
    public interface INameToShowOnException
    {
        /// <summary>
        /// The most useful name in an entity class to show when there is a database exception
        /// </summary>
        public string NameToUseForError { get; }
    }
}