using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthPermissions.SupportCode.AddUsersServices
{
    /// <summary>
    /// Adds new user with expiration date
    /// </summary>
    internal class AddNewUserWithExpirationDto
    {
        /// <summary>
        /// Contains a unique Email (normalized by applying .ToLower), which is used for lookup
        /// If null, then it takes the UserName value
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Contains a unique user name
        /// This is used to a) provide more info on the user, or b) when using Windows authentication provider
        /// If null, then it takes non-normalized Email
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// A list of Role names to add to the AuthP user when the AuthP user is created
        /// </summary>
        public List<string> Roles { get; set; }

        /// <summary>
        /// Optional. This holds the tenantId of the tenant that the joining user should be linked to
        /// If null, then the application must not be a multi-tenant application 
        /// </summary>
        public int? TenantId { get; set; }

        //----------------------------------------------------
        //If using a register / login authentication provider

        /// <summary>
        /// If using a register / login authentication provider you need to provide the user's password
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// If using a register / login authentication provider and using cookies
        /// setting this to true will make the cookie persist after using the app
        /// </summary>
        public bool IsPersistent { get; set; }

        /// <summary>
        /// Expiration time in Epoch seconds (Unix Time Seconds)
        /// </summary>
        public long Expiration { get; set; }
    }
}
