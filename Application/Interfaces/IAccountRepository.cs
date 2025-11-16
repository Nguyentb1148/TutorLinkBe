using TutorLinkBe.Domain.Entities;

namespace TutorLinkBe.Application.Interfaces
{
    public interface IAccountRepository
    {
        Task<RefreshToken?> GetRefreshTokenByTokenAsync(string token);
        
        Task<RefreshToken?> GetActiveRefreshTokenByJwtIdAsync(string jwtId);
        Task CreateRefreshTokenAsync(RefreshToken refreshToken);
        Task RevokeRefreshTokenAsync(string token);
        Task RevokeAllUserTokensAsync(string userId);
        
        Task<RefreshToken?> GetActiveRefreshTokenByUserIdAsync(string userId);
        
        // to get all active token by user to show logged-in sessions for a user (mobile/web) and allow selective logout.
        //Task<IEnumerable<RefreshToken>> GetAllActiveRefreshTokensByUserIdAsync(string userId);
        //check revoked & expired
        //Task<bool> IsRefreshTokenValidAsync(string token);
        //implement refresh token rotation and want to link ReplacedByToken, more secure, especially for multi-device login or detecting token theft
        //Task ReplaceRefreshTokenAsync(string oldToken, RefreshToken newToken);
        //Periodic cleanup to delete old refresh tokens from DB.
        //Task CleanupExpiredTokensAsync();
        //to track token chains via ReplacedByToken
        //Task<RefreshToken?> GetRefreshTokenChainAsync(string token);
    }
}
