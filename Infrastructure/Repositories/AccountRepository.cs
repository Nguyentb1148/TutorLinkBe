using Microsoft.EntityFrameworkCore;
using TutorLinkBe.Application.Interfaces;
using TutorLinkBe.Domain.Entities;
using TutorLinkBe.Infrastructure.Persistence;

namespace TutorLinkBe.Infrastructure.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AccountRepository> _logger;

        public AccountRepository(AppDbContext context, ILogger<AccountRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<RefreshToken?> GetRefreshTokenByTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            return await _context.RefreshTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(rt => rt.Token == token && !rt.IsRevoked);
        }

        public async Task<RefreshToken?> GetActiveRefreshTokenByUserIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return null;

            return await _context.RefreshTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresUtc > DateTime.UtcNow);
        }

        public async Task<RefreshToken?> GetActiveRefreshTokenByJwtIdAsync(string jwtId)
        {
            if (string.IsNullOrWhiteSpace(jwtId))
                return null;

            return await _context.RefreshTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(rt => rt.JwtId == jwtId && !rt.IsRevoked && rt.ExpiresUtc > DateTime.UtcNow);
        }

        public async Task CreateRefreshTokenAsync(RefreshToken refreshToken)
        {
            if (refreshToken == null)
                throw new ArgumentNullException(nameof(refreshToken));

            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();
            
            _logger.LogDebug("Refresh token created for user: {UserId}", refreshToken.UserId);
        }

        public async Task RevokeRefreshTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return;

            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == token);

            if (refreshToken != null)
            {
                refreshToken.IsRevoked = true;
                refreshToken.ReplacedByToken = null;
                _context.RefreshTokens.Update(refreshToken);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Refresh token revoked: {TokenId} for user: {UserId}", 
                    refreshToken.Id, refreshToken.UserId);
            }
        }

        public async Task RevokeAllUserTokensAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return;

            var activeTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                .ToListAsync();

            if (activeTokens.Any())
            {
                foreach (var token in activeTokens)
                {
                    token.IsRevoked = true;
                }

                _context.RefreshTokens.UpdateRange(activeTokens);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("All refresh tokens revoked for user: {UserId}, Count: {Count}", 
                    userId, activeTokens.Count);
            }
        }
    }
}