// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.CommonCode;

namespace Example6.SingleLevelSharding.EfCoreClasses
{
    public class LineItem : IDataKeyFilterReadWrite
    {
        public int LineItemId { get; set; }

        public string ItemName { get; set; }

        public int NumberItems { get; set; }

        public decimal TotalPrice { get; set; }

        public string DataKey { get; set; }

        //----------------------------------------------
        // relationships 

        public int InvoiceId { get; set; }
    }
}