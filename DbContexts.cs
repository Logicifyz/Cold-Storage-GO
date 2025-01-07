using Microsoft.EntityFrameworkCore;
using Cold_Storage_GO.Models;

namespace Cold_Storage_GO
{
    public class DbContexts : DbContext
    {
        private readonly IConfiguration _configuration;

        public DbContexts(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string? connectionString = _configuration.GetConnectionString("MyConnection");

            if (!string.IsNullOrEmpty(connectionString))
            {
                // ✅ Correct approach for MySql.EntityFrameworkCore
                optionsBuilder.UseMySQL(connectionString);
            }
        }

        public DbSet<Order> Orders { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Delivery> Deliveries { get; set; }
    }
}
