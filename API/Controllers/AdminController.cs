using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TutorLinkBe.Domain.Context;
using TutorLinkBe.Domain.Models;
using TutorLinkBe.Application.Repository;

namespace TutorLinkBe.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class AdminController : ControllerBase
{
    private readonly ILogger<AdminController> _logger;
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserRepository _userRepository;

    public AdminController(ILogger<AdminController> logger, AppDbContext context, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole> roleManager, UserRepository userRepository)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _userRepository = userRepository;
    }

    [HttpPost("update-role")]
    public async Task<IActionResult> UpdateRole([FromQuery] string email, [FromQuery] string newRole)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return NotFound(new { sucess=false, message="User not found."});

        if (!await _roleManager.RoleExistsAsync(newRole))
            await _roleManager.CreateAsync(new IdentityRole(newRole));

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, newRole);

        return Ok(new { sucess=true, message=$"User {email} role updated to {newRole}."});
    }

    [HttpPut("tutor-request/{id}/approve")]
    public async Task<IActionResult> ApproveTutorRequest(Guid id)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminId))
            return Unauthorized(new { sucess=false, message="Invalid admin token."});
        
        // Check if the admin user exists in the database
        var adminUser = await _context.Users.FindAsync(adminId);
        if (adminUser == null) {
            // Try to find by email as fallback
            var adminEmail = User.FindFirstValue(ClaimTypes.Email);
            adminUser = await _userManager.FindByEmailAsync(adminEmail);
            if (adminUser != null)
                adminId = adminUser.Id;
            else
                return BadRequest(new { sucess=false, message="Admin user not found in database"});
        }

        // Verify the adminId is a valid GUID
        if (!Guid.TryParse(adminId, out var adminGuid))
            return BadRequest(new { sucess=false, message="Invalid admin ID format"});
        
        var request = await _context.TutorRequests.FindAsync(id);
        if (request == null)
            return NotFound(new { sucess=false, message="Request not found."});
        
        var user = await _context.Users.FindAsync(request.UserId);
        if (user == null)
            return NotFound(new { sucess=false, message="User not found."});
        
        var currentRoles = await _userManager.GetRolesAsync(user);
        var oldRole = currentRoles.FirstOrDefault() ?? "User";

        var changed = await _userRepository.ChangeUserRoleAsync(user.Id, "Teacher");
        if (!changed)
            return StatusCode(500,new { sucess=false, message= "Failed to update user role."});
        
        request.Status = TutorRequestStatus.Approved;
        request.UpdatedAt = DateTime.UtcNow;

        var history = new RoleHistory {
            UserId = user.Id,
            OldRole = oldRole,
            NewRole = "Teacher",
            ChangedBy = adminId,
            ChangedAt = DateTime.UtcNow
        };

        _context.RoleHistories.Add(history);
        await _context.SaveChangesAsync();

        return Ok(new { sucess=true, message="Teacher request approved."});
    }
}