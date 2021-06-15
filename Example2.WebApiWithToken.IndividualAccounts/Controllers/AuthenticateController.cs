using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Example2.WebApiWithToken.IndividualAccounts.JwtCode;
using Example2.WebApiWithToken.IndividualAccounts.Models;
using Microsoft.AspNetCore.Authentication;
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

        public AuthenticateController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, ITokenBuilder tokenBuilder)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _tokenBuilder = tokenBuilder;
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("authenticate")]
        public async Task<ActionResult> Authenticate(LoginUserModel loginUser)
        {
            var result = await _signInManager.PasswordSignInAsync(loginUser.Email, loginUser.Password, false, false);
            if (!result.Succeeded)
            {
                return BadRequest(new { message = "Username or password is incorrect" });
            }
            var user = await _userManager.FindByEmailAsync(loginUser.Email);
            var claims = new List<Claim>
            {
                new Claim("Test1", "Test1Value"),
                new Claim("Test2", "Test2Value")
            };

            await HttpContext.AuthenticateAsync();

            return Ok(_tokenBuilder.GenerateJwtToken(user, claims));
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("authenticate")]
        public async Task<ActionResult> QuickAuthenticate()
        {
            return await Authenticate(new LoginUserModel {Email = "Super@g1.com", Password = "Super@g1.com"});
        }
    }
}
