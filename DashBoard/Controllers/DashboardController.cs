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
        public async Task<IActionResult> GetTickets()
        {
            var tickets = await _dashboardService.GetTicketsAsync();
            return Ok(tickets);
        }
        [HttpGet("total")]
        public async Task<IActionResult> GetTotal()
        {
            var summary = await _dashboardService.GetTotalAsync();
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
        public async Task<IActionResult> GetTicketsByStatusId(int statusId)
        {
            var tickets = await _dashboardService.GetTicketsByStatusIdAsync(statusId);
            return Ok(tickets);
        }
        [HttpGet("tickets/user/{userId}/totaldetails")]
        public async Task<IActionResult> GetTotalTicketsByUserId(int userId)
        {
            var result = await _dashboardService.GetTotalByUserIdAsync(userId);
            return Ok(result);
        }
        [HttpGet("tickets/users/totaldetails")]
        public async Task<IActionResult> GetTotalTicketsByAllUsers()
        {
            var result = await _dashboardService.GetSummaryByAllUsersAsync();
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
