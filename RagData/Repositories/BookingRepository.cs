using Rag.Data.Data;
using Rag.Data.Entity;
using Rag.Data.Repositories.Interfaces;

namespace Rag.Data.Repositories
{
    public class BookingRepository : BaseRepository<Booking>, IBookingRepository
    {
        public BookingRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
