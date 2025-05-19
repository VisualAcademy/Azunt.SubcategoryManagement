using Microsoft.EntityFrameworkCore;

namespace Azunt.SubcategoryManagement
{
    public class SubcategoryDbContext : DbContext
    {
        public SubcategoryDbContext(DbContextOptions<SubcategoryDbContext> options)
            : base(options)
        {
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Subcategory>()
                .Property(m => m.Created)
                .HasDefaultValueSql("GetDate()");
        }

        public DbSet<Subcategory> Subcategories { get; set; } = null!;
    }
}