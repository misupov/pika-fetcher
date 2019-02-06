﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PikaModel;

namespace PikaModel.Migrations
{
    [DbContext(typeof(PikabuContext))]
    [Migration("20190206211120_AuthorColumnAdded")]
    partial class AuthorColumnAdded
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.1-servicing-10028")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("PikaModel.Comment", b =>
                {
                    b.Property<long>("CommentId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("CommentBody");

                    b.Property<DateTime>("DateTimeUtc");

                    b.Property<long>("ParentId");

                    b.Property<int>("StoryId");

                    b.Property<string>("UserName");

                    b.HasKey("CommentId");

                    b.HasIndex("StoryId");

                    b.HasIndex("UserName");

                    b.ToTable("Comments");
                });

            modelBuilder.Entity("PikaModel.Story", b =>
                {
                    b.Property<int>("StoryId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("AuthorUserName");

                    b.Property<DateTime>("DateTimeUtc");

                    b.Property<DateTime>("LastScanUtc");

                    b.Property<int?>("Rating");

                    b.Property<string>("Title");

                    b.HasKey("StoryId");

                    b.HasIndex("AuthorUserName");

                    b.ToTable("Stories");
                });

            modelBuilder.Entity("PikaModel.User", b =>
                {
                    b.Property<string>("UserName")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(100);

                    b.HasKey("UserName");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("PikaModel.Comment", b =>
                {
                    b.HasOne("PikaModel.Story", "Story")
                        .WithMany("Comments")
                        .HasForeignKey("StoryId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("PikaModel.User", "User")
                        .WithMany("Comments")
                        .HasForeignKey("UserName");
                });

            modelBuilder.Entity("PikaModel.Story", b =>
                {
                    b.HasOne("PikaModel.User", "Author")
                        .WithMany()
                        .HasForeignKey("AuthorUserName");
                });
#pragma warning restore 612, 618
        }
    }
}
