using DashBoard.Models.Dashboard.DashBoard.Models;
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
        [HttpGet("total")]
        public async Task<IActionResult> GetTotal()
        {
            var tickets = await _glpiService.GetTicketsAsync();
            var result = new DashboardSummary
            {
                Total = tickets.Count,
                New = tickets.Count(t => t.Status?.Id == 1),
                Processing = tickets.Count(t => t.Status?.Id == 2),
                Pending = tickets.Count(t => t.Status?.Id == 4),
                Solved = tickets.Count(t => t.Status?.Id == 5),
                Closed = tickets.Count(t => t.Status?.Id == 6)
            };
            return Ok(result);
        }
        [HttpGet("tickets/{id}")]
        public async Task<IActionResult> GetTicketById(int id)
        {
            var tickets = await _glpiService.GetTicketsAsync();
            var ticket = tickets.FirstOrDefault(t => t.Id == id);
            if (ticket == null)
                return NotFound(new
                {
                    message = $"Ticket with ID {id} was not found."
                });
            return Ok(ticket);
        }
        [HttpGet("tickets/user/{userId}")]
        public async Task<IActionResult> GetTicketsByUserId(int userId)
        {
            var tickets = await _glpiService.GetTicketsAsync();
            var userTickets = tickets
                .Where(t => t.Team != null &&
                            t.Team.Any(member => member.Id == userId))
                .ToList();
            return Ok(userTickets);
        }
        [HttpGet("tickets/status/{statusId}")]
        public async Task<IActionResult> GetTicketsByStatusId(int statusId)
        {
            var tickets = await _glpiService.GetTicketsAsync();
            var statusTickets = tickets
                .Where(t => t.Status?.Id == statusId)
                .ToList();
            return Ok(statusTickets);
        }
        [HttpGet("tickets/user/{userId}/totaldetails")]
        public async Task<IActionResult> GetTotalTicketsByUserId(int userId)
        {
            var tickets = await _glpiService.GetTicketsAsync();

            var userTickets = tickets
                .Where(t => t.Team != null &&
                            t.Team.Any(member => member.Id == userId))
                .ToList();

            var result = new
            {
                total = userTickets.Count,
                @new = userTickets.Count(t => t.Status?.Id == 1),
                processing = userTickets.Count(t => t.Status?.Id == 2),
                pending = userTickets.Count(t => t.Status?.Id == 4),
                solved = userTickets.Count(t => t.Status?.Id == 5),
                closed = userTickets.Count(t => t.Status?.Id == 6)
            };

            return Ok(result);
        }
    }
}
