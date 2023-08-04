// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace Example7.SingleLevelShardingOnly.EfCoreClasses
{
    public class Invoice
    {
        public int InvoiceId { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string InvoiceName { get; set; }

        public DateTime DateCreated { get; set; }

        //----------------------------------------
        // relationships

        public List<LineItem> LineItems { get; set; }
    }
}