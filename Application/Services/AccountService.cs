using System.IdentityModel.Tokens.Jwt;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using TutorLinkBe.Application.DTOs;
using TutorLinkBe.Application.Interfaces;
using TutorLinkBe.Domain.Entities;
using TutorLinkBe.Infrastructure.Helper;

namespace TutorLinkBe.Application.Services
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailService _emailService;
        private readonly IUserRepository _userRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly TokenService _tokenService;
        private readonly ILogger<AccountService> _logger;

        public AccountService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailService emailService,
            IUserRepository userRepository,
            IAccountRepository accountRepository,
            TokenService tokenService,
            ILogger<AccountService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _userRepository = userRepository;
            _accountRepository = accountRepository;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<ServiceResult<IdentityResult>> RegisterAsync(RegisterDto model, string confirmationUrl)
        {
            try
            {
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning("Registration attempt with existing email: {Email}", model.Email);
                    return ServiceResult<IdentityResult>.FailureResult("Email is already registered.");
                }

                var result = await _userRepository.CreateUserWithRoleAsync(model, Roles.User);
                if (!result.Succeeded)
                {
                    _logger.LogWarning("User registration failed for {Email}. Errors: {Errors}", 
                        model.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                    return ServiceResult<IdentityResult>.FailureResult(result);
                }

                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    _logger.LogError("User created but not found after registration: {Email}", model.Email);
                    return ServiceResult<IdentityResult>.FailureResult("User registration completed but user not found.");
                }

                await _signInManager.SignInAsync(user, isPersistent: false);
                var confirmationCode = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var callbackUrl = $"{confirmationUrl}?userId={user.Id}&code={Uri.EscapeDataString(confirmationCode)}";
                
                await _emailService.SendEmailAsync(
                    model.Email, 
                    "Confirm your email", 
                    $"Please confirm your account by clicking this link: <a href='{callbackUrl}'>Confirm Email</a>");
                
                _logger.LogInformation("Registration successful and confirmation email sent to {Email}", model.Email);
                return ServiceResult<IdentityResult>.SuccessResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration for {Email}", model.Email);
                return ServiceResult<IdentityResult>.FailureResult("An error occurred during registration. Please try again.");
            }
        }

        public async Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginDto model)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    _logger.LogWarning("Login attempt with non-existent email: {Email}", model.Email);
                    return ServiceResult<AuthResponseDto>.FailureResult("Invalid email or password.");
                }

                if (!user.EmailConfirmed)
                {
                    _logger.LogWarning("Login attempt with unconfirmed email: {Email}", model.Email);
                    return ServiceResult<AuthResponseDto>.FailureResult("Please confirm your email before logging in.");
                }

                if (!user.IsActive)
                {
                    _logger.LogWarning("Login attempt with inactive account: {Email}", model.Email);
                    return ServiceResult<AuthResponseDto>.FailureResult("Your account has been deactivated. Please contact support.");
                }

                var signInResult = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
                
                if (!signInResult.Succeeded)
                {
                    if (signInResult.IsLockedOut)
                    {
                        _logger.LogWarning("Account locked out: {Email}", model.Email);
                        return ServiceResult<AuthResponseDto>.FailureResult("Account is temporarily locked due to multiple failed login attempts.");
                    }
                    
                    _logger.LogWarning("Invalid login attempt for {Email}", model.Email);
                    return ServiceResult<AuthResponseDto>.FailureResult("Invalid email or password.");
                }

                var authResponse = await GenerateAuthResponseAsync(user);
                _logger.LogInformation("Successful login for user: {Email}", model.Email);
                
                return ServiceResult<AuthResponseDto>.SuccessResult(authResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for {Email}", model.Email);
                return ServiceResult<AuthResponseDto>.FailureResult("An error occurred during login. Please try again.");
            }
        }

        public async Task<ServiceResult<AuthResponseDto>> LoginViaGoogleAsync(LoginViaGoogleDto model, string googleClientId)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new List<string> { googleClientId }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(model.Credential, settings);

                if (payload == null || string.IsNullOrEmpty(payload.Email))
                {
                    _logger.LogWarning("Invalid Google credential payload");
                    return ServiceResult<AuthResponseDto>.FailureResult("Invalid Google credential.");
                }

                var loginInfo = new UserLoginInfo("Google", payload.Subject, "Google");
                var user = await _userManager.FindByLoginAsync(loginInfo.LoginProvider, loginInfo.ProviderKey);

                if (user == null)
                {
                    user = await _userManager.FindByEmailAsync(payload.Email);

                    if (user == null)
                    {
                        user = new ApplicationUser
                        {
                            UserName = payload.Name ?? payload.Email,
                            Email = payload.Email,
                            AvatarUrl = payload.Picture,
                            EmailConfirmed = true,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };

                        var createResult = await _userManager.CreateAsync(user);
                        if (!createResult.Succeeded)
                        {
                            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                            _logger.LogError("Google user creation failed: {Errors}", errors);
                            return ServiceResult<AuthResponseDto>.FailureResult($"User creation failed: {errors}");
                        }

                        await _userManager.AddToRoleAsync(user, Roles.User);
                        _logger.LogInformation("New Google user created: {Email}", payload.Email);
                    }

                    var addLoginResult = await _userManager.AddLoginAsync(user, loginInfo);
                    if (!addLoginResult.Succeeded)
                    {
                        var errors = string.Join(", ", addLoginResult.Errors.Select(e => e.Description));
                        _logger.LogError("Failed to link Google account: {Errors}", errors);
                        return ServiceResult<AuthResponseDto>.FailureResult($"Failed to link Google account: {errors}");
                    }
                }

                if (!user.IsActive)
                {
                    _logger.LogWarning("Google login attempt with inactive account: {Email}", payload.Email);
                    return ServiceResult<AuthResponseDto>.FailureResult("Your account has been deactivated. Please contact support.");
                }

                var authResponse = await GenerateAuthResponseAsync(user);
                _logger.LogInformation("Successful Google login for user: {Email}", payload.Email);
                
                return ServiceResult<AuthResponseDto>.SuccessResult(authResponse);
            }
            catch (InvalidJwtException ex)
            {
                _logger.LogWarning(ex, "Invalid Google JWT token");
                return ServiceResult<AuthResponseDto>.FailureResult("Invalid Google credential token.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google login");
                return ServiceResult<AuthResponseDto>.FailureResult("An error occurred during Google login. Please try again.");
            }
        }

        public async Task<ServiceResult<RefreshTokenResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request)
        {
            try
            {
                var refreshToken = await _accountRepository.GetRefreshTokenByTokenAsync(request.Token);
                
                if (refreshToken == null)
                {
                    _logger.LogWarning("Refresh token not found or revoked: {Token}", request.Token?.Substring(0, Math.Min(10, request.Token?.Length ?? 0)));
                    return ServiceResult<RefreshTokenResponseDto>.FailureResult("Invalid refresh token.");
                }

                if (refreshToken.ExpiresUtc < DateTime.UtcNow)
                {
                    _logger.LogWarning("Expired refresh token used: {TokenId}", refreshToken.Id);
                    await _accountRepository.RevokeRefreshTokenAsync(request.Token);
                    return ServiceResult<RefreshTokenResponseDto>.FailureResult("Refresh token has expired. Please log in again.");
                }

                var user = await _userManager.FindByIdAsync(refreshToken.UserId ?? string.Empty);
                if (user == null)
                {
                    _logger.LogWarning("User not found for refresh token: {UserId}", refreshToken.UserId);
                    return ServiceResult<RefreshTokenResponseDto>.FailureResult("User not found.");
                }

                if (!user.IsActive)
                {
                    _logger.LogWarning("Refresh token used for inactive user: {UserId}", refreshToken.UserId);
                    await _accountRepository.RevokeAllUserTokensAsync(refreshToken.UserId ?? string.Empty);
                    return ServiceResult<RefreshTokenResponseDto>.FailureResult("Your account has been deactivated.");
                }

                // Token rotation: Revoke old token and create new one
                await _accountRepository.RevokeRefreshTokenAsync(request.Token);
                
                var newAccessToken = await _tokenService.GenerateAccessToken(user);
                var jwtId = ExtractJwtId(newAccessToken);
                var newRefreshToken = _tokenService.GenerateRefreshToken(user.Id, jwtId);
                
                await _accountRepository.CreateRefreshTokenAsync(newRefreshToken);

                var response = new RefreshTokenResponseDto
                {
                    AccessToken = newAccessToken
                };

                _logger.LogInformation("Token refreshed successfully for user: {UserId}", user.Id);
                return ServiceResult<RefreshTokenResponseDto>.SuccessResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return ServiceResult<RefreshTokenResponseDto>.FailureResult("An error occurred during token refresh. Please try again.");
            }
        }

        public async Task<ServiceResult> ConfirmEmailAsync(string userId, string code)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
                {
                    return ServiceResult.FailureResult("User ID and confirmation code are required.");
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Email confirmation attempt with invalid user ID: {UserId}", userId);
                    return ServiceResult.FailureResult("Invalid confirmation link.");
                }

                if (user.EmailConfirmed)
                {
                    _logger.LogInformation("Email already confirmed for user: {UserId}", userId);
                    return ServiceResult.SuccessResult();
                }

                var result = await _userManager.ConfirmEmailAsync(user, code);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Email confirmed successfully for user: {Email}", user.Email);
                    return ServiceResult.SuccessResult();
                }

                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Email confirmation failed for user {UserId}: {Errors}", userId, errors);
                return ServiceResult.FailureResult("Invalid or expired confirmation code.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during email confirmation for user: {UserId}", userId);
                return ServiceResult.FailureResult("An error occurred during email confirmation. Please try again.");
            }
        }

        public async Task<ServiceResult> LogoutAsync(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return ServiceResult.FailureResult("Token is required.");
                }

                var refreshToken = await _accountRepository.GetRefreshTokenByTokenAsync(token);
                if (refreshToken == null)
                {
                    _logger.LogWarning("Logout attempt with invalid token");
                    return ServiceResult.FailureResult("Invalid token.");
                }

                await _accountRepository.RevokeRefreshTokenAsync(token);
                _logger.LogInformation("User logged out successfully: {UserId}", refreshToken.UserId);
                
                return ServiceResult.SuccessResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return ServiceResult.FailureResult("An error occurred during logout. Please try again.");
            }
        }

        private async Task<AuthResponseDto> GenerateAuthResponseAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = await _tokenService.GenerateAccessToken(user);
            var jwtId = ExtractJwtId(accessToken);
            var refreshToken = _tokenService.GenerateRefreshToken(user.Id, jwtId);
            
            // Revoke old token if exists (single device) or allow multiple devices
            // For multiple device support, comment out the next line
            var existingToken = await _accountRepository.GetActiveRefreshTokenByJwtIdAsync(jwtId);
            if (existingToken != null)
            {
                await _accountRepository.RevokeRefreshTokenAsync(existingToken.Token ?? string.Empty);
            }
            
            await _accountRepository.CreateRefreshTokenAsync(refreshToken);

            var userRole = DetermineUserRole(roles);

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token ?? string.Empty,
                User = new UserInfoDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    EmailConfirmed = user.EmailConfirmed,
                    PhoneNumber = user.PhoneNumber,
                    AvatarUrl = user.AvatarUrl
                },
                Role = userRole
            };
        }

        private static string ExtractJwtId(string accessToken)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(accessToken);
                return jsonToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value 
                       ?? Guid.NewGuid().ToString();
            }
            catch
            {
                return Guid.NewGuid().ToString();
            }
        }

        private static string DetermineUserRole(IList<string> roles)
        {
            if (roles.Contains(Roles.Admin))
                return Roles.Admin;
            if (roles.Contains(Roles.Teacher))
                return Roles.Teacher;
            return Roles.User;
        }
    }
}