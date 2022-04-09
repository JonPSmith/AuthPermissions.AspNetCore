// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.DataLayer.EfCode;

namespace AuthPermissions.AspNetCore.GetDataKeyCode
{
    /// <summary>
    /// This is the interface used by the GetDataKeyFilterFromUser and <see cref="DataKeyQueryExtension"/>
    /// </summary>
    public interface IGetDataKeyFromUser
    {
        /// <summary>
        /// The DataKey to be used for multi-tenant applications
        /// </summary>
        string DataKey { get; }
    }
}