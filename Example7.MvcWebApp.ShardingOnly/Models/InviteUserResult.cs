// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace Example7.MvcWebApp.ShardingOnly.Models;

public class InviteUserResult
{
    public InviteUserResult(string message, string url)
    {
        Message = message;
        Url = url;
    }

    public string Message { get; }
    public string Url { get;  }
}