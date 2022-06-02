// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using Azure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Graph;

namespace AuthPermissions.SupportCode.AzureAdServices;

/// <summary>
/// This provides a <see cref="ISyncAuthenticationUsers"/> service that returns all the Azure AD users who's account is enabled.
/// This implementation uses Microsoft.Graph library
/// This code came from https://docs.microsoft.com/en-us/samples/azure-samples/ms-identity-dotnetcore-b2c-account-management/manage-b2c-users-dotnet-core-ms-graph/
/// </summary>
public class AzureAdAccessService : IAzureAdAccessService, ISyncAuthenticationUsers
{
    private readonly ClientSecretCredential _clientSecretCredential;
    private readonly string[] _scopes = new[] { "https://graph.microsoft.com/.default" };

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="options"></param>
    public AzureAdAccessService(IOptions<AzureAdOptions> options)
    {
        var value = options.Value;
        _clientSecretCredential = new ClientSecretCredential(value.TenantId, value.ClientId, value.ClientSecret);
    }

    /// <summary>
    /// This returns a list of all the enabled users for syncing with the AuthP users
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<SyncAuthenticationUser>> GetAllActiveUserInfoAsync()
    {
        
        var result = new List<SyncAuthenticationUser>();
        var graphClient = new GraphServiceClient(_clientSecretCredential, _scopes);

        var users = await graphClient.Users
            .Request()
            .Select(x => new { x.Id, x.Mail, x.UserPrincipalName, x.DisplayName, x.AccountEnabled })
            .GetAsync();

        // Iterate over all the users in the directory
        var pageIterator = PageIterator<User>
            .CreatePageIterator(
                graphClient,
                users,
                // Callback executed for each user in the collection
                (user) =>
                {
                    if (user.AccountEnabled == true)
                        result.Add(new SyncAuthenticationUser(user.Id,
                            user.Mail ?? user.UserPrincipalName, user.DisplayName));
                    return true;
                }
            );

        await pageIterator.IterateAsync();

        return result; 
    }

    /// <summary>
    /// This creates a new user in the Azure AD. It returns the ID of the new Azure AD user.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="userName"></param>
    /// <param name="password"></param>
    /// <returns>returns the ID of the newly created Azure AD user</returns>
    public async Task<string> CreateNewUserAsync(string email, string userName, string password )
    {
        var graphClient = new GraphServiceClient(_clientSecretCredential, _scopes);

        var user = new User
        {
            AccountEnabled = true,
            DisplayName = userName,
            UserPrincipalName = email,
            PasswordProfile = new PasswordProfile
            {
                ForceChangePasswordNextSignIn = false,
                Password = password
            }
        };

        var result = await graphClient.Users
            .Request()
            .AddAsync(user);

        return result.Id;
    }

}