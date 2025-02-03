﻿using Microsoft.EntityFrameworkCore;
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
            if (connectionString != null)
            {
                optionsBuilder.UseMySQL(connectionString); // Ensure MySQL package is installed.
            }
        }

        // Define DbSet properties
        public DbSet<User> Users { get; set; }
        public DbSet<UserAdministration> UserAdministration { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<Follows> Follows { get; set; }
        public DbSet<Staff> Staff { get; set; }
        public DbSet<SupportTicket> SupportTickets { get; set; }
        public DbSet<StaffSession> StaffSessions { get; set; }
        public DbSet<Article> Articles { get; set; }
  
        public DbSet<Dish> Dishes { get; set; }
        public DbSet<NutritionalFacts> NutritionalFacts { get; set; }
        public DbSet<Rewards> Rewards { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<Redemptions> Redemptions { get; set; }
        public DbSet<MealKit> MealKits { get; set; }

        // Newly added models
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Discussion> Discussions { get; set; }
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<AIRecommendation> AIRecommendations { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Delivery> Deliveries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Define composite primary key for Follows table
            modelBuilder.Entity<Follows>()
                .HasKey(f => new { f.FollowerId, f.FollowedId });

            // Configure Follows relationships
            modelBuilder.Entity<Follows>()
                .HasOne(f => f.Follower)
                .WithMany(f => f.Following)
                .HasForeignKey(f => f.FollowerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Follows>()
                .HasOne(f => f.Followed)
                .WithMany(f => f.Followers)
                .HasForeignKey(f => f.FollowedId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure NutritionalFacts relationships
            modelBuilder.Entity<NutritionalFacts>()
                .HasOne(nf => nf.Dish)
                .WithMany()
                .HasForeignKey(nf => nf.DishId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Comment threading
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);

            // ✅ One-to-One Relationship with Proper Null Handling
            modelBuilder.Entity<User>()
                .HasMany(u => u.Subscriptions) // ✅ Change to One-to-Many
                .WithOne(s => s.User)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ✅ Set default values and constraints
            modelBuilder.Entity<Subscription>()
                .Property(s => s.IsFrozen)
                .HasDefaultValue(false)
                .ValueGeneratedNever(); // ✅ Prevents overriding default

            modelBuilder.Entity<Subscription>()
                .Property(s => s.AutoRenewal)
                .HasDefaultValue(false)
                .ValueGeneratedNever();

            modelBuilder.Entity<Subscription>()
                .HasIndex(s => s.Status);

        }
    }
}
