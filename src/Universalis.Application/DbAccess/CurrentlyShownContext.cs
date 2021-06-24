using Microsoft.EntityFrameworkCore;
using Universalis.Entities.MarketBoard;

namespace Universalis.Application.DbAccess
{
    public class CurrentlyShownContext : DbContext
    {
        public DbSet<CurrentlyShown> CurrentlyShownData { get; set; }

        public CurrentlyShownContext()
        {
        }

        public CurrentlyShownContext(DbContextOptions<CurrentlyShownContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CurrentlyShown>()
                .HasKey(o => new {o.ItemId, o.WorldId});
        }
    }
}