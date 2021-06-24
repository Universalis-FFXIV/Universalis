using Microsoft.EntityFrameworkCore;
using Universalis.Entities.Uploaders;

namespace Universalis.Application.DbAccess
{
    public class AuthenticationInfoContext : DbContext
    {
        public DbSet<AuthenticationInfo> RegisteredClients { get; set; }

        public AuthenticationInfoContext()
        {
        }

        public AuthenticationInfoContext(DbContextOptions<AuthenticationInfoContext> options) : base(options)
        {
        }
    }
}