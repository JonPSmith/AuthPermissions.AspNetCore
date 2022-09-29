
using AuthPermissions.AspNetCore.Services;
using AuthPermissions.BaseCode.CommonCode;
using Example7.BlazorWASMandWebApi.Application.Identity.Tokens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;

namespace Example7.BlazorWASMandWebApi.Host.Controllers;

public sealed class TokensController : VersionNeutralApiController
{
    private readonly ITokenService _tokenService;
    private readonly IDisableJwtRefreshToken _disableJwtRefreshService;

    public TokensController(ITokenService tokenService, IDisableJwtRefreshToken disableJwtRefreshService)
    {
        _tokenService = tokenService;
        _disableJwtRefreshService = disableJwtRefreshService;
    }

    [HttpPost]
    [AllowAnonymous]
    // [OpenApiOperation("Request an access token using credentials.", "")]
    public Task<TokenResponse> GetTokenAsync(TokenRequest request, CancellationToken cancellationToken)
    {
        return _tokenService.GetTokenAsync(request, cancellationToken);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    // [OpenApiOperation("Request an access token using a refresh token.", "")]
    public Task<TokenResponse> RefreshAsync(RefreshTokenRequest request)
    {
        return _tokenService.RefreshTokenAsync(request);
    }

    /// <summary>
    /// This will mark the JST refresh as used, so the user cannot refresh the JWT Token.
    /// </summary>
    /// <returns></returns>
    [HttpPost("logout")]
    // [OpenApiOperation("Mark the access token as so the user cannot refresh the token.", "")]
    public async Task<ActionResult> Logout()
    {
        if (User.GetUserIdFromUser() is not { } userId || string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await _disableJwtRefreshService.MarkJwtRefreshTokenAsUsedAsync(userId);

        return Ok();
    }

    /*private string GetIpAddress() =>
        Request.Headers.ContainsKey("X-Forwarded-For")
            ? Request.Headers["X-Forwarded-For"]
            : HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "N/A";
    */
}