// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace AuthPermissions.DataKeyCode
{
    public interface IDataKeyCalc
    {
        /// <summary>
        /// This return the multi-tenant data key.
        /// It assumes that 
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        Task<string> GetDataKey(string userid);
    }
}