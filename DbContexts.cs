using Microsoft.EntityFrameworkCore;

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
                    optionsBuilder.UseSqlServer(connectionString);
                }
            }
    }
    
}
