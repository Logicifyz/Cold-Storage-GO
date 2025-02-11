﻿// <auto-generated />
using System;
using Cold_Storage_GO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Cold_Storage_GO.Migrations
{
    [DbContext(typeof(DbContexts))]
    partial class DbContextsModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("Cold_Storage_GO.Models.AIRecommendation", b =>
                {
                    b.Property<Guid>("ChatId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("varchar(1000)");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("datetime(6)");

                    b.Property<Guid>("UserId")
                        .HasColumnType("char(36)");

                    b.HasKey("ChatId");

                    b.ToTable("AIRecommendations");
                });

            modelBuilder.Entity("Cold_Storage_GO.Models.Article", b =>
                {
                    b.Property<Guid>("ArticleId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<string>("Category")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<bool>("Highlighted")
                        .HasColumnType("tinyint(1)");

                    b.Property<Guid?>("StaffId")
                        .HasColumnType("char(36)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("Views")
                        .HasColumnType("int");

                    b.HasKey("ArticleId");

                    b.ToTable("Articles");
                });

            modelBuilder.Entity("Cold_Storage_GO.Models.Comment", b =>
                {
                    b.Property<Guid>("CommentId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("varchar(500)");

                    b.Property<Guid?>("DiscussionId")
                        .HasColumnType("char(36)");

                    b.Property<int>("Downvotes")
                        .HasColumnType("int");

                    b.Property<Guid?>("ParentCommentId")
                        .HasColumnType("char(36)");

                    b.Property<string>("PostType")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<Guid?>("RecipeId")
                        .HasColumnType("char(36)");

                    b.Property<int>("Upvotes")
                        .HasColumnType("int");

                    b.Property<Guid>("UserId")
                        .HasColumnType("char(36)");

                    b.HasKey("CommentId");

                    b.HasIndex("ParentCommentId");

                    b.ToTable("Comments");
                });

            modelBuilder.Entity("Cold_Storage_GO.Models.Delivery", b =>
                {
                    b.Property<Guid>("DeliveryId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<DateTime?>("ConfirmedDeliveryDatetime")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("DeliveryDatetime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("DeliveryStatus")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<Guid>("OrderId")
                        .HasColumnType("char(36)");

                    b.HasKey("DeliveryId");

                    b.ToTable("Deliveries");
                });

            modelBuilder.Entity("Cold_Storage_GO.Models.Discussion", b =>
                {
                    b.Property<Guid>("DiscussionId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<string>("Category")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("varchar(1000)");

                    b.Property<int>("Downvotes")
                        .HasColumnType("int");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<int>("Upvotes")
                        .HasColumnType("int");

                    b.Property<Guid>("UserId")
                        .HasColumnType("char(36)");

                    b.Property<string>("Visibility")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("DiscussionId");

                    b.ToTable("Discussions");
                });

            modelBuilder.Entity("Cold_Storage_GO.Models.Dish", b =>
                {
                    b.Property<Guid>("DishId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<string>("Instructions")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("varchar(1000)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("char(36)");

                    b.HasKey("DishId");

                    b.ToTable("Dishes");
                });

            modelBuilder.Entity("Cold_Storage_GO.Models.Follows", b =>
                {
                    b.Property<Guid>("FollowerId")
                        .HasColumnType("char(36)")
                        .HasColumnOrder(0);

                    b.Property<Guid>("FollowedId")
                        .HasColumnType("char(36)")
                        .HasColumnOrder(1);

                    b.HasKey("FollowerId", "FollowedId");

                    b.HasIndex("FollowedId");

                    b.ToTable("Follows");
                });

            modelBuilder.Entity("Cold_Storage_GO.Models.MealKit", b =>
                {
                    b.Property<Guid>("MealKitId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime");

                    b.Property<string>("DishIdsSerialized")
                        .HasColumnType("longtext")
                        .HasColumnName("DishIds");

                    b.Property<DateTime>("ExpiryDate")
                        .HasColumnType("datetime");

                    b.Property<string>("Ingredients")
                        .HasColumnType("longtext");

                    b.Property<byte[]>("ListingImage")
                        .HasColumnType("longblob");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("Price")
                        .HasColumnType("int");

                    b.Property<string>("TagsSerialized")
                        .HasColumnType("longtext")
                        .HasColumnName("Tags");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime");

                    b.HasKey("MealKitId");

                    b.ToTable("MealKits");
                });

            modelBuilder.Entity("Cold_Storage_GO.Models.NutritionalFacts", b =>
                {
                    b.Property<Guid>("DishId")
                        .HasColumnType("char(36)");

                    b.Property<int>("Calories")
                        .HasColumnType("int");

                    b.Property<int>("Cholesterol")
                        .HasColumnType("int");

                    b.Property<string>("DietaryCategory")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<int>("DietaryFibre")
                        .HasColumnType("int");

                    b.Property<int>("Protein")
                        .HasColumnType("int");

                    b.Property<int>("SaturatedFat")
                        .HasColumnType("int");

                    b.Property<int>("Sodium")
                        .HasColumnType("int");

                    b.Property<int>("Sugar")
                        .HasColumnType("int");

                    b.Property<int>("TransFat")
                        .HasColumnType("int");

                    b.Property<string>("Vitamins")
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.HasKey("DishId");

                    b.ToTable("NutritionalFacts");
                });

            modelBuilder.Entity("Cold_Storage_GO.Models.Order", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<string>("DeliveryAddress")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("OrderStatus")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("OrderTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("OrderType")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime?>("ShipTime")
                        .HasColumnType("datetime(6)");

                    b.Property<decimal>("ShippingCost")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("Subtotal")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("Tax")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("TotalAmount")
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid>("UserId")
                        .HasColumnType("char(36)");

                    b.HasKey("Id");

                    b.ToTable("orders");
                });

            modelBuilder.Entity("Cold_Storage_GO.Models.OrderItem", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<Guid>("MealKitId")
                        .HasColumnType("char(36)");

                    b.Property<Guid>("OrderId")
                        .HasColumnType("char(36)");

                    b.Property<int>("Quantity")
                        .HasColumnType("int");

                    b.Property<decimal>("UnitPrice")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("Id");

                    b.HasIndex("MealKitId");

                    b.HasIndex("OrderId");

                    b.ToTable("orderitems");
                });

            modelBuilder.Entity("Cold_Storage_GO.Models.Recipe", b =>
                {
                    b.Property<Guid>("RecipeId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("varchar(500)");

                    b.Property<Guid>("DishId")
                        .HasColumnType("char(36)");

                    b.Property<int>("Downvotes")
                        .HasColumnType("int");

                    b.Property<string>("Ingredients")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("varchar(1000)");

                    b.Property<string>("Instructions")
                        .IsRequired()
                        .HasMaxLength(2000)
                        .HasColumnType("varchar(2000)");

                    b.Property<string>("MediaUrl")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<string>("Tags")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("varchar(200)");

                    b.Property<int>("TimeTaken")
                        .HasColumnType("int");

                    b.Property<int>("Upvotes")
                        .HasColumnType("int");

                    b.Property<Guid>("UserId")
                        .HasColumnType("char(36)");

                    b.Property<string>("Visibility")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("RecipeId");

                    b.ToTable("Recipes");
                });

            modelBuilder.Entity("Cold_Storage_GO.Models.Redemptions", b =>
                {
                    b.Property<Guid>("RedemptionId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<DateTime>("ExpiryDate")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("RedeemedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<Guid>("RewardId")
                        .HasColumnType("char(36)");

                    b.Property<bool>("RewardUsable")
                        .HasColumnType("tinyint(1)");

                    b.Property<Guid>("UserId")
                        .HasColumnType("char(36)");

                    b.HasKey("RedemptionId");

                    b.ToTable("Redemptions");
                });

            modelBuilder.Entity("Cold_Storage_GO.Models.Rewards", b =>
                {
                    b.Property<Guid>("RewardId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<string>("AvailabilityStatus")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<int>("CoinsCost")
                        .HasColumnType("int");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<DateTime>("ExpiryDate")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<string>("RewardType")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.HasKey("RewardId");

                    b.ToTable("Rewards");
                });

            modelBuilder.Entity("Cold_Storage_GO.Models.Staff", b =>
                {
                    b.Property<Guid>("StaffId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.Property<bool>("Status")
                        .HasColumnType("tinyint(1)");

                    b.HasKey("StaffId");

                    b.ToTable("Staff");
                });

            modelBuilder.Entity("Cold_Storage_GO.Models.StaffSession", b =>
                {
                    b.Property<string>("StaffSessionId")
                        .HasColumnType("varchar(255)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Data")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<bool>("IsActive")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime>("LastAccessed")
                        .HasColumnType("datetime(6)");

                    b.Property<Guid>("StaffId")
                        .HasColumnType("char(36)");

                    b.HasKey("StaffSessionId");

                    b.ToTable("StaffSessions");
                });

            modelBuilder.Entity("Cold_Storage_GO.Models.Subscription", b =>
                {
                    b.Property<Guid>("SubscriptionId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<bool?>("AutoRenewal")
                        .HasColumnType("tinyint(1)")
                        .HasDefaultValue(false);

                    b.Property<string>("DeliveryTimeSlot")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("EndDate")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("Frequency")
                        .HasColumnType("int");

                    b.Property<bool?>("IsFrozen")
                        .HasColumnType("tinyint(1)")
                        .HasDefaultValue(false);

                    b.Property<decimal>("Price")
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime>("StartDate")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<string>("StripeSessionId")
                        .HasColumnType("longtext");

                    b.Property<string>("SubscriptionChoice")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("SubscriptionType")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<Guid>("UserId")
                        .HasColumnType("char(36)");

                    b.HasKey("SubscriptionId");

                    b.HasIndex("Status");

                    b.HasIndex("UserId");

                    b.ToTable("Subscriptions");
                });

            modelBuilder.Entity("Cold_Storage_GO.Models.SupportTicket", b =>
                {
                    b.Property<Guid>("TicketId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<string>("Category")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Details")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Priority")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)");

                    b.Property<DateTime?>("ResolvedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<Guid?>("StaffId")
                        .HasColumnType("char(36)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)");

                    b.Property<string>("Subject")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("varchar(200)");

                    b.Property<Guid>("UserId")
                        .HasColumnType("char(36)");

                    b.HasKey("TicketId");

                    b.ToTable("SupportTickets");
                });

            modelBuilder.Entity("Cold_Storage_GO.Models.User", b =>
                {
                    b.Property<Guid>("UserId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("UserId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Cold_Storage_GO.Models.UserAdministration", b =>
                {
                    b.Property<Guid>("UserId")
                        .HasColumnType("char(36)");

                    b.Property<bool>("Activation")
                        .HasColumnType("tinyint(1)");

                    b.Property<int>("FailedLoginAttempts")
                        .HasColumnType("int");

                    b.Property<DateTime?>("LastFailedLogin")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("LockoutUntil")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("PasswordResetToken")
                        .HasColumnType("longtext");

                    b.Property<string>("VerificationToken")
                        .HasColumnType("longtext");

                    b.Property<bool>("Verified")
                        .HasColumnType("tinyint(1)");

                    b.HasKey("UserId");

                    b.ToTable("UserAdministration");
                });

            modelBuilder.Entity("Cold_Storage_GO.Models.UserProfile", b =>
                {
                    b.Property<int>("ProfileId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("FullName")
                        .HasColumnType("longtext");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("longtext");

                    b.Property<string>("PostalCode")
                        .HasColumnType("longtext");

                    b.Property<byte[]>("ProfilePicture")
                        .HasColumnType("longblob");

                    b.Property<string>("StreetAddress")
                        .HasColumnType("longtext");

                    b.Property<string>("SubscriptionStatus")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<Guid>("UserId")
                        .HasColumnType("char(36)");

                    b.HasKey("ProfileId");

                    b.HasIndex("UserId")
                        .IsUnique();

                    b.ToTable("UserProfiles");
                });

            modelBuilder.Entity("Cold_Storage_GO.Models.UserSession", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Data")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<bool>("IsActive")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime>("LastAccessed")
                        .HasColumnType("datetime(6)");

                    b.Property<Guid>("UserId")
                        .HasColumnType("char(36)");

                    b.Property<string>("UserSessionId")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.ToTable("UserSessions");
                });

            modelBuilder.Entity("Cold_Storage_GO.Models.Wallet", b =>
                {
                    b.Property<Guid>("WalletId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<int>("CoinsEarned")
                        .HasColumnType("int");

                    b.Property<int>("CoinsRedeemed")
                        .HasColumnType("int");

                    b.Property<Guid>("UserId")
                        .HasColumnType("char(36)");

                    b.HasKey("WalletId");

                    b.ToTable("Wallets");
                });

            modelBuilder.Entity("Cold_Storage_GO.Models.Comment", b =>
                {
                    b.HasOne("Cold_Storage_GO.Models.Comment", "ParentComment")
                        .WithMany("Replies")
                        .HasForeignKey("ParentCommentId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("ParentComment");
                });

            modelBuilder.Entity("Cold_Storage_GO.Models.Follows", b =>
                {
                    b.HasOne("Cold_Storage_GO.Models.User", "Followed")
                        .WithMany("Followers")
                        .HasForeignKey("FollowedId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Cold_Storage_GO.Models.User", "Follower")
                        .WithMany("Following")
                        .HasForeignKey("FollowerId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Followed");

                    b.Navigation("Follower");
                });

            modelBuilder.Entity("Cold_Storage_GO.Models.NutritionalFacts", b =>
                {
                    b.HasOne("Cold_Storage_GO.Models.Dish", "Dish")
                        .WithMany()
                        .HasForeignKey("DishId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Dish");
                });

            modelBuilder.Entity("Cold_Storage_GO.Models.OrderItem", b =>
                {
                    b.HasOne("Cold_Storage_GO.Models.MealKit", "MealKit")
                        .WithMany()
                        .HasForeignKey("MealKitId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Cold_Storage_GO.Models.Order", "Order")
                        .WithMany("OrderItems")
                        .HasForeignKey("OrderId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("MealKit");

                    b.Navigation("Order");
                });

            modelBuilder.Entity("Cold_Storage_GO.Models.Subscription", b =>
                {
                    b.HasOne("Cold_Storage_GO.Models.User", "User")
                        .WithMany("Subscriptions")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("Cold_Storage_GO.Models.UserAdministration", b =>
                {
                    b.HasOne("Cold_Storage_GO.Models.User", "User")
                        .WithOne("UserAdministration")
                        .HasForeignKey("Cold_Storage_GO.Models.UserAdministration", "UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("Cold_Storage_GO.Models.UserProfile", b =>
                {
                    b.HasOne("Cold_Storage_GO.Models.User", "User")
                        .WithOne("UserProfile")
                        .HasForeignKey("Cold_Storage_GO.Models.UserProfile", "UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("Cold_Storage_GO.Models.Comment", b =>
                {
                    b.Navigation("Replies");
                });

            modelBuilder.Entity("Cold_Storage_GO.Models.Order", b =>
                {
                    b.Navigation("OrderItems");
                });

            modelBuilder.Entity("Cold_Storage_GO.Models.User", b =>
                {
                    b.Navigation("Followers");

                    b.Navigation("Following");

                    b.Navigation("Subscriptions");

                    b.Navigation("UserAdministration")
                        .IsRequired();

                    b.Navigation("UserProfile")
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
