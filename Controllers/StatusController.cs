using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TutorLinkBe.Config;

namespace TutorLinkBe.Controllers;

//Provides application health and environment status information
[ApiController]
[Route("api/status/info")]
public class StatusController : ControllerBase
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly AppSettings _appSettings;
    
    //Initializes a new instance of the status Controller class
    public StatusController(IWebHostEnvironment webHostEnvironment, IOptions<AppSettings> appSettings)
    {
        _webHostEnvironment = webHostEnvironment;
        _appSettings = appSettings.Value;
    }
    //Returns a simple health status for the API
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<object> Get()
    {
        var payload = new
        {
            status = "OK",
            environment = _webHostEnvironment.EnvironmentName,
            timestamp = DateTime.UtcNow.ToString("O"),
            config = new
            {
                corsOrigins = _appSettings.CorsOrigins,
                jwtIssuer = _appSettings.Jwt.Issuer,
                jwtAudience = _appSettings.Jwt.Audience
            }
        };
        return Ok(payload);
    }
}