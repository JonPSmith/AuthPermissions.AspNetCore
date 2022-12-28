// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Text.Json;
using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore.OpenIdCode;
using AuthPermissions.BaseCode.SetupCode;
using Azure.Identity;
using LocalizeMessagesAndErrors;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using StatusGeneric;

namespace AuthPermissions.SupportCode.AzureAdServices;

/// <summary>
/// This provides a <see cref="ISyncAuthenticationUsers"/> service that returns all the Azure AD users who's account is enabled.
/// This implementation uses Microsoft.Graph library
/// This code came from https://docs.microsoft.com/en-us/samples/azure-samples/ms-identity-dotnetcore-b2c-account-management/manage-b2c-users-dotnet-core-ms-graph/
/// </summary>
public class AzureAdAccessService : IAzureAdAccessService
{
    private readonly ClientSecretCredential _clientSecretCredential;
    private readonly string[] _scopes = new[] { "https://graph.microsoft.com/.default" };

    private readonly IDefaultLocalizer _localizeDefault;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="options"></param>
    /// <param name="localizeProvider"></param>
    public AzureAdAccessService(IOptions<AzureAdOptions> options, IAuthPDefaultLocalizer localizeProvider)
    {
        _localizeDefault = localizeProvider.DefaultLocalizer; ;
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
    /// This will look for a user with the given email
    /// </summary>
    /// <param name="email"></param>
    /// <returns>if found it returns the user's ID, otherwise it returns null</returns>
    public async Task<string> FindAzureUserAsync(string email)
    {
        try
        {
            var graphClient = new GraphServiceClient(_clientSecretCredential, _scopes);
            var user = await graphClient.Users[email]
                .Request()
                .Select("id")
                .GetAsync();

            return user.Id;
        }
        catch (ServiceException e)
        {
            var errorJson = JsonSerializer.Deserialize<Rootobject>(e.RawResponseBody);
            if (errorJson.error.code == "Request_ResourceNotFound")
                return null;
            throw;
        }
    }

    /// <summary>
    /// This creates a new user in the Azure AD. It returns the ID of the new Azure AD user.
    /// </summary>
    /// <param name="email">Must be provided</param>
    /// <param name="userName">Must be provided</param>
    /// <param name="temporaryPassword">Must be present. It is a temporary Password</param>
    /// <returns>status: if error then return message, otherwise Result holds ID of the newly created Azure AD user</returns>
    public async Task<IStatusGeneric<string>> CreateNewUserAsync(string email, string userName, string temporaryPassword)
    {
        var status = new StatusGenericLocalizer<string>(_localizeDefault);

        if (string.IsNullOrWhiteSpace(temporaryPassword)) throw new ArgumentNullException(nameof(temporaryPassword));

        //I have to check that the email otherwise the creating of the MailNickname would fail
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return status.AddErrorString("BadEmail".ClassLocalizeKey(this, true),
                "The email was either missing or does not contain a '@'.", "Email");

        var user = new User
        {
            AccountEnabled = true,
            DisplayName = userName,
            MailNickname = GenerateValidMailNickname(email),
            UserPrincipalName = email,
            PasswordProfile = new PasswordProfile
            {
                ForceChangePasswordNextSignIn = true,
                Password = temporaryPassword
            }
        };

        try
        {
            var graphClient = new GraphServiceClient(_clientSecretCredential, _scopes);
            var result = await graphClient.Users
                .Request()
                .AddAsync(user);

            return status.SetResult(result.Id);
        }
        catch (ServiceException e)
        {
            var errorJson = JsonSerializer.Deserialize<Rootobject>(e.RawResponseBody);
            if (errorJson!.error.code == "Request_BadRequest")
                return status.AddErrorFormatted("BadAuthorization".ClassLocalizeKey(this, true),
                $"The Azure AD authorization service says: {errorJson.error.message}");
            throw;
        }
    }

    //-------------------------------------------------------
    //private methods / classes 

    private static readonly char[] NickNameInvalidChars = 
        new char [] { '@', '(', ')', '\\', '[', ']', '"', ';', ':', '.', '<', '>', ',', ' ' };

    private string GenerateValidMailNickname(string email)
    {
        //see the link below on what are allowed charaters in the Azure AD MailNickname
        //https://docs.microsoft.com/en-us/graph/api/group-post-groups?view=graph-rest-1.0&tabs=http#request-body

        return new(email.Substring(0, email.IndexOf('@'))
            .Where(c => c < 128 && !NickNameInvalidChars.Contains(c)).ToArray());
    }


    private class Rootobject
    {
        public Error error { get; set; }
    }

    private class Error
    {
        public string code { get; set; }
        public string message { get; set; }

    }

}