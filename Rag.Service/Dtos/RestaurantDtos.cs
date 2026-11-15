namespace Rag.Service.Dtos
{
    public class MenuItemDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = default!;
        public string Category { get; set; } = default!;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; }
    }

    public class RestaurantTableDto
    {
        public long Id { get; set; }
        public int Number { get; set; }
        public int Seats { get; set; }
        public string Location { get; set; } = default!;
    }

    public class BookingDto
    {
        public long Id { get; set; }
        public long TableId { get; set; }
        public int TableNumber { get; set; }
        public string TableLocation { get; set; } = string.Empty;
        public string CustomerName { get; set; } = default!;
        public string Phone { get; set; } = default!;
        public int Guests { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? Notes { get; set; }
        public string Status { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateBookingDto
    {
        public string CustomerName { get; set; } = default!;
        public string Phone { get; set; } = default!;
        public int Guests { get; set; }
        /// <summary>Restaurant-local start time.</summary>
        public DateTime StartTime { get; set; }
        public string? Notes { get; set; }
        /// <summary>Optional: a specific table number the guest asked for.</summary>
        public int? TableNumber { get; set; }
    }

    public class AvailabilityResultDto
    {
        public bool IsValidRequest { get; set; }
        public string? Message { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Guests { get; set; }
        public List<RestaurantTableDto> FreeTables { get; set; } = new();
    }

    public class BookingResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public BookingDto? Booking { get; set; }
        /// <summary>Filled when a cancel-by-phone matches several bookings.</summary>
        public List<BookingDto>? Candidates { get; set; }
    }
}
