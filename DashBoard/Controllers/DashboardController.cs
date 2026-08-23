using DashBoard.Service;
using Microsoft.AspNetCore.Mvc;

namespace DashBoard.Controllers
{
    public class DashboardController : ControllerBase
    {
        private readonly IGLPIService _glpiService;

        public DashboardController(IGLPIService glpiService)
        {
            _glpiService = glpiService;
        }

        [HttpGet("tickets")]
        public async Task<IActionResult> GetTickets()
        {
            var tickets = await _glpiService.GetTicketsAsync();
            return Ok(tickets);
        }
    }
}
