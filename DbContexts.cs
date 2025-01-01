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
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<Follows> Follows { get; set; }
        public DbSet<Staff> Staff { get; set; }
        public DbSet<SupportTicket> SupportTickets { get; set; }
        public DbSet<StaffSession> StaffSessions { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Define composite primary key for Follows table
            modelBuilder.Entity<Follows>()
                .HasKey(f => new { f.FollowerId, f.FollowedId });

            // Configure the relationship for Follower
            modelBuilder.Entity<Follows>()
                .HasOne(f => f.Follower)
                .WithMany(f => f.Following)  // A User can follow many others
                .HasForeignKey(f => f.FollowerId)  // Explicitly define the foreign key
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes

            // Configure the relationship for Followed
            modelBuilder.Entity<Follows>()
                .HasOne(f => f.Followed)
                .WithMany(f => f.Followers)  // A User can have many Followers
                .HasForeignKey(f => f.FollowedId)  // Explicitly define the foreign key
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes
        }




    }

}

