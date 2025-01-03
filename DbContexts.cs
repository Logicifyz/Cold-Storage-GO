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
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<NutritionalFacts>()
                .HasOne(nf => nf.Dish)
                .WithMany() // Optional: Specify if `Dish` has a navigation property
                .HasForeignKey(nf => nf.DishId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public DbSet<MealKit> MealKits { get; set; }
        public DbSet<Dish> Dishes { get; set; }
        public DbSet<NutritionalFacts> NutritionalFacts { get; set; }
        public DbSet<Rewards> Rewards { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<Redemptions> Redemptions { get; set; }
    }

}   
