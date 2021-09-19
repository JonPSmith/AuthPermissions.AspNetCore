// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Example3.InvoiceCode.EfCoreClasses;
using Example3.InvoiceCode.EfCoreCode;

namespace Example3.InvoiceCode.AppStart
{
    public enum ExampleInvoiceTypes { Computer = 0, Office = 1, Travel = 2 }

    public class ExampleInvoiceBuilder
    {
        private readonly Dictionary<ExampleInvoiceTypes, string[]> LineItemsDict = new Dictionary<ExampleInvoiceTypes, string[]>()
        {
            { ExampleInvoiceTypes.Computer, new [] {"Windows PC", "Keyboard", "BIG Screen" } },
            { ExampleInvoiceTypes.Office, new [] {"Desk", "Chair", "Filing cabinet", "Waste bin" } },
            { ExampleInvoiceTypes.Travel, new [] { "Taxi", "Flight", "Hotel", "Taxi", "Lunch", "Flight", "Taxi" } },
        };

        private readonly Random _random = new Random();
        private readonly string _dataKey;

        public ExampleInvoiceBuilder(string dataKey)
        {
            _dataKey = dataKey;
        }

        public Invoice CreateRandomInvoice(string invoiceName)
        {
            //thanks to https://stackoverflow.com/questions/29482/how-can-i-cast-int-to-enum
            var invoiceType = (ExampleInvoiceTypes)Enum.ToObject(typeof(ExampleInvoiceTypes), 
                _random.Next(0, ((int)ExampleInvoiceTypes.Travel)+1));

            return CreateExampleInvoice(invoiceType, invoiceName ?? invoiceType.ToString());
        }

        public Invoice CreateExampleInvoice(ExampleInvoiceTypes invoiceType, string invoiceName)
        {
            var invoice = new Invoice
            {
                InvoiceName = invoiceName,
                DataKey = _dataKey,
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
                    DataKey = _dataKey
                });
            }

            return invoice;
        }
    }
}