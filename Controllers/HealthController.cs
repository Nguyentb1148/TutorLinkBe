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
    private readonly MongoDbService _mongoDbService;
    private readonly AppDbContext _dbContext;

    public HealthController(MongoDbService mongoDbService, AppDbContext dbContext)
    {
        _mongoDbService = mongoDbService;
        _dbContext = dbContext;
    }
    //returns overall health status and MongoDB connection state
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> Get()
    {
        var mongoConnected = await _mongoDbService.CheckConnectionAsync(HttpContext.RequestAborted);
        return Ok(new
        {
            statue = "OK",
            mongoConnected
        });
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