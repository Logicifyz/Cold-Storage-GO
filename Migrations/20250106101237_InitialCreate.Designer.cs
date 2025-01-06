﻿// <auto-generated />
using System;
using Cold_Storage_GO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Cold_Storage_GO.Migrations
{
    [DbContext(typeof(DbContexts))]
    [Migration("20250106101237_InitialCreate")]
    partial class InitialCreate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("Cold_Storage_GO.Models.AIRecommendation", b =>
                {
                    b.Property<Guid>("ChatId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("datetime(6)");

                    b.Property<Guid>("UserId")
                        .HasColumnType("char(36)");

                    b.HasKey("ChatId");

                    b.ToTable("AIRecommendations");
                });

            modelBuilder.Entity("Cold_Storage_GO.Models.Comment", b =>
                {
                    b.Property<Guid>("CommentId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<Guid?>("DiscussionId")
                        .HasColumnType("char(36)");

                    b.Property<int>("Downvotes")
                        .HasColumnType("int");

                    b.Property<Guid?>("ParentCommentId")
                        .HasColumnType("char(36)");

                    b.Property<string>("PostType")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<Guid>("RecipeId")
                        .HasColumnType("char(36)");

                    b.Property<int>("Upvotes")
                        .HasColumnType("int");

                    b.Property<Guid>("UserId")
                        .HasColumnType("char(36)");

                    b.HasKey("CommentId");

                    b.HasIndex("ParentCommentId");

                    b.ToTable("Comments");
                });

            modelBuilder.Entity("Cold_Storage_GO.Models.Discussion", b =>
                {
                    b.Property<Guid>("DiscussionId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<string>("Category")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("Downvotes")
                        .HasColumnType("int");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("longtext");

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

            modelBuilder.Entity("Cold_Storage_GO.Models.Recipe", b =>
                {
                    b.Property<Guid>("RecipeId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<Guid>("DishId")
                        .HasColumnType("char(36)");

                    b.Property<int>("Downvotes")
                        .HasColumnType("int");

                    b.Property<string>("Ingredients")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Instructions")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("MediaUrl")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Tags")
                        .IsRequired()
                        .HasColumnType("longtext");

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

            modelBuilder.Entity("Cold_Storage_GO.Models.Comment", b =>
                {
                    b.HasOne("Cold_Storage_GO.Models.Comment", "ParentComment")
                        .WithMany("Replies")
                        .HasForeignKey("ParentCommentId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("ParentComment");
                });

            modelBuilder.Entity("Cold_Storage_GO.Models.Comment", b =>
                {
                    b.Navigation("Replies");
                });
#pragma warning restore 612, 618
        }
    }
}
