using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TutorLinkBe.Context;
using TutorLinkBe.Models;
using TutorLinkBe.Repository;
using Microsoft.EntityFrameworkCore;
using TutorLinkBe.Dto;
using TutorLinkBe.Services;
using Newtonsoft.Json;

namespace TutorLinkBe.Controllers
{
     [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
         private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<AccountController> _logger; 
        private readonly UserRepository _userRepository;
        private readonly TokenService _tokenService; 
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        
        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailService emailService,
            ILogger<AccountController> logger,
            UserRepository userRepository,
            TokenService tokenService,
            AppDbContext context,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _logger = logger;
            _userRepository = userRepository;
            _tokenService = tokenService;
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto model)
        {
            _logger.LogInformation("register start");

            if (!ModelState.IsValid || model.Password!=model.ConfirmPassword) {
                return BadRequest(ModelState);
            }
            
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null) {
                return Conflict(new { message = "Email is already registered." });
            }
            var result = await _userRepository.CreateUserWithRoleAsync(model, "User");
            if (result.Succeeded) {
                var user = await _userManager.FindByEmailAsync(model.Email);
                await _signInManager.SignInAsync(user, isPersistent: false);
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var callbackUrl = Url.Action(nameof(ConfirmEmail), "Account", new { userId = user.Id, code }, protocol: HttpContext.Request.Scheme);
                await _emailService.SendEmailAsync(model.Email, "Confirm your email", $"Please confirm your account by clicking this link: <a href='{callbackUrl}'>link</a>");
                _logger.LogInformation("after call send email");

                return Ok(new { message = "User registered successfully,Confirm your email before login." });
            }
            else {
                foreach (var error in result.Errors) {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return BadRequest(ModelState);
            }
        }

        [HttpPost("loginViaGoogle")]
        public async Task<IActionResult> LoginViaGoogle([FromBody] LoginViaGoogleDto model)
        {
            if (model == null || string.IsNullOrEmpty(model.Email))
                return BadRequest(new { message = "Email is required" });
            string pwdValue = _configuration["pwd:pwd"];
            
            if (string.IsNullOrEmpty(pwdValue))
                return StatusCode(500, new { message = "Server password configuration missing" });

            // check if user exists
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                // create a new user
                user = new ApplicationUser
                {
                    UserName = model.Name,
                    Email = model.Email,
                    AvatarUrl = model.Picture,
                    EmailConfirmed = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(user, pwdValue);

                if (!createResult.Succeeded)
                {
                    foreach (var err in createResult.Errors)
                        ModelState.AddModelError(string.Empty, err.Description);
                    return BadRequest(ModelState);
                }

                // add role
                await _userManager.AddToRoleAsync(user, "User");
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var callbackUrl = Url.Action(nameof(ConfirmEmail), "Account", new { userId = user.Id, code }, protocol: HttpContext.Request.Scheme);
                await _emailService.SendEmailAsync(model.Email, "Confirm your email", $"Please confirm your account by clicking this link: <a href='{callbackUrl}'>Link</a>");

                return Ok(new { message = "Google account registered. Please verify your email before login." });
            }

            // existing user — must have confirmed email before login
            if (!user.EmailConfirmed)
            {
                return Unauthorized(new { message = "Please confirm your email before logging in." });
            }

            // sign in with the shared password
            var result = await _signInManager.PasswordSignInAsync(user, pwdValue, false, false);
            if (!result.Succeeded)
            {
                return Unauthorized(new { message = $"Invalid login attempt." });
            }

            // generate tokens (updated to receive JTI)
            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = await _tokenService.GenerateAccessToken(user);
            var jwtId = new JwtSecurityTokenHandler().ReadJwtToken(accessToken)
                .Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
            var refreshToken = _tokenService.GenerateRefreshToken(user.Id, jwtId);


            await SaveRefreshTokenAsync(refreshToken);

            var userRole = roles.Contains("Admin") ? "Admin"
                         : roles.Contains("Teacher") ? "Teacher"
                         : "User";

            var userData = new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.EmailConfirmed,
                user.PhoneNumber,
                user.AvatarUrl
            };

            return Ok(new
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                User = userData,
                Role = userRole
            });
        }
        
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken( RefreshTokenRequestDto request)
        {
            var refreshToken = await _context.RefreshTokens
                .SingleOrDefaultAsync(rt => rt.Token == request.Token && !rt.IsRevoked);
            if (refreshToken == null) {
                return BadRequest(new { message = "Invalid refresh token." });
            }
            if (refreshToken.ExpiresUtc < DateTime.UtcNow) {
                return Unauthorized(new { message = "Refresh token has expired. Please log in again." });
            }
            var user = await _userManager.FindByIdAsync(refreshToken.UserId);
            if (user == null) {
                return BadRequest(new { message = "User not found." });
            }
            // Generate new access token (capture JTI if needed)
            var newAccessToken = await _tokenService.GenerateAccessToken(user);

            return Ok(new {
                AccessToken = newAccessToken
            });
        }
       
        private async Task SaveRefreshTokenAsync(RefreshToken refreshToken)
        {
            var existingToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.UserId == refreshToken.UserId && !rt.IsRevoked);

            if (existingToken != null) {
                existingToken.Token = refreshToken.Token;
                existingToken.IssuedUtc = refreshToken.IssuedUtc;
                existingToken.ExpiresUtc = refreshToken.ExpiresUtc;
                existingToken.JwtId = refreshToken.JwtId;
                existingToken.IsRevoked = refreshToken.IsRevoked;
                existingToken.ReplacedByToken = null; 
            }
            else {
                await _context.RefreshTokens.AddAsync(refreshToken);
            }
            await _context.SaveChangesAsync();
        }

        [HttpGet("Confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
            {
                return BadRequest("User ID and Code are required to confirm email");
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return BadRequest("Invalid User ID");
            }
            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (result.Succeeded)
            {
                return Ok(new { message = "Email confirmed successfully." });
            }
            return BadRequest("Error confirming your email.");
        }
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RevokeTokenDto request)
        {
            var token = await _context.RefreshTokens.SingleOrDefaultAsync(r => r.Token == request.Token);
            if (token == null) return NotFound(new { message = "Token not found" });
            token.IsRevoked = true;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Logout successful, token revoked." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto model)
        {
            if (!ModelState.IsValid) {
                return BadRequest(ModelState);
            }
            var user = await _userManager.FindByEmailAsync(model.Email);
            var userData = new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.EmailConfirmed,
                user.PhoneNumber,
                user.AvatarUrl
            };
            if (user == null) {
                _logger.LogWarning("Login failed for {Email}: User not found", model.Email);
                return Unauthorized(new { message = "Invalid login attempt Be." });
            }
            if (!user.EmailConfirmed)
            {
                return Unauthorized(new { message = "Please confirm your email before logging in." });
            }
            var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);

            if (result.Succeeded) {
                var roles = await _userManager.GetRolesAsync(user); 
                var accessToken = await _tokenService.GenerateAccessToken(user); 
                var refreshToken =_tokenService.GenerateRefreshToken(user.Id,Guid.NewGuid().ToString()); 
                await SaveRefreshTokenAsync(refreshToken);
                var userRole = roles.Contains("Admin") ? "Admin" : roles.Contains("Teacher") ? "Teacher" : "User";
                return Ok(new {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken.Token,
                    User = userData,
                    Role = userRole
                });
            }
            else {
                return Unauthorized(new { message = "Invalid login attempt." });
            }
        }
    }
}