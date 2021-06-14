// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace Example2.WebApiWithToken.IndividualAccounts.Models
{
    public class JwtData
    {
        public const string SectionName = nameof(JwtData);

        public string Issuer { get; set; }
        public string Audience { get; set; }
        public string SigningKey { get; set; }
    }
}