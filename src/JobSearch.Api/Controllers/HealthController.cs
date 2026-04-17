using JobSearch.Api.Responses;
using Microsoft.AspNetCore.Mvc;

namespace JobSearch.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ServiceStatusResponse>(StatusCodes.Status200OK)]
    public ActionResult<ServiceStatusResponse> Get()
    {
        return Ok(new ServiceStatusResponse("Healthy", DateTimeOffset.UtcNow));
    }
}
