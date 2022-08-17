// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;

namespace Example4.ShopCode.RefreshUsersClaims;

public interface IGlobalChangeTimeService
{
    void SetGlobalChangeTimeToNowUtc();
    DateTime GetGlobalChangeTimeUtc();
}