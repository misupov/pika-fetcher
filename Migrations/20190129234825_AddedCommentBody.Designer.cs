﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PikaFetcher;

namespace PikaFetcher.Migrations
{
    [DbContext(typeof(PikabuContext))]
    [Migration("20190129234825_AddedCommentBody")]
    partial class AddedCommentBody
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.1-servicing-10028");

            modelBuilder.Entity("PikaFetcher.Comment", b =>
                {
                    b.Property<long>("CommentId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("CommentBody");

                    b.Property<DateTime>("DateTimeUtc");

                    b.Property<long>("ParentId");

                    b.Property<int?>("StoryId");

                    b.Property<string>("UserName");

                    b.HasKey("CommentId");

                    b.HasIndex("StoryId");

                    b.HasIndex("UserName");

                    b.ToTable("Comments");
                });

            modelBuilder.Entity("PikaFetcher.Story", b =>
                {
                    b.Property<int>("StoryId")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("DateTimeUtc");

                    b.Property<DateTime>("LastScanUtc");

                    b.Property<int?>("Rating");

                    b.Property<string>("Title");

                    b.HasKey("StoryId");

                    b.ToTable("Stories");
                });

            modelBuilder.Entity("PikaFetcher.User", b =>
                {
                    b.Property<string>("UserName")
                        .ValueGeneratedOnAdd();

                    b.HasKey("UserName");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("PikaFetcher.Comment", b =>
                {
                    b.HasOne("PikaFetcher.Story", "Story")
                        .WithMany("Comments")
                        .HasForeignKey("StoryId");

                    b.HasOne("PikaFetcher.User", "User")
                        .WithMany("Comments")
                        .HasForeignKey("UserName");
                });
#pragma warning restore 612, 618
        }
    }
}
