using DashBoard.Service;
using Microsoft.AspNetCore.Mvc;

namespace DashBoard.Controllers
{
    [Route("auth/glpi")]
    public class GlpiAuthController : ControllerBase
    {
        private readonly IGLPIService _glpiService;

        public GlpiAuthController(IGLPIService glpiService)
        {
            _glpiService = glpiService;
        }

        // One-time step: open this in a browser and log in/consent in GLPI.
        [HttpGet("login")]
        public IActionResult Login()
        {
            return Redirect(_glpiService.GetAuthorizationUrl());
        }

        // GLPI redirects here (must match GLPI:RedirectUri) with ?code=...
        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest("Missing authorization code.");

            await _glpiService.ExchangeAuthorizationCodeAsync(code);

            return Ok("GLPI authorization complete. You can close this tab.");
        }
    }
}
