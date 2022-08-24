using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
