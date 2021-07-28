using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.AspNetCore.JwtTokenCode;
using AuthPermissions.PermissionsCode;
using Example2.WebApiWithToken.IndividualAccounts.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Example2.WebApiWithToken.IndividualAccounts.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticateController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ITokenBuilder _tokenBuilder;

        public AuthenticateController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, ITokenBuilder tokenBuilder, IClaimsCalculator claimsCalculator)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _tokenBuilder = tokenBuilder;
        }

        /// <summary>
        /// This checks you are a valid user and returns a JTW token
        /// </summary>
        /// <param name="loginUser"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost]
        [Route("authenticate")]
        public async Task<ActionResult> Authenticate(LoginUserModel loginUser)
        {
            //NOTE: The _signInManager.PasswordSignInAsync does not change the current ClaimsPrincipal - that only happens on the next access with the token
            var result = await _signInManager.PasswordSignInAsync(loginUser.Email, loginUser.Password, false, false);
            if (!result.Succeeded)
            {
                return BadRequest(new { message = "Username or password is incorrect" });
            }
            var user = await _userManager.FindByEmailAsync(loginUser.Email);

            return Ok(await _tokenBuilder.GenerateJwtTokenAsync(user.Id));
        }

        /// <summary>
        /// DEMO ONLY: This will generate a JWT token for the user "Super@g1.com"
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost]
        [Route("quickauthenticate")]
        public async Task<ActionResult> QuickAuthenticate()
        {
            return await Authenticate(new LoginUserModel {Email = "Super@g1.com", Password = "Super@g1.com"});
        }

        /// <summary>
        /// This checks you are a valid user and returns a JTW token and a Refresh token
        /// </summary>
        /// <param name="loginUser"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost]
        [Route("authenticatewithrefresh")]
        public async Task<ActionResult<TokenAndRefreshToken>> AuthenticateWithRefresh(LoginUserModel loginUser)
        {
            //NOTE: The _signInManager.PasswordSignInAsync does not change the current ClaimsPrincipal - that only happens on the next access with the token
            var result = await _signInManager.PasswordSignInAsync(loginUser.Email, loginUser.Password, false, false);
            if (!result.Succeeded)
            {
                return BadRequest(new { message = "Username or password is incorrect" });
            }
            var user = await _userManager.FindByEmailAsync(loginUser.Email);

            return Ok(await _tokenBuilder.GenerateTokenAndRefreshTokenAsync(user.Id));
        }

        /// <summary>
        /// DEMO ONLY: This will generate a JWT token and a Refresh token for the user "Super@g1.com"
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost]
        [Route("quickauthenticatewithrefresh")]
        public Task<ActionResult<TokenAndRefreshToken>> QuickAuthenticateWithRefresh()
        {
            return AuthenticateWithRefresh(new LoginUserModel {Email = "Super@g1.com", Password = "Super@g1.com"});
        }

        /// <summary>
        /// This will refresh the JWT token using the provided Refresh token
        /// </summary>
        /// <param name="tokenAndRefresh"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost]
        [Route("refreshauthentication")]
        public async Task<ActionResult<TokenAndRefreshToken>> RefreshAuthentication(TokenAndRefreshToken tokenAndRefresh)
        {
            var result = await _tokenBuilder.RefreshTokenUsingRefreshTokenAsync(tokenAndRefresh);
            if (result.updatedTokens != null)
                return result.updatedTokens;

            return StatusCode(result.HttpStatusCode);
        }

        /// <summary>
        /// This returns the permission names for the current user (or null if not available)
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("getuserpermissions")]
        public ActionResult<List<string>> GetUsersPermissions([FromServices] IUsersPermissionsService service)
        {
            return service.PermissionsFromUser(User);
        }

    }
}
