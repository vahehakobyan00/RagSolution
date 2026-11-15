using Microsoft.AspNetCore.Mvc;
using Rag.Service.Services.Interfaces;
using RagWebAPI.Attributes;

namespace RagWebAPI.Controllers
{
    /// <summary>Read-only view of restaurant data (handy in Swagger to see what the agent booked).</summary>
    [ApiController]
    [Route("restaurant")]
    [SkipUserIdHeader]
    public class RestaurantController : ControllerBase
    {
        private readonly IRestaurantService _restaurantService;

        public RestaurantController(IRestaurantService restaurantService)
        {
            _restaurantService = restaurantService;
        }

        [HttpGet("menu")]
        public async Task<IActionResult> GetMenu([FromQuery] string? category) =>
            Ok(await _restaurantService.GetMenuAsync(category));

        [HttpGet("tables")]
        public async Task<IActionResult> GetTables() =>
            Ok(await _restaurantService.GetTablesAsync());

        [HttpGet("bookings")]
        public async Task<IActionResult> GetBookings([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] bool includeCancelled = false) =>
            Ok(await _restaurantService.GetBookingsAsync(from, to, includeCancelled));

        /// <summary>Free tables for a 2h slot, e.g. ?start=2026-11-20T19:00&amp;guests=4</summary>
        [HttpGet("availability")]
        public async Task<IActionResult> GetAvailability([FromQuery] DateTime start, [FromQuery] int guests) =>
            Ok(await _restaurantService.CheckAvailabilityAsync(start, guests));
    }
}
