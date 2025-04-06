﻿// <auto-generated />
using System;
using JustDanceNextPlus.JustDanceClasses.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace JustDanceNextPlus.Migrations
{
    [DbContext(typeof(UserDataContext))]
    partial class UserDataContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.3")
                .HasAnnotation("Proxies:ChangeTracking", false)
                .HasAnnotation("Proxies:CheckEquality", false)
                .HasAnnotation("Proxies:LazyLoading", true);

            modelBuilder.Entity("JustDanceNextPlus.JustDanceClasses.Database.Profile.BossMode", b =>
                {
                    b.Property<Guid>("BossId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.HasKey("BossId");

                    b.ToTable("BossMode");
                });

            modelBuilder.Entity("JustDanceNextPlus.JustDanceClasses.Database.Profile.BossModeStats", b =>
                {
                    b.Property<Guid>("BossStatsId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("BossModeBossId")
                        .HasColumnType("TEXT");

                    b.HasKey("BossStatsId");

                    b.HasIndex("BossModeBossId");

                    b.ToTable("BossStats");
                });

            modelBuilder.Entity("JustDanceNextPlus.JustDanceClasses.Database.Profile.CompletedTask", b =>
                {
                    b.Property<Guid>("TaskId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CompletedAt")
                        .HasColumnType("TEXT");

                    b.HasKey("TaskId");

                    b.ToTable("CompletedTasks");
                });

            modelBuilder.Entity("JustDanceNextPlus.JustDanceClasses.Database.Profile.DancerCard", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("AliasGender")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("AliasId")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("AvatarId")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("BackgroundId")
                        .HasColumnType("TEXT");

                    b.PrimitiveCollection<string>("BadgesIds")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Country")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("PortraitBorderId")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("ScoringFxId")
                        .HasColumnType("TEXT");

                    b.PrimitiveCollection<string>("StickersIds")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("VictoryFxId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("DancerCard");
                });

            modelBuilder.Entity("JustDanceNextPlus.JustDanceClasses.Database.Profile.MapStats", b =>
                {
                    b.Property<Guid>("MapId")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("ProfileId")
                        .HasColumnType("TEXT");

                    b.Property<int>("HighScore")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Platform")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("PlayCount")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("TEXT");

                    b.HasKey("MapId", "ProfileId");

                    b.ToTable("HighScores");
                });

            modelBuilder.Entity("JustDanceNextPlus.JustDanceClasses.Database.Profile.ObjectiveCompletionData", b =>
                {
                    b.Property<Guid>("ObjectiveId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CompletedAt")
                        .HasColumnType("TEXT");

                    b.HasKey("ObjectiveId");

                    b.ToTable("ObjectiveCompletionData");
                });

            modelBuilder.Entity("JustDanceNextPlus.JustDanceClasses.Database.Profile.PlaylistStats", b =>
                {
                    b.Property<Guid>("PlaylistId")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("ProfileId")
                        .HasColumnType("TEXT");

                    b.Property<int>("HighScore")
                        .HasColumnType("INTEGER");

                    b.Property<string>("HighScorePerMap")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Platform")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("PlayCount")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("TEXT");

                    b.HasKey("PlaylistId", "ProfileId");

                    b.ToTable("PlaylistHighScores");
                });

            modelBuilder.Entity("JustDanceNextPlus.JustDanceClasses.Database.Profile.Profile", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<int>("CurrentLevel")
                        .HasColumnType("INTEGER");

                    b.Property<int>("CurrentXP")
                        .HasColumnType("INTEGER");

                    b.Property<Guid>("DancercardId")
                        .HasColumnType("TEXT");

                    b.Property<int>("PrestigeGrade")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Ticket")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("DancercardId");

                    b.ToTable("Profiles");
                });

            modelBuilder.Entity("JustDanceNextPlus.JustDanceClasses.Database.Profile.RunningTask", b =>
                {
                    b.Property<Guid>("TaskId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<int>("CurrentLevel")
                        .HasColumnType("INTEGER");

                    b.Property<int>("StepsDone")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Type")
                        .HasColumnType("INTEGER");

                    b.HasKey("TaskId");

                    b.ToTable("RunningTasks");
                });

            modelBuilder.Entity("JustDanceNextPlus.JustDanceClasses.Database.Profile.BossModeStats", b =>
                {
                    b.HasOne("JustDanceNextPlus.JustDanceClasses.Database.Profile.BossMode", "BossMode")
                        .WithMany()
                        .HasForeignKey("BossModeBossId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("BossMode");
                });

            modelBuilder.Entity("JustDanceNextPlus.JustDanceClasses.Database.Profile.MapStats", b =>
                {
                    b.OwnsOne("JustDanceNextPlus.JustDanceClasses.Database.Profile.GameModeStats", "GameModeStats", b1 =>
                        {
                            b1.Property<Guid>("MapStatsMapId")
                                .HasColumnType("TEXT");

                            b1.Property<Guid>("MapStatsProfileId")
                                .HasColumnType("TEXT");

                            b1.Property<bool>("Exists")
                                .HasColumnType("INTEGER");

                            b1.HasKey("MapStatsMapId", "MapStatsProfileId");

                            b1.ToTable("HighScores");

                            b1.WithOwner()
                                .HasForeignKey("MapStatsMapId", "MapStatsProfileId");

                            b1.OwnsOne("JustDanceNextPlus.JustDanceClasses.Database.Profile.ChallengeStats", "Challenge", b2 =>
                                {
                                    b2.Property<Guid>("GameModeStatsMapStatsMapId")
                                        .HasColumnType("TEXT");

                                    b2.Property<Guid>("GameModeStatsMapStatsProfileId")
                                        .HasColumnType("TEXT");

                                    b2.Property<int>("LastScore")
                                        .HasColumnType("INTEGER");

                                    b2.HasKey("GameModeStatsMapStatsMapId", "GameModeStatsMapStatsProfileId");

                                    b2.ToTable("HighScores");

                                    b2.WithOwner()
                                        .HasForeignKey("GameModeStatsMapStatsMapId", "GameModeStatsMapStatsProfileId");
                                });

                            b1.Navigation("Challenge")
                                .IsRequired();
                        });

                    b.OwnsOne("JustDanceNextPlus.JustDanceClasses.Database.Profile.HighscorePerformance", "HighScorePerformance", b1 =>
                        {
                            b1.Property<Guid>("MapStatsMapId")
                                .HasColumnType("TEXT");

                            b1.Property<Guid>("MapStatsProfileId")
                                .HasColumnType("TEXT");

                            b1.PrimitiveCollection<string>("GoldMovesAchieved")
                                .IsRequired()
                                .HasColumnType("TEXT");

                            b1.HasKey("MapStatsMapId", "MapStatsProfileId");

                            b1.ToTable("HighScores");

                            b1.WithOwner()
                                .HasForeignKey("MapStatsMapId", "MapStatsProfileId");

                            b1.OwnsOne("JustDanceNextPlus.JustDanceClasses.Database.Profile.MoveCounts", "Moves", b2 =>
                                {
                                    b2.Property<Guid>("HighscorePerformanceMapStatsMapId")
                                        .HasColumnType("TEXT");

                                    b2.Property<Guid>("HighscorePerformanceMapStatsProfileId")
                                        .HasColumnType("TEXT");

                                    b2.Property<int>("Gold")
                                        .HasColumnType("INTEGER");

                                    b2.Property<int>("Good")
                                        .HasColumnType("INTEGER");

                                    b2.Property<int>("Missed")
                                        .HasColumnType("INTEGER");

                                    b2.Property<int>("Okay")
                                        .HasColumnType("INTEGER");

                                    b2.Property<int>("Perfect")
                                        .HasColumnType("INTEGER");

                                    b2.Property<int>("Super")
                                        .HasColumnType("INTEGER");

                                    b2.HasKey("HighscorePerformanceMapStatsMapId", "HighscorePerformanceMapStatsProfileId");

                                    b2.ToTable("HighScores");

                                    b2.WithOwner()
                                        .HasForeignKey("HighscorePerformanceMapStatsMapId", "HighscorePerformanceMapStatsProfileId");
                                });

                            b1.Navigation("Moves")
                                .IsRequired();
                        });

                    b.Navigation("GameModeStats");

                    b.Navigation("HighScorePerformance")
                        .IsRequired();
                });

            modelBuilder.Entity("JustDanceNextPlus.JustDanceClasses.Database.Profile.Profile", b =>
                {
                    b.HasOne("JustDanceNextPlus.JustDanceClasses.Database.Profile.DancerCard", "Dancercard")
                        .WithMany()
                        .HasForeignKey("DancercardId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Dancercard");
                });
#pragma warning restore 612, 618
        }
    }
}
