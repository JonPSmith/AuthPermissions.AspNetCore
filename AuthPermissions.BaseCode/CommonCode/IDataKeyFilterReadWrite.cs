// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace AuthPermissions.BaseCode.CommonCode
{
    /// <summary>
    /// This is on entity classes where the DataKey set by setting the DataKey directly
    /// </summary>
    public interface IDataKeyFilterReadWrite
    {
        /// <summary>
        /// The DataKey to be used for multi-tenant applications
        /// </summary>
        public string DataKey { get; set; }
    }
}