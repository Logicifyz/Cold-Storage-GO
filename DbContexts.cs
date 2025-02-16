using Microsoft.EntityFrameworkCore;
using Cold_Storage_GO.Models;
using System.Text.Json;

namespace Cold_Storage_GO
{
    public class DbContexts : DbContext
    {
        
        public DbContexts(DbContextOptions<DbContexts> options) : base(options)
        {
        }

        // Define DbSet properties
        public DbSet<User> Users { get; set; }
        public DbSet<UserAdministration> UserAdministration { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<Follows> Follows { get; set; }
        public DbSet<Staff> Staff { get; set; }
        public DbSet<SupportTicket> SupportTickets { get; set; }
        public DbSet<TicketImage> TicketImage { get; set; }
        public DbSet<StaffSession> StaffSessions { get; set; }
        public DbSet<Article> Articles { get; set; }
        public DbSet<Dish> Dishes { get; set; }
        public DbSet<NutritionalFacts> NutritionalFacts { get; set; }
        public DbSet<Rewards> Rewards { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<Redemptions> Redemptions { get; set; }
        public DbSet<MealKit> MealKits { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<CommentVote> CommentVotes { get; set; }
        public DbSet<Discussion> Discussions { get; set; }
        public DbSet<DiscussionImage> DiscussionImages { get; set; }
        public DbSet<DiscussionVote> DiscussionVotes { get; set; }
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<RecipeIngredient> RecipeIngredients { get; set; }
        public DbSet<RecipeInstruction> RecipeInstructions { get; set; }
        public DbSet<RecipeImage> RecipeImages { get; set; }
        public DbSet<RecipeVote> RecipeVotes { get; set; }
        public DbSet<AIResponseLog> AIResponseLogs { get; set; }
        public DbSet<UserRecipeRequest> UserRecipeRequests { get; set; }
        public DbSet<AIRecipeRequest> AIRecipeRequests { get; set; }
        public DbSet<FinalDish> FinalDishes { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Delivery> Deliveries { get; set; }
        public DbSet<CartEvent> CartEvents { get; set; }
        public DbSet<OrderEvent> OrderEvents { get; set; }
        public DbSet<RewardRedemptionEvent> RewardRedemptionEvents { get; set; }
        public DbSet<SupportTicketEvent> SupportTicketEvents { get; set; }
        public DbSet<SubscriptionEvent> SubscriptionEvents { get; set; }
        public DbSet<SubscriptionFreezeHistory> SubscriptionFreezeHistories { get; set; }
        public DbSet<ScheduledFreeze> ScheduledFreezes { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
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
                .OnDelete(DeleteBehavior.Cascade);  // Change Restrict to Cascade

            modelBuilder.Entity<Follows>()
                .HasOne(f => f.Followed)
                .WithMany(f => f.Followers)
                .HasForeignKey(f => f.FollowedId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure NutritionalFacts relationships
            modelBuilder.Entity<NutritionalFacts>()
                .HasOne(nf => nf.Dish)
                .WithMany()
                .HasForeignKey(nf => nf.DishId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-Many Relationship for Subscriptions
            modelBuilder.Entity<User>()
                .HasMany(u => u.Subscriptions)
                .WithOne(s => s.User)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Set default values and constraints for Subscription
            modelBuilder.Entity<Subscription>()
                .Property(s => s.IsFrozen)
                .HasDefaultValue(false)
                .ValueGeneratedNever();

            modelBuilder.Entity<Subscription>()
                .Property(s => s.AutoRenewal)
                .HasDefaultValue(false)
                .ValueGeneratedNever();

            modelBuilder.Entity<Subscription>()
                .HasIndex(s => s.Status);

            modelBuilder.Entity<Recipe>()
                .HasMany(r => r.Ingredients)
                .WithOne(i => i.Recipe)
                .HasForeignKey(i => i.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Recipe>()
                .HasMany(r => r.Instructions)
                .WithOne(instr => instr.Recipe)
                .HasForeignKey(instr => instr.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Recipe>()
                .HasMany(r => r.CoverImages)
                .WithOne(img => img.Recipe)
                .HasForeignKey(img => img.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RecipeVote>()
                .HasOne(rv => rv.Recipe)
                .WithMany(r => r.Votes)
                .HasForeignKey(rv => rv.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);  

            modelBuilder.Entity<RecipeVote>()
                .HasIndex(rv => new { rv.RecipeId, rv.UserId })
                .IsUnique();

            modelBuilder.Entity<Discussion>()
                .HasMany(d => d.CoverImages)
                .WithOne(img => img.Discussion)
                .HasForeignKey(img => img.DiscussionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DiscussionVote>()
                .HasOne(dv => dv.Discussion)
                .WithMany(d => d.Votes)
                .HasForeignKey(dv => dv.DiscussionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DiscussionVote>()
                .HasIndex(dv => new { dv.DiscussionId, dv.UserId })
                .IsUnique();


            modelBuilder.Entity<Comment>()
                .HasOne(c => c.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CommentVote>()
                .HasIndex(cv => new { cv.CommentId, cv.UserId })
                .IsUnique();


            // AI DB CONFIG
            // ?? Mark UserRecipeRequest as Keyless (Not stored in DB)
            modelBuilder.Entity<UserRecipeRequest>().HasNoKey();

            // ?? Ensure FinalDish links to User properly
            modelBuilder.Entity<FinalDish>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(fd => fd.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ?? Ensure AIResponseLog is correctly linked to User
            modelBuilder.Entity<AIResponseLog>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(ai => ai.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ?? Ensure AIRecipeRequest is linked to User
            modelBuilder.Entity<AIRecipeRequest>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ?? AIResponseLog ? FinalDish (One-to-Many)
            modelBuilder.Entity<AIResponseLog>()
                .HasOne<FinalDish>()
                .WithMany()
                .HasForeignKey(ai => ai.FinalRecipeId)
                .OnDelete(DeleteBehavior.SetNull);

            // ?? Ensure ResponseType in AIResponseLog is stored as a string instead of an int
            modelBuilder.Entity<AIResponseLog>()
                .Property(a => a.Type)
                .HasConversion(
                    v => v.ToString(),
                    v => (ResponseType)Enum.Parse(typeof(ResponseType), v)
                );

            // ?? Ensure List<string> is stored as JSON
            modelBuilder.Entity<FinalDish>()
                .Property(d => d.Ingredients)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null)
                );

            modelBuilder.Entity<FinalDish>()
                .Property(d => d.Steps)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null)
                );

            modelBuilder.Entity<FinalDish>()
                .Property(d => d.Tags)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null)
                );

            modelBuilder.Entity<AIRecipeRequest>()
                .Property(a => a.Ingredients)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null)
                );

            modelBuilder.Entity<AIRecipeRequest>()
                .Property(a => a.ExcludeIngredients)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null)
                );

            modelBuilder.Entity<AIRecipeRequest>()
                .Property(a => a.DietaryPreferences)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null)
                );

            // ?? Ensure NutritionInfo is embedded, not a separate table
            modelBuilder.Entity<FinalDish>()
                .OwnsOne(d => d.Nutrition);
        }
    }
}
