using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rag.Data.Entity
{
    public enum BookingStatus
    {
        Confirmed,
        Cancelled
    }

    public class Booking
    {
        [Key]
        public long Id { get; set; }
        public long TableId { get; set; }
        [ForeignKey(nameof(TableId))]
        public RestaurantTable Table { get; set; } = default!;
        [MaxLength(200)]
        public string CustomerName { get; set; } = default!;
        [MaxLength(50)]
        public string Phone { get; set; } = default!;
        public int Guests { get; set; }
        /// <summary>Stored exactly as received (no time zone conversion).</summary>
        public DateTime StartTime { get; set; }
        /// <summary>Stored exactly as received (no time zone conversion).</summary>
        public DateTime EndTime { get; set; }
        public string? Notes { get; set; }
        /// <summary>Stored as text ("Confirmed"/"Cancelled").</summary>
        public BookingStatus Status { get; set; } = BookingStatus.Confirmed;
        /// <summary>Local machine time when the booking was created.</summary>
        public DateTime CreatedAt { get; set; }
    }
}
