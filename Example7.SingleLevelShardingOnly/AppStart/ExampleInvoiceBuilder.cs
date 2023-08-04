// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using Example7.SingleLevelShardingOnly.EfCoreClasses;

namespace Example7.SingleLevelShardingOnly.AppStart
{
    public enum ExampleInvoiceTypes { Computer = 0, Office = 1, Travel = 2 }

    public class ExampleInvoiceBuilder
    {
        private readonly Random _random = new Random();

        private readonly Dictionary<ExampleInvoiceTypes, string[]> LineItemsDict = new Dictionary<ExampleInvoiceTypes, string[]>()
        {
            { ExampleInvoiceTypes.Computer, new [] {"Windows PC", "Keyboard", "BIG Screen" } },
            { ExampleInvoiceTypes.Office, new [] { "Desk", "Chair", "Filing cabinet", "Waste bin" } },
            { ExampleInvoiceTypes.Travel, new [] { "Taxi", "Flight", "Hotel", "Taxi", "Lunch", "Flight", "Taxi" } },
        };

        public Invoice CreateRandomInvoice(string companyName, string invoiceName = null)
        {
            //thanks to https://stackoverflow.com/questions/29482/how-can-i-cast-int-to-enum
            var invoiceType = (ExampleInvoiceTypes)Enum.ToObject(typeof(ExampleInvoiceTypes), 
                _random.Next(0, ((int)ExampleInvoiceTypes.Travel)+1));

            return CreateExampleInvoice(invoiceType, invoiceName ?? invoiceType.ToString(), companyName);
        }

        public Invoice CreateExampleInvoice(ExampleInvoiceTypes invoiceType, string invoiceName, string companyName)
        {
            var invoice = new Invoice
            {
                InvoiceName = invoiceName + $" - ({companyName})",
                DateCreated = DateTime.UtcNow,
                LineItems = new List<LineItem>()
            };

            foreach (var name in LineItemsDict[invoiceType])
            {
                invoice.LineItems.Add(new LineItem
                {
                    ItemName = name,
                    NumberItems = 1,
                    TotalPrice = _random.Next(10, 1000),
                });
            }

            return invoice;
        }
    }
}