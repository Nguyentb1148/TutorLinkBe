using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TutorLinkBe.Context;
using TutorLinkBe.Models;
using TutorLinkBe.Repository;

namespace TutorLinkBe.Controllers;

[ApiController]
[Route("api/[controller]")]
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

    [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost("update-role")]
    public async Task<IActionResult> UpdateRole([FromQuery] string email, [FromQuery] string newRole)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return NotFound("User not found.");

        if (!await _roleManager.RoleExistsAsync(newRole))
            await _roleManager.CreateAsync(new IdentityRole(newRole));

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, newRole);

        return Ok($"User {email} role updated to {newRole}.");
    }


    [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPut("tutor-request/{id}/approve")]
    public async Task<IActionResult> ApproveTutorRequest(Guid id)
    {
        _logger.LogInformation($"Approving tutor request with Id: {id}");
        
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminId))
        {
            _logger.LogWarning("No admin ID found in token claims.");
            return Unauthorized("Invalid admin token.");
        }
        // Add comprehensive debugging
        _logger.LogInformation("Admin ID from token: {AdminId}", adminId);
        _logger.LogInformation("Admin ID type: {Type}", adminId.GetType());

        // Check if the admin user exists in the database
        var adminUser = await _context.Users.FindAsync(adminId);
        if (adminUser == null)
        {
            _logger.LogError("Admin user with ID {AdminId} not found in database", adminId);
            // Try to find by email as fallback
            var adminEmail = User.FindFirstValue(ClaimTypes.Email);
            adminUser = await _userManager.FindByEmailAsync(adminEmail);
            if (adminUser != null)
            {
                _logger.LogInformation("Found admin by email: {Email}, ID: {Id}", adminEmail, adminUser.Id);
                adminId = adminUser.Id; // Update adminId to the correct one
            }
            else
            {
                return BadRequest("Admin user not found in database");
            }
        }
        else
        {
            _logger.LogInformation("Admin user found: {Email}, ID: {Id}", adminUser.Email, adminUser.Id);
        }

        // Verify the adminId is a valid GUID
        if (!Guid.TryParse(adminId, out var adminGuid))
        {
            _logger.LogError("Admin ID is not a valid GUID: {AdminId}", adminId);
            return BadRequest("Invalid admin ID format");
        }

        _logger.LogInformation("Using admin ID: {AdminId} (GUID: {Guid})", adminId, adminGuid);

        var request = await _context.TutorRequests.FindAsync(id);
        if (request == null)
        {
            _logger.LogWarning($"Tutor request {id} not found.");
            return NotFound("Request not found.");
        }
        
        var user = await _context.Users.FindAsync(request.UserId);
        if (user == null)
        {
            _logger.LogWarning($"User with ID {request.UserId} not found.");
            return NotFound("User not found.");
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var oldRole = currentRoles.FirstOrDefault() ?? "User";

        var changed = await _userRepository.ChangeUserRoleAsync(user.Id, "Teacher");
        if (!changed)
        {
            _logger.LogError($"Failed to update role for user {user.Email}");
            return StatusCode(500, "Failed to update user role.");
        }

        request.Status = TutorRequestStatus.Approved;
        request.UpdatedAt = DateTime.UtcNow;

        var history = new RoleHistory
        {
            UserId = user.Id,
            OldRole = oldRole,
            NewRole = "Teacher",
            ChangedBy = adminId,
            ChangedAt = DateTime.UtcNow
        };

        _context.RoleHistories.Add(history);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Tutor request {id} approved and role changed to Teacher.");
        return Ok("Teacher request approved.");
    }


}