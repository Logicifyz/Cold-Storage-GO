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
    [Migration("20241231113439_UpdateNutrition")]
    partial class UpdateNutrition
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("Cold_Storage_GO.Models.Dish", b =>
                {
                    b.Property<Guid>("DishId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<string>("Ingredients")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<string>("Instructions")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<Guid?>("MealKitId")
                        .HasColumnType("char(36)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<string>("Tags")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("char(36)");

                    b.HasKey("DishId");

                    b.ToTable("Dishes");
                });

            modelBuilder.Entity("Cold_Storage_GO.Models.MealKit", b =>
                {
                    b.Property<Guid>("MealKitId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime");

                    b.Property<Guid>("DishId")
                        .HasColumnType("char(36)");

                    b.Property<DateTime>("ExpiryDate")
                        .HasColumnType("datetime");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<int>("Price")
                        .HasColumnType("int");

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

                    b.Property<string>("Ingredients")
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

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

            modelBuilder.Entity("Cold_Storage_GO.Models.NutritionalFacts", b =>
                {
                    b.HasOne("Cold_Storage_GO.Models.Dish", "Dish")
                        .WithMany()
                        .HasForeignKey("DishId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Dish");
                });
#pragma warning restore 612, 618
        }
    }
}
