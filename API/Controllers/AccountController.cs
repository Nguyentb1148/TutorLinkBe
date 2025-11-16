using Microsoft.AspNetCore.Mvc;
using TutorLinkBe.Application.DTOs;
using TutorLinkBe.Application.Interfaces;

namespace TutorLinkBe.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly ILogger<AccountController> _logger;
        private readonly IConfiguration _configuration;

        public AccountController(
            IAccountService accountService,
            ILogger<AccountController> logger,
            IConfiguration configuration)
        {
            _accountService = accountService;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto model)
        {
            if (!ModelState.IsValid || model.Password != model.ConfirmPassword)
                return BadRequest("Password and Confirm Password do not match");

            var confirmationUrl = Url.Action(nameof(ConfirmEmail), "Account", null, protocol: HttpContext.Request.Scheme) ?? "";
            var result = await _accountService.RegisterAsync(model, confirmationUrl);

            if (result.Success)
            {
                return Ok(new { success = true, message = "User registered successfully. Please confirm your email before login." });
            }

            if (result.IdentityResult == null) return Conflict(new { success = false, message = result.ErrorMessage });
            foreach (var error in result.IdentityResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return BadRequest(ModelState);

        }

        [HttpPost("loginViaGoogle")]
        public async Task<IActionResult> LoginViaGoogle([FromBody] LoginViaGoogleDto model)
        {
            var googleClientId = _configuration["GoogleAuth:ClientId"] ?? throw new InvalidOperationException("Google ClientId not configured");
            var result = await _accountService.LoginViaGoogleAsync(model, googleClientId);

            if (result is { Success: true, Data: not null })
            {
                return Ok(new
                {
                    success = true,
                    message = "User logged in successfully",
                    data = result.Data
                });
            }

            return Unauthorized(new { success = false, message = result.ErrorMessage });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(RefreshTokenRequestDto request)
        {
            var result = await _accountService.RefreshTokenAsync(request);

            if (result.Success && result.Data != null)
            {
                return Ok(new
                {
                    success = true,
                    message = "Token refreshed successfully",
                    data = result.Data
                });
            }

            if (result.ErrorMessage?.Contains("expired") == true)
                return Unauthorized(new { success = false, message = result.ErrorMessage });

            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        [HttpGet("Confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            var result = await _accountService.ConfirmEmailAsync(userId, code);

            if (result.Success)
                return Ok(new { success = true, message = "Email confirmed successfully." });

            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RevokeTokenDto request)
        {
            var result = await _accountService.LogoutAsync(request.Token);

            if (result.Success)
                return Ok(new { success = true, message = "Logout successful, token revoked." });

            return NotFound(new { success = false, message = result.ErrorMessage });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _accountService.LoginAsync(model);

            if (result is { Success: true, Data: not null })
            {
                return Ok(new
                {
                    success = true,
                    message = "User logged in successfully.",
                    data = result.Data
                });
            }

            return Unauthorized(new { success = false, message = result.ErrorMessage });
        }
    }
}
