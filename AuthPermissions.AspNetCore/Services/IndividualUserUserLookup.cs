// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Threading.Tasks;
using AuthPermissions.SetupParts;
using Microsoft.AspNetCore.Identity;

namespace AuthPermissions.AspNetCore.Services
{
    public class IndividualUserUserLookup : IFindUserIdService
    {
        private readonly UserManager<IdentityUser> _userManager;

        public IndividualUserUserLookup(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<string> FindUserIdAsync(string uniqueName)
        {
            var user = await _userManager.FindByNameAsync(uniqueName);
            return user?.Id;
        }
    }
}