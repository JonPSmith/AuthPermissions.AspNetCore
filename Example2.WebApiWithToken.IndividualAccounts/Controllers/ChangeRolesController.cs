using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using AuthPermissions.AdminCode;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Net.DistributedFileStoreCache;

namespace Example2.WebApiWithToken.IndividualAccounts.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ChangeRolesController : ControllerBase
    {
        private readonly IDistributedFileStoreCacheClass _fsCache;
        private readonly IAuthRolesAdminService _rolesAdmin;

        public ChangeRolesController(IDistributedFileStoreCacheClass fsCache, IAuthRolesAdminService rolesAdmin)
        {
            _fsCache = fsCache;
            _rolesAdmin = rolesAdmin;
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("ListCache")]
        public IEnumerable<string> ListCache()
        {
            var allCache = _fsCache.GetAllKeyValues();
            var result = new List<string>();
            if (!allCache.Any())
                result.Add( "No cache entries" );
            else
            {
                result.AddRange(allCache.Select(entry => $"{entry.Key} = {entry.Value})"));
            }

            return result.ToArray();
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("ListUserClaims")]
        public IEnumerable<string> ListUserClaims()
        {
            var allCache = _fsCache.GetAllKeyValues();
            var result = new List<string>();
            if (User.Identity?.IsAuthenticated != true)
                result.Add("User not logged in.");
            else
            {
                result.AddRange(User.Claims.Select(entry => $"{entry.Type} = {entry.Value})"));
            }

            return result.ToArray();
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("ListAllUsers")]
        public IEnumerable<string> ListAllUsers([FromServices]IAuthUsersAdminService usersAdmin)
        {
            var allUsers = usersAdmin.QueryAuthUsers()
                .OrderBy(x => x.Email)
                .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role).ToList();
            var result = new List<string>();
            foreach (var user in allUsers)
            {
                var combinedPermissions = new string(string
                    .Concat(user.UserRoles.SelectMany(x => x.Role.PackedPermissionsInRole)).Distinct().ToArray());
                result.Add($"Email: {user.Email}, UserId: {user.UserId}, Permissions:{string.Join(", ", combinedPermissions.Select(x => (int)x))}");
            }
            
            return result.ToArray();
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("Role2NonStandard")]
        public async Task<ActionResult> Role2NonStandard()
        {
            await _rolesAdmin.UpdateRoleToPermissionsAsync("Role2", new[] { "Permission3" }, null);
            return Ok();
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("Role2Standard")]
        public async Task<ActionResult> Role2Standard()
        {
            await _rolesAdmin.UpdateRoleToPermissionsAsync("Role2", new[] { "Permission1", "Permission2" }, null);
            return Ok();
        }
    }
}
