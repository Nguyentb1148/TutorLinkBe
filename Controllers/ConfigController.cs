using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TutorLinkBe.Config;

namespace TutorLinkBe.Controllers;

//Exposes configuration values for verification purposes
[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly AppSettings _appSettings;

    public ConfigController(IOptions<AppSettings> appSettingsOptions)
    {
        _appSettings = appSettingsOptions.Value;
    }
    //return the current application settings to verify configuration binding.
    //return "AppSettings" object.
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<AppSettings> Get()
    {
        return Ok(_appSettings);
    }
}