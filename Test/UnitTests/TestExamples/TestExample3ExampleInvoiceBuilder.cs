// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.DataLayer.EfCode;
using Example3.InvoiceCode.AppStart;
using Example3.InvoiceCode.EfCoreClasses;
using Example3.InvoiceCode.EfCoreCode;
using Microsoft.EntityFrameworkCore;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestExamples
{
    public class TestExample3ExampleInvoiceBuilder
    {
        private readonly ITestOutputHelper _output;

        public TestExample3ExampleInvoiceBuilder(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(ExampleInvoiceTypes.Computer, 3)]
        [InlineData(ExampleInvoiceTypes.Office, 3)]
        [InlineData(ExampleInvoiceTypes.Travel, 7)]
        public void TestCreateExampleInvoice(ExampleInvoiceTypes type, int numItems)
        {
            //SETUP
            var builder = new ExampleInvoiceBuilder(".");

            //ATTEMPT
            var invoice = builder.CreateExampleInvoice(type, type.ToString());

            //VERIFY
            invoice.LineItems.Count.ShouldEqual(numItems);
        }

        [Fact]
        public void TestCreateRandomInvoice()
        {
            //SETUP
            var builder = new ExampleInvoiceBuilder(".");

            //ATTEMPT
            var invoice = builder.CreateRandomInvoice(null);

            //VERIFY
            invoice.LineItems.Count.ShouldBeInRange(3,7);
        }

    }
}