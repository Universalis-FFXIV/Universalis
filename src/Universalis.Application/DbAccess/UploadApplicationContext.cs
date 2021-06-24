using Microsoft.EntityFrameworkCore;
using Universalis.Entities.Uploaders;

namespace Universalis.Application.DbAccess
{
    public class UploadApplicationContext : DbContext
    {
        public DbSet<UploadApplication> Applications { get; set; }

        public UploadApplicationContext()
        {
        }

        public UploadApplicationContext(DbContextOptions<UploadApplicationContext> options) : base(options)
        {
        }
    }
}