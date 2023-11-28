// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Test.StubClasses
{
    public class StubEndpointBuilder : EndpointBuilder
    {
        public override Endpoint Build()
        {
            throw new NotImplementedException();
        }
    }
}
