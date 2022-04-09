// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;

namespace ExamplesCommonCode.CommonAdmin
{
    public class EmailAndUserNameDto
    {
        public EmailAndUserNameDto(string email, string userName)
        {
            Email = email;
            UserName = userName;
        }

        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.EmailSize)]
        public string Email { get; private set; }

        [MaxLength(AuthDbConstants.UserNameSize)]
        public string UserName { get; private set; }
    }
}