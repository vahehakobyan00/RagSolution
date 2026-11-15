using Rag.Service.Dtos;

namespace Rag.Service.Services.Interfaces
{
    public interface IRestaurantService
    {
        Task<List<MenuItemDto>> GetMenuAsync(string? category = null, bool onlyAvailable = false);
        Task<List<RestaurantTableDto>> GetTablesAsync();
        Task<List<BookingDto>> GetBookingsAsync(DateTime? from = null, DateTime? to = null, bool includeCancelled = false);
        /// <summary>Free tables for a 2h slot starting at <paramref name="start"/> (times are stored as given).</summary>
        Task<AvailabilityResultDto> CheckAvailabilityAsync(DateTime start, int guests);
        Task<BookingResultDto> CreateBookingAsync(CreateBookingDto dto);
        /// <summary>Cancel by id (phone, if given, must match) or by phone alone.</summary>
        Task<BookingResultDto> CancelBookingAsync(long? bookingId, string? phone);
    }
}
