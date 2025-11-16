using Microsoft.AspNetCore.Identity;
using TutorLinkBe.Application.DTOs;

namespace TutorLinkBe.Application.Interfaces
{
    public interface IAccountService
    {
        Task<ServiceResult<IdentityResult>> RegisterAsync(RegisterDto model, string confirmationUrl);
        Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginDto model);
        Task<ServiceResult<AuthResponseDto>> LoginViaGoogleAsync(LoginViaGoogleDto model, string googleClientId);
        Task<ServiceResult<RefreshTokenResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request);
        Task<ServiceResult> ConfirmEmailAsync(string userId, string code);
        Task<ServiceResult> LogoutAsync(string token);
    }

    public class ServiceResult<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? ErrorMessage { get; set; }
        public IdentityResult? IdentityResult { get; set; }

        public static ServiceResult<T> SuccessResult(T data) => new() { Success = true, Data = data };
        public static ServiceResult<T> FailureResult(string errorMessage) => new() { Success = false, ErrorMessage = errorMessage };
        public static ServiceResult<T> FailureResult(IdentityResult identityResult) => new() { Success = false, IdentityResult = identityResult };
    }

    public class ServiceResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }

        public static ServiceResult SuccessResult() => new() { Success = true };
        public static ServiceResult FailureResult(string errorMessage) => new() { Success = false, ErrorMessage = errorMessage };
    }
}
