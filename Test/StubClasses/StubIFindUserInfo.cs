﻿// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.SetupCode;
using AuthPermissions.Factories;

namespace Test.StubClasses
{

    public class StubIFindUserInfoFactory : IAuthPServiceFactory<IFindUserInfoService>
    {
        private readonly bool _returnNullService;

        public StubIFindUserInfoFactory(bool returnNullService)
        {
            _returnNullService = returnNullService;
        }

        public IFindUserInfoService GetService(bool throwExceptionIfNull = true, string callingMethod = "")
        {
            return _returnNullService ? null : new StubIFindUserInfo();
        }

        public class StubIFindUserInfo : IFindUserInfoService
        {
            public Task<FindUserInfoResult> FindUserInfoAsync(string uniqueName)
            {
                return Task.FromResult(new FindUserInfoResult(uniqueName, null));
            }
        }
    }
}