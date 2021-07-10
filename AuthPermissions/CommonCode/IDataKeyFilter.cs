// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.DataLayer.EfCode;

namespace AuthPermissions.CommonCode
{
    /// <summary>
    /// This is the interface used by the GetDataKeyFilterFromUser and <see cref="DataKeyQueryExtension"/>
    /// </summary>
    public interface IDataKeyFilter
    {
        /// <summary>
        /// The DataKey to be used for multi-tenant applications
        /// </summary>
        string DataKey { get; }
    }
}