using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorLinkBe.Context;
using TutorLinkBe.Services;

namespace TutorLinkBe.Controllers;

//Health endpoint that includes MongoDB connectivity status.
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
            return Ok(new { status = "Postgres OK" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { status = "Postgres ERROR", error = ex.Message });
        }
    }

}