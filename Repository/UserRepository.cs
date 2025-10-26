using TutorLinkBe.Models;
using TutorLinkBe.Dto;
using Microsoft.AspNetCore.Identity;

namespace TutorLinkBe.Repository
{
    public class UserRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private string _baseDir;
        private readonly ILogger<UserRepository> _logger;

    public UserRepository(UserManager<ApplicationUser> userManager,ILogger<UserRepository> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }
    public async Task<IdentityResult> CreateUserWithRoleAsync(RegisterDto model, string roleName)
    {
        var user = new ApplicationUser { UserName = model.Email, Email = model.Email};
        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, roleName);
        }

        return result;
    }

    public async Task<IdentityResult> CreateUserWithRoleAdminAsync(RegisterDto model)
    {
        return await CreateUserWithRoleAsync(model, "Admin");
    }

    public async Task<IdentityResult> CreateUserWithRoleTeacherAsync(RegisterDto model)
    {
        return await CreateUserWithRoleAsync(model, "Teacher");
    }

    }
}