using Microsoft.AspNetCore.Mvc;
using TutorLinkBe.Services;

namespace TutorLinkBe.Controllers;

//Health endpoint that includes MongoDB connectivity status.
[ApiController]
[Route("api/status")]
public class HealthController : ControllerBase
{
    private readonly MongoDbService _mongoDbService;

    public HealthController(MongoDbService mongoDbService)
    {
        _mongoDbService = mongoDbService;
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
}