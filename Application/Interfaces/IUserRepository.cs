using Microsoft.AspNetCore.Identity;
using TutorLinkBe.Application.DTOs;

namespace TutorLinkBe.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<IdentityResult> CreateUserWithRoleAsync(RegisterDto model, string roleName);
        Task<bool> ChangeUserRoleAsync(string userId, string newRole);
    }
}

