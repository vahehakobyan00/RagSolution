using System.ComponentModel.DataAnnotations;

namespace Rag.Data.Entity
{
    public class RestaurantTable
    {
        [Key]
        public long Id { get; set; }
        public int Number { get; set; }
        public int Seats { get; set; }
        /// <summary>"hall", "terrace" or "VIP".</summary>
        [MaxLength(50)]
        public string Location { get; set; } = default!;
        public List<Booking> Bookings { get; set; } = new();
    }
}
