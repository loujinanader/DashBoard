using DashBoard.Service.DashboardServices;
using Microsoft.AspNetCore.Mvc;
namespace DashBoard.Controllers
{
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardServices _dashboardService;
        public DashboardController(IDashboardServices dashboardService)
        {
            _dashboardService = dashboardService;
        }
        [HttpGet("tickets")]
        public async Task<IActionResult> GetTickets([FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo)
        {
            var from = dateFrom?.ToDateTime(TimeOnly.MinValue);
            var to = dateTo?.ToDateTime(TimeOnly.MinValue);
            var tickets = await _dashboardService.GetTicketsAsync(from, to);
            return Ok(tickets);
        }
        [HttpGet("total")]
        public async Task<IActionResult> GetTotal([FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo)
        {
            var from = dateFrom?.ToDateTime(TimeOnly.MinValue);
            var to = dateTo?.ToDateTime(TimeOnly.MinValue);
            var summary = await _dashboardService.GetTotalAsync(from, to);
            return Ok(summary);
        }
        [HttpGet("tickets/{id}")]
        public async Task<IActionResult> GetTicketById(int id)
        {
            var ticket = await _dashboardService.GetTicketByIdAsync(id);
            if (ticket == null)
                return NotFound(new { message = $"Ticket with ID {id} was not found." });
            return Ok(ticket);
        }
        [HttpGet("tickets/user/{userId}")]
        public async Task<IActionResult> GetTicketsByUserId(int userId)
        {
            var tickets = await _dashboardService.GetTicketsByUserIdAsync(userId);
            return Ok(tickets);
        }
        [HttpGet("tickets/status/{statusId}")]
        public async Task<IActionResult> GetTicketsByStatusId(int statusId, [FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo)
        {
            var from = dateFrom?.ToDateTime(TimeOnly.MinValue);
            var to = dateTo?.ToDateTime(TimeOnly.MinValue);
            var tickets = await _dashboardService.GetTicketsByStatusIdAsync(statusId, from, to);
            return Ok(tickets);
        }
        [HttpGet("tickets/user/{userId}/totaldetails")]
        public async Task<IActionResult> GetTotalTicketsByUserId(int userId)
        {
            var result = await _dashboardService.GetTotalByUserIdAsync(userId);
            return Ok(result);
        }
        [HttpGet("tickets/users/totaldetails")]
        public async Task<IActionResult> GetTotalTicketsByAllUsers([FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo)
        {
            var from = dateFrom?.ToDateTime(TimeOnly.MinValue);
            var to = dateTo?.ToDateTime(TimeOnly.MinValue);
            var result = await _dashboardService.GetSummaryByAllUsersAsync(from, to);
            return Ok(result);
        }
        [HttpGet("tickets/locations/totaldetails")]
        public async Task<IActionResult> GetTotalTicketsByAllLocations([FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo)
        {
            var from = dateFrom?.ToDateTime(TimeOnly.MinValue);
            var to = dateTo?.ToDateTime(TimeOnly.MinValue);
            var result = await _dashboardService.GetSummaryByAllLocationsAsync(from, to);
            return Ok(result);
        }
        [HttpGet("tickets/types/totaldetails")]
        public async Task<IActionResult> GetTotalTicketsByType([FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo)
        {
            var from = dateFrom?.ToDateTime(TimeOnly.MinValue);
            var to = dateTo?.ToDateTime(TimeOnly.MinValue);
            var result = await _dashboardService.GetSummaryByTypeAsync(from, to);
            return Ok(result);
        }
        [HttpPost("sync")]
        public async Task<IActionResult> SyncTickets()
        {
            await _dashboardService.SyncTicketsAsync();
            return Ok(new { message = "Tickets synchronized successfully." });
        }
    }
}
