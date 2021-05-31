// Copyright (c) 2019 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.PermissionsCode;
using Microsoft.AspNetCore.Authorization;

namespace AuthPermissions.AspNetCore.PolicyCode
{
    //thanks to https://www.jerriepelser.com/blog/creating-dynamic-authorization-policies-aspnet-core/

    public class PermissionPolicyHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly Type _enumPermissionType;

        public PermissionPolicyHandler(EnumTypeService enumTypeService)
        {
            _enumPermissionType = enumTypeService.EnumPermissionsType;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {

            var permissionsClaim =
                context.User.Claims.SingleOrDefault(c => c.Type == PermissionConstants.PackedPermissionClaimType);
            // If user does not have the scope claim, get out of here
            if (permissionsClaim == null)
                return Task.CompletedTask;

            if (_enumPermissionType.ThisPermissionIsAllowed(permissionsClaim.Value, requirement.PermissionName))
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}