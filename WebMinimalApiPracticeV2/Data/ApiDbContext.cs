using Microsoft.EntityFrameworkCore;
using WebMinimalApiPracticeV2.Data.Entities;

namespace WebMinimalApiPracticeV2.Data
{
    public class ApiDbContext : DbContext
    {
        public DbSet<AuthorEntity> Authors { get; set; }
        public DbSet<BookEntity> Books { get; set; }

        public ApiDbContext(DbContextOptions<ApiDbContext> options)
            : base(options)
        {

        }
    }
}
