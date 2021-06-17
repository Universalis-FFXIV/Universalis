using Microsoft.EntityFrameworkCore;
using Universalis.Entities.MarketBoard;

namespace Universalis.Application.DbAccess
{
    public class CurrentlyShownContext : DbContext
    {
        public DbSet<CurrentlyShown> CurrentlyShownData { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase("Test");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CurrentlyShown>()
                .HasKey(o => new {o.ItemId, o.WorldId});
        }
    }
}