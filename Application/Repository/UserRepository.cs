using TutorLinkBe.Domain.Models;
using TutorLinkBe.Application.Dto;
using Microsoft.AspNetCore.Identity;

namespace TutorLinkBe.Application.Repository
{
    public class UserRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private string _baseDir;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(UserManager<ApplicationUser> userManager, ILogger<UserRepository> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IdentityResult> CreateUserWithRoleAsync(RegisterDto model, string roleName)
        {
            var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, roleName);
            }

            return result;
        }

        public async Task<bool> ChangeUserRoleAsync(string userId, string newRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning($"User with ID {userId} not found.");
                return false;
            }

            var currentRoles = await _userManager.GetRolesAsync(user);

            // Remove existing roles if any
            if (currentRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    _logger.LogError(
                        $"Failed to remove existing roles from user {user.Email}: {string.Join(", ", removeResult.Errors.Select(e => e.Description))}");
                    return false;
                }
            }

            // Add new role
            var addResult = await _userManager.AddToRoleAsync(user, newRole);
            if (!addResult.Succeeded)
            {
                _logger.LogError(
                    $"Failed to assign new role {newRole} to user {user.Email}: {string.Join(", ", addResult.Errors.Select(e => e.Description))}");
                return false;
            }

            _logger.LogInformation($"User {user.Email} role successfully changed to {newRole}");
            return true;
        }
    }
}