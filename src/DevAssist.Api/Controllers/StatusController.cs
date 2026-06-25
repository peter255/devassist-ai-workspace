using Microsoft.AspNetCore.Mvc;

namespace DevAssist.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class StatusController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() =>
        Ok(new
        {
            Service = "DevAssist AI Workspace API",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            UtcNow = DateTimeOffset.UtcNow
        });
}
