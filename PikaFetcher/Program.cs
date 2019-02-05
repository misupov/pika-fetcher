﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PikaModel;

namespace PikaFetcher
{
    class Program
    {
        private readonly Random r = new Random();

        private static async Task Main(string[] args)
        {
            var program = new Program();
            await program.OnExecuteAsync();
        }

        private Program()
        {
        }

        private async Task OnExecuteAsync()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using (var context = new PikabuContext())
            {
                context.Database.Migrate();
            }

            var api = new PikabuApi();
            await api.Init();

            var loopTop = LoopTop(api);
            var loopPeriod = LoopPeriod(api);
            await Task.WhenAll(loopTop, loopPeriod);
        }

        private async Task LoopPeriod(PikabuApi api)
        {
            int c = 0;
            var latestStoryId = await api.GetLatestStoryId();
            while (true)
            {
                int storyId = -1;
                try
                {
                    storyId = GetStoryId(latestStoryId);
                    var result = await ProcessStory(api, storyId);
                    Console.WriteLine($"{DateTime.UtcNow} [{FormatTimeSpan(DateTime.UtcNow - result.TimestampUtc)}] ({storyId}) {result.Rating?.ToString("+0;-#") ?? "?"} ({result.NewCommentsCount}/{result.TotalCommentsCount}) {result.StoryTitle}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"ERROR ({storyId}/{latestStoryId}): {e.Message}");
                }

                c++;
                if (c % 100 == 0)
                {
                    latestStoryId = await api.GetLatestStoryId();
                    c = 0;
                }
            }
        }

        private int GetStoryId(int latestStoryId)
        {
            var next = r.NextDouble();
            if (next < 0.5)
            {
                return latestStoryId - r.Next(200);
            }
            if (next < 0.7)
            {
                return latestStoryId - 200 - r.Next(800);
            }
            if (next < 0.9)
            {
                return latestStoryId - 1000 - r.Next(9000);
            }
            return latestStoryId - 10000 - r.Next(latestStoryId - 10000);
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan < TimeSpan.FromMinutes(1))
            {
                return $"{(int) timeSpan.TotalSeconds} sec ago";
            }

            if (timeSpan < TimeSpan.FromHours(1))
            {
                return $"{(int)timeSpan.TotalMinutes} min ago";
            }

            if (timeSpan < TimeSpan.FromHours(48))
            {
                return $"{timeSpan.TotalHours:##.##} hours ago";
            }

            return $"{timeSpan.TotalDays:##.##} days ago";
        }

        private async Task LoopTop(PikabuApi api)
        {
            int top = 500;
            while (true)
            {
                int[] topStoryIds;
                using (var db = new PikabuContext())
                {
                    topStoryIds = await db.Stories
                        .Where(story => story.DateTimeUtc >= DateTime.UtcNow - TimeSpan.FromDays(7))
                        .OrderByDescending(story => story.Rating)
                        .Select(story => story.StoryId)
                        .Take(top)
                        .ToArrayAsync();
                }

                if (topStoryIds.Length < top)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    continue;
                }

                for (var index = 0; index < topStoryIds.Length; index++)
                {
                    var storyId = topStoryIds[index];
                    try
                    {
                        var result = await ProcessStory(api, storyId);
                        if (result != null)
                        {
                            Console.WriteLine(
                                $"TOP[{index + 1}] {DateTime.UtcNow} [{FormatTimeSpan(DateTime.UtcNow - result.TimestampUtc)}] ({storyId}) {result.Rating?.ToString("+0;-#") ?? "?"} ({result.NewCommentsCount}/{result.TotalCommentsCount}) {result.StoryTitle}");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"ERROR: {e.Message}");
                    }
                }

                Console.WriteLine($"RESTART");
            }
        }

        private async Task<StoryProcessingResult> ProcessStory(PikabuApi api, int storyId)
        {
            using (var db = new PikabuContext())
            using (var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                var scanTime = DateTime.UtcNow;
                var newComments = 0;
                var story = await db.Stories.SingleOrDefaultAsync(s => s.StoryId == storyId);
                var storyComments = await api.GetStoryComments(storyId);
                if (story == null)
                {
                    story = new Story
                    {
                        StoryId = storyComments.StoryId,
                        Rating = storyComments.Rating,
                        Title = storyComments.StoryTitle,
                        DateTimeUtc = storyComments.Timestamp.UtcDateTime,
                        Comments = new List<Comment>()
                    };
                    await db.Stories.AddAsync(story);
                }

                story.Rating = storyComments.Rating;
                story.Title = storyComments.StoryTitle;
                story.LastScanUtc = scanTime;

                var userNames = new HashSet<string>(storyComments.Comments.Select(c => c.User));

                var userComments = await db.Comments.Where(c => userNames.Contains(c.User.UserName)).Select(c => new { c.User.UserName, c.CommentId}).ToArrayAsync();

                var users = new HashSet<string>(userComments.Select(uc => uc.UserName));
                var comments = new HashSet<long>(userComments.Select(uc => uc.CommentId));

                foreach (var comment in storyComments.Comments)
                {
                    if (!users.Contains(comment.User))
                    {
                        var user = new User {UserName = comment.User, Comments = new List<Comment>()};
                        await db.Users.AddAsync(user);
                        users.Add(user.UserName);
                    }

                    if (!comments.Contains(comment.CommentId))
                    {
                        var item = new Comment
                        {
                            CommentId = comment.CommentId,
                            ParentId = comment.ParentId,
                            DateTimeUtc = comment.Timestamp.UtcDateTime,
                            Story = story,
                            UserName = comment.User,
                            CommentBody = comment.Body
                        };
                        comments.Add(comment.CommentId);
                        await db.Comments.AddAsync(item);
                        newComments++;
                    }
                }

                await db.SaveChangesAsync();
                transaction.Commit();

                return new StoryProcessingResult(story, storyComments.Comments.Count, newComments);
            }
        }
    }
}