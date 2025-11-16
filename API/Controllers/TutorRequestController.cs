using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorLinkBe.Infrastructure.Persistence;
using TutorLinkBe.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace TutorLinkBe.API.Controllers;
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
            return Unauthorized( new {sucess= false, mesage="Email not found in token"});
        
        // Find the user by email
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            return BadRequest( new {sucess= false, mesage="User not found in database"});

        var userId = user.Id;

        var existing = await _context.TutorRequests
            .FirstOrDefaultAsync(tr => tr.UserId == userId && tr.Status == TutorRequestStatus.Pending);
        if (existing != null)
            return BadRequest( new {sucess= false, mesage="You already have a pending request."});

        var request = new TutorRequest {
            UserId = userId,
            Status = TutorRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _context.TutorRequests.Add(request);
        await _context.SaveChangesAsync();

        return Ok( new {sucess= true, mesage="Tutor request submitted successfully!"});
    }
}