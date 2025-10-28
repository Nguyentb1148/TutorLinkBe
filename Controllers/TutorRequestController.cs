using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorLinkBe.Context;
using TutorLinkBe.Models;
using Microsoft.EntityFrameworkCore;

namespace TutorLinkBe.Controllers;
[ApiController]
[Route("api/[controller]")]
public class TutorRequestController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<TutorRequestController> _logger;
    public TutorRequestController(AppDbContext context, ILogger<TutorRequestController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [Authorize(Roles = "User")]
    [HttpPost("apply")]
    public async Task<IActionResult> ApplyForTutor()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email))
        {
            return Unauthorized("Email not found in token");
        }
        // Find the user by email
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            _logger.LogError("User with email {Email} not found in database", email);
            return BadRequest("User not found in database");
        }

        var userId = user.Id;

        var existing = await _context.TutorRequests
            .FirstOrDefaultAsync(tr => tr.UserId == userId && tr.Status == TutorRequestStatus.Pending);
        if (existing != null)
        {
            _logger.LogWarning("email {email} already has a pending request", email);
            return BadRequest("You already have a pending request.");
        }

        var request = new TutorRequest
        {
            UserId = userId,
            Status = TutorRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Creating TutorRequest with User email: {email}", user.Email);

        _context.TutorRequests.Add(request);
        await _context.SaveChangesAsync();

        return Ok("Tutor request submitted successfully!");
    }
}