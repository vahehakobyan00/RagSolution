using Rag.Data.Data;
using Rag.Data.Entity;
using Rag.Data.Repositories.Interfaces;

namespace Rag.Data.Repositories
{
    public class RestaurantTableRepository : BaseRepository<RestaurantTable>, IRestaurantTableRepository
    {
        public RestaurantTableRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
