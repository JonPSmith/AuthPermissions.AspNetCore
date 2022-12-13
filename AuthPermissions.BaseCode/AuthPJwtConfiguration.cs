// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using StatusGeneric;

namespace AuthPermissions.BaseCode
{
    /// <summary>
    /// This contains the data that the JWT token (and optional RefreshToken)
    /// </summary>
    public class AuthPJwtConfiguration
    {
        /// <summary>
        /// This identifies provider that issued the JWT
        /// </summary>
        public string Issuer { get; set; }
        /// <summary>
        /// This identifies the recipients that the JWT is intended for
        /// </summary>
        public string Audience { get; set; }
        /// <summary>
        /// This is a SECRET key that both the issuer and audience have to have 
        /// </summary>
        public string SigningKey { get; set; }
        /// <summary>
        /// JWT Token' `Expires` property is set to a date by added a `TokenExpires` timespan to the current Datetime.UtcNow
        /// </summary>
        public TimeSpan TokenExpires { get; set; }
        /// <summary>
        /// This Timespan is used to work out if the RefreshToken (in the database) has expired or not
        /// </summary>
        public TimeSpan RefreshTokenExpires { get; set; }

        /// <summary>
        /// This checks that the properties have been set
        /// NOTE: Doesn't check RefreshTokenExpires as might not be used.
        /// </summary>
        /// <returns></returns>
        public IStatusGeneric CheckThisJwtConfiguration()
        {
            //This isn't changed to StatusGenericLocalizer because
            //a) its used while it is called during the registering the services
            //b) The errors are turned into an exception
            var status = new StatusGenericHandler("AuthP JWT Token config");

            if (string.IsNullOrEmpty(Issuer))
                status.AddError($"{nameof(Issuer)} must not be null or empty", nameof(Issuer));
            if (string.IsNullOrEmpty(Audience))
                status.AddError($"{nameof(Audience)} must not be null or empty", nameof(Audience));
            if (string.IsNullOrEmpty(SigningKey))
                status.AddError($"{nameof(SigningKey)} must not be null or empty", nameof(SigningKey));
            if (TokenExpires == default)
                status.AddError($"{nameof(TokenExpires)} must be set with a TimeSpan", nameof(TokenExpires));

            return status;
        }
    }
}