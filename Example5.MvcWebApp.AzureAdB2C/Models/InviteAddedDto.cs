// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace Example5.MvcWebApp.AzureAdB2C.Models;

public class InviteAddedDto
{
    public InviteAddedDto(string message, string tempPassword)
    {
        Message = message;
        TempPassword = tempPassword;
    }

    public string Message { get; }
    public string TempPassword { get; }
}