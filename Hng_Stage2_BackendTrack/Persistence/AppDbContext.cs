using Hng_Stage2_BackendTrack.Models;
using Microsoft.EntityFrameworkCore;

namespace Hng_Stage2_BackendTrack.Persistence
{
    public class AppDbContext: DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options):base(options)
        {
            
        }
        public DbSet<Country> Countries => Set<Country>();
    }
}
