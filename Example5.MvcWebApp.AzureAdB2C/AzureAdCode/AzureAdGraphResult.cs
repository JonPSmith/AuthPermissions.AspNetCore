// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Example5.MvcWebApp.AzureAdB2C.AzureAdCode
{
    public class AzureAdGraphResult
    {
        [JsonProperty(PropertyName = "value")]
        public List<AzureUserInfo> Users { get; set; }
    }
}