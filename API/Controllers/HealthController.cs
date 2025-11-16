using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorLinkBe.Infrastructure.Persistence;
using TutorLinkBe.Application.Services;

namespace TutorLinkBe.API.Controllers;

[ApiController]
[Route("api/status")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public HealthController( AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    [HttpGet("db")]
    public async Task<IActionResult> CheckDb()
    {
        try
        {
            await _dbContext.Database.OpenConnectionAsync();
            await _dbContext.Database.CloseConnectionAsync();
            return Ok(new { status = "Supabase db connection opened." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { status = "Supabase ERROR", error = ex.Message });
        }
    }

}