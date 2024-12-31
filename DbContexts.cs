using Microsoft.EntityFrameworkCore;
using Cold_Storage_GO.Models;

namespace Cold_Storage_GO
{
    public class DbContexts(IConfiguration configuration) : DbContext
    {
        private readonly IConfiguration _configuration = configuration;
        protected override void OnConfiguring(DbContextOptionsBuilder
        optionsBuilder)
        {
            string? connectionString = _configuration.GetConnectionString(
            "MyConnection");
            if (connectionString != null)
            {
                optionsBuilder.UseMySQL(connectionString);
            }
        }
        public DbSet<User> Users { get; set; }
        public DbSet<UserAdministration> UserAdministration { get; set; }

        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<Session> Sessions { get; set; }
    }

}

