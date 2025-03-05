﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartBot.Infrastructure.Contexts;

#nullable disable

namespace SmartBot.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.2");

            modelBuilder.Entity("SmartBot.Abstractions.Models.Exporter", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("LastExportedReportId")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("LastExportingDate")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("LastExportedReportId");

                    b.ToTable("Exporter", (string)null);
                });

            modelBuilder.Entity("SmartBot.Abstractions.Models.Report", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Comment")
                        .HasMaxLength(1500)
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Date")
                        .HasColumnType("TEXT");

                    b.Property<string>("EveningReport")
                        .HasMaxLength(5000)
                        .HasColumnType("TEXT");

                    b.Property<string>("MorningReport")
                        .HasMaxLength(5000)
                        .HasColumnType("TEXT");

                    b.Property<long>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Reports", (string)null);
                });

            modelBuilder.Entity("SmartBot.Abstractions.Models.User", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("FullName")
                        .HasMaxLength(150)
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsExaminer")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Position")
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("RegistrationTime")
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("ReviewingReportId")
                        .HasColumnType("TEXT");

                    b.Property<int>("State")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("ReviewingReportId");

                    b.ToTable("Users", (string)null);
                });

            modelBuilder.Entity("SmartBot.Abstractions.Models.Exporter", b =>
                {
                    b.HasOne("SmartBot.Abstractions.Models.Report", null)
                        .WithMany()
                        .HasForeignKey("LastExportedReportId")
                        .OnDelete(DeleteBehavior.SetNull);
                });

            modelBuilder.Entity("SmartBot.Abstractions.Models.Report", b =>
                {
                    b.HasOne("SmartBot.Abstractions.Models.User", "User")
                        .WithMany("Reports")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("SmartBot.Abstractions.Models.User", b =>
                {
                    b.HasOne("SmartBot.Abstractions.Models.Report", null)
                        .WithMany()
                        .HasForeignKey("ReviewingReportId")
                        .OnDelete(DeleteBehavior.SetNull);
                });

            modelBuilder.Entity("SmartBot.Abstractions.Models.User", b =>
                {
                    b.Navigation("Reports");
                });
#pragma warning restore 612, 618
        }
    }
}
