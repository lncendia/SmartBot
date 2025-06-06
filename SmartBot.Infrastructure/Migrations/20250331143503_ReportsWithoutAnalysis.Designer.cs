﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartBot.Infrastructure.Contexts;

#nullable disable

namespace SmartBot.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20250331143503_ReportsWithoutAnalysis")]
    partial class ReportsWithoutAnalysis
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.3");

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

            modelBuilder.Entity("SmartBot.Abstractions.Models.Reports.Report", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Comment")
                        .HasMaxLength(1500)
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Date")
                        .HasColumnType("TEXT");

                    b.Property<long>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Reports", (string)null);
                });

            modelBuilder.Entity("SmartBot.Abstractions.Models.Users.User", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("CurrentReport")
                        .HasMaxLength(5000)
                        .HasColumnType("TEXT");

                    b.Property<string>("FullName")
                        .HasMaxLength(150)
                        .HasColumnType("TEXT");

                    b.Property<string>("Position")
                        .HasMaxLength(50)
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("RegistrationTime")
                        .HasColumnType("TEXT");

                    b.Property<int>("Role")
                        .HasColumnType("INTEGER");

                    b.Property<long?>("SelectedWorkingChatId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("State")
                        .HasColumnType("INTEGER");

                    b.Property<long?>("WorkingChatId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("SelectedWorkingChatId");

                    b.HasIndex("WorkingChatId");

                    b.ToTable("Users", (string)null);
                });

            modelBuilder.Entity("SmartBot.Abstractions.Models.WorkingChats.WorkingChat", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("WorkingChats", (string)null);
                });

            modelBuilder.Entity("SmartBot.Abstractions.Models.Exporter", b =>
                {
                    b.HasOne("SmartBot.Abstractions.Models.Reports.Report", null)
                        .WithMany()
                        .HasForeignKey("LastExportedReportId")
                        .OnDelete(DeleteBehavior.SetNull);
                });

            modelBuilder.Entity("SmartBot.Abstractions.Models.Reports.Report", b =>
                {
                    b.HasOne("SmartBot.Abstractions.Models.Users.User", "User")
                        .WithMany("Reports")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.OwnsOne("SmartBot.Abstractions.Models.Reports.UserReport", "EveningReport", b1 =>
                        {
                            b1.Property<Guid>("ReportId")
                                .HasColumnType("TEXT");

                            b1.Property<string>("Data")
                                .IsRequired()
                                .HasMaxLength(5000)
                                .HasColumnType("TEXT");

                            b1.Property<TimeSpan?>("Overdue")
                                .HasColumnType("TEXT");

                            b1.HasKey("ReportId");

                            b1.ToTable("Reports");

                            b1.WithOwner()
                                .HasForeignKey("ReportId");
                        });

                    b.OwnsOne("SmartBot.Abstractions.Models.Reports.UserReport", "MorningReport", b1 =>
                        {
                            b1.Property<Guid>("ReportId")
                                .HasColumnType("TEXT");

                            b1.Property<string>("Data")
                                .IsRequired()
                                .HasMaxLength(5000)
                                .HasColumnType("TEXT");

                            b1.Property<TimeSpan?>("Overdue")
                                .HasColumnType("TEXT");

                            b1.HasKey("ReportId");

                            b1.ToTable("Reports");

                            b1.WithOwner()
                                .HasForeignKey("ReportId");
                        });

                    b.Navigation("EveningReport");

                    b.Navigation("MorningReport")
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("SmartBot.Abstractions.Models.Users.User", b =>
                {
                    b.HasOne("SmartBot.Abstractions.Models.WorkingChats.WorkingChat", null)
                        .WithMany()
                        .HasForeignKey("SelectedWorkingChatId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.HasOne("SmartBot.Abstractions.Models.WorkingChats.WorkingChat", null)
                        .WithMany()
                        .HasForeignKey("WorkingChatId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.OwnsOne("SmartBot.Abstractions.Models.Users.AnswerFor", "AnswerFor", b1 =>
                        {
                            b1.Property<long>("UserId")
                                .HasColumnType("INTEGER");

                            b1.Property<bool>("EveningReport")
                                .HasColumnType("INTEGER");

                            b1.Property<string>("Message")
                                .IsRequired()
                                .HasMaxLength(2000)
                                .HasColumnType("TEXT");

                            b1.Property<Guid>("ReportId")
                                .HasColumnType("TEXT");

                            b1.Property<long>("ToUserId")
                                .HasColumnType("INTEGER");

                            b1.HasKey("UserId");

                            b1.HasIndex("ReportId");

                            b1.HasIndex("ToUserId");

                            b1.ToTable("AnswersFor", (string)null);

                            b1.HasOne("SmartBot.Abstractions.Models.Reports.Report", null)
                                .WithMany()
                                .HasForeignKey("ReportId")
                                .OnDelete(DeleteBehavior.Cascade)
                                .IsRequired();

                            b1.HasOne("SmartBot.Abstractions.Models.Users.User", null)
                                .WithMany()
                                .HasForeignKey("ToUserId")
                                .OnDelete(DeleteBehavior.Cascade)
                                .IsRequired();

                            b1.WithOwner()
                                .HasForeignKey("UserId");
                        });

                    b.OwnsOne("SmartBot.Abstractions.Models.Users.ReviewingReport", "ReviewingReport", b1 =>
                        {
                            b1.Property<long>("UserId")
                                .HasColumnType("INTEGER");

                            b1.Property<bool>("EveningReport")
                                .HasColumnType("INTEGER");

                            b1.Property<Guid>("ReportId")
                                .HasColumnType("TEXT");

                            b1.HasKey("UserId");

                            b1.HasIndex("ReportId");

                            b1.ToTable("ReviewingReports", (string)null);

                            b1.HasOne("SmartBot.Abstractions.Models.Reports.Report", null)
                                .WithMany()
                                .HasForeignKey("ReportId")
                                .OnDelete(DeleteBehavior.Cascade)
                                .IsRequired();

                            b1.WithOwner()
                                .HasForeignKey("UserId");
                        });

                    b.Navigation("AnswerFor");

                    b.Navigation("ReviewingReport");
                });

            modelBuilder.Entity("SmartBot.Abstractions.Models.Users.User", b =>
                {
                    b.Navigation("Reports");
                });
#pragma warning restore 612, 618
        }
    }
}
