// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using AuthPermissions.CommonCode;
using Example3.InvoiceCode.EfCoreCode;

namespace Example3.InvoiceCode.EfCoreClasses
{
    public class Invoice : IDataKeyFilterReadWrite
    {
        public int InvoiceId { get; set; }

        public string InvoiceName { get; set; }

        public DateTime DateCreated { get; set; }

        public string DataKey { get; set; }

        //----------------------------------------
        // relationships

        public List<LineItem> LineItems { get; set; }


    }
}