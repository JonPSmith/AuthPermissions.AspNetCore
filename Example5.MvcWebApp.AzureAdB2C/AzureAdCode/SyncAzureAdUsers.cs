// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.CommonCode;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Example5.MvcWebApp.AzureAdB2C.AzureAdCode
{
    //code taken from https://github.com/Azure-Samples/active-directory-dotnetcore-daemon-v2/tree/master/1-Call-MSGraph
    //see https://github.com/Azure-Samples/active-directory-dotnetcore-daemon-v2/tree/master/1-Call-MSGraph#register-the-client-app-daemon-console on how to set it up
    public class SyncAzureAdUsers : ISyncAuthenticationUsers
    {
        //See this SO answer https://stackoverflow.com/a/52497226/1434764 for using filter
        const string ReadUsersGraphApi = "https://graph.microsoft.com/v1.0/users";//"?$select=displayName,id,userPrincipalName,preferred_username";
        //const string ReadUsersGraphApi = "https://graph.microsoft.com/v1.0/users";

        private readonly AzureAdOptions _config;
        private readonly HttpClient _httpClient;

        public SyncAzureAdUsers(IOptions<AzureAdOptions> config, IHttpClientFactory httpClientFactory)
        {
            _config = config.Value;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<IEnumerable<SyncAuthenticationUser>> GetAllActiveUserInfoAsync()
        {
            await SetupHttpClientToAdAsync();
            var fullUserInfo = new List<AzureUserInfo>();
            var jsonResults = await QueryAzureAdGraphAsync(ReadUsersGraphApi);
            foreach (var jsonResult in jsonResults)
            {
                fullUserInfo.AddRange(JsonConvert.DeserializeObject<AzureAdGraphResult>(jsonResult).Users);
            }

            return fullUserInfo.Select(x => new SyncAuthenticationUser(x.id, x.mail ?? x.userPrincipalName, x.displayName));
        }

        //-----------------------------------
        //private methods

        //see https://docs.microsoft.com/en-us/graph/paging for paging
        private async Task<List<string>> QueryAzureAdGraphAsync(string url)
        {
            var returns = new List<string>();
            while (url != null)
            {
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    returns.Add(json);
                    url = FindPossibleNextLink(ref json);
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    throw new AuthPermissionsException($"{url} failed with code {response.StatusCode}. Content = {content}");
                }
            }

            return returns;
        }

        /// <summary>
        /// This looks for a NextLink item and returns that if found (otherwise null)
        /// NOTE: I use ref string for performance reasons - the ref means the data is not copied to the stack
        /// </summary>
        /// <param name="graphReturnString"></param>
        /// <returns></returns>
        private static string FindPossibleNextLink(ref string graphReturnString)
        {
            return JObject.Parse(graphReturnString)["@odata.nextLink"]?.Value<string>();
        }

        private async Task SetupHttpClientToAdAsync()
        {
            var accessToken = await GetAppTokenAsync(_config);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", accessToken);
        }

        private static async Task<string> GetAppTokenAsync(AzureAdOptions config)
        {
            var authority = config.Instance + config.TenantId;
            var app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
                .WithClientSecret(config.ClientSecret)
                .WithAuthority(new Uri(authority))
                .Build();

            // With client credentials flows the scopes is ALWAYS of the shape "resource/.default", as the 
            // application permissions need to be set statically (in the portal or by PowerShell), and then granted by
            // a tenant administrator
            string[] scopes = new string[]
            {
                "https://graph.microsoft.com/.default"
            };

            AuthenticationResult result = await app.AcquireTokenForClient(scopes).ExecuteAsync();

            // Return the access token.
            return result.AccessToken;
        }
    }
}