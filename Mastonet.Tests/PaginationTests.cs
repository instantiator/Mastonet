using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mastonet.Entities;
using Xunit;
using Xunit.Abstractions;

namespace Mastonet.Tests
{
	public class PaginationTests
	{
		private string token = "TODO"; // TODO - set your token
        private string instance = "TODO"; // TODO - set your instance domain
        private int MAX_PAGES = 4;

        [Fact]
        public async Task PaginatesIndefinitely()
        {
            // the test actually caps pagination at MAX_PAGES, but this proves it could go on...
            var client = new MastodonClient(instance, token);
            var notifications = await GetAllNotificationsSinceSinceIdAsync(client, 0, MAX_PAGES);
            Assert.NotNull(notifications);
            Assert.True(notifications.Count() > 2);
            Assert.Equal(MAX_PAGES, recentIterations);
        }

        [Fact]
        public async Task PaginationRespectsMinId()
        {
            var client = new MastodonClient(instance, token);
            var debug = new List<String>();

            var notifications = await GetAllNotificationsBackToMinIdAsync(client, 0, MAX_PAGES);
            var firstPaginations = recentIterations;

            debug.Add($"notifications.Count() = {notifications.Count()}");
            debug.Add($"minId = {notifications.Min(n => long.Parse(n.Id))}");

            // find an id in the middle of the pack
            var midId = FindAMiddleishId(notifications);
            debug.Add($"midId = {midId}");

            var justSecondHalf = await GetAllNotificationsBackToMinIdAsync(client, midId, MAX_PAGES);
            var secondPaginations = recentIterations;
            debug.Add($"justSecondHalf.Count() = {justSecondHalf.Count()}");

            Assert.True(justSecondHalf.Count() > 0);
            Assert.True(justSecondHalf.Count() < notifications.Count(), string.Join("\n", debug));
            Assert.True(secondPaginations < firstPaginations);
        }

        [Fact]
        public async Task PaginationRespectsSinceId()
        {
            var client = new MastodonClient(instance, token);
            var debug = new List<String>();

            var notifications = await GetAllNotificationsSinceSinceIdAsync(client, 0, MAX_PAGES);
            var firstPaginations = recentIterations;

            debug.Add($"notifications.Count() = {notifications.Count()}");
            debug.Add($"minId = {notifications.Min(n => long.Parse(n.Id))}");

            // find an id in the middle of the pack
            var sinceId = FindAMiddleishId(notifications);
            debug.Add($"midId = {sinceId}");

            var justSecondHalf = await GetAllNotificationsSinceSinceIdAsync(client, sinceId, MAX_PAGES);
            var secondPaginations = recentIterations;
            debug.Add($"justSecondHalf.Count() = {justSecondHalf.Count()}");

            Assert.True(justSecondHalf.Count() > 0);
            Assert.True(justSecondHalf.Count() < notifications.Count(), string.Join("\n", debug));
            Assert.True(secondPaginations < firstPaginations);
        }

        private long FindAMiddleishId(IEnumerable<Notification> notifications)
        {
            var avgTicks = (notifications.Max(n => n.CreatedAt.Ticks) + notifications.Min(n => n.CreatedAt.Ticks)) / 2;
            var closestDiff = notifications.Min(n => Math.Abs(n.CreatedAt.Ticks - avgTicks));
            var midNotification = notifications.Single(n => Math.Abs(n.CreatedAt.Ticks - avgTicks) == closestDiff);
            return long.Parse(midNotification.Id);
        }

        private int recentIterations;
        private async Task<IEnumerable<Notification>> GetAllNotificationsBackToMinIdAsync(MastodonClient client, long minId, int maxPages)
        {
            var list = new List<Notification>();
            long? nextPageMaxId = null;
            recentIterations = 0;

            do
            {
                ArrayOptions opts = new ArrayOptions()
                {
                    MinId = minId.ToString(),
                    MaxId = nextPageMaxId?.ToString()
                };

                var page = await client.GetNotifications(options: opts);

                list.AddRange(page.Where(pn => !list.Select(n => n.Id).Contains(pn.Id)));
                nextPageMaxId = page.NextPageMaxId;
                recentIterations++;
            } while (nextPageMaxId != null && recentIterations < maxPages);

            return list;
        }

        private async Task<IEnumerable<Notification>> GetAllNotificationsSinceSinceIdAsync(MastodonClient client, long sinceId, int maxPages)
        {
            var list = new List<Notification>();
            long? nextPageMaxId = null;
            recentIterations = 0;

            do
            {
                ArrayOptions opts = new ArrayOptions()
                {
                    SinceId = sinceId.ToString(),
                    MaxId = nextPageMaxId?.ToString()
                };

                var page = await client.GetNotifications(options: opts);

                list.AddRange(page.Where(pn => !list.Select(n => n.Id).Contains(pn.Id)));
                nextPageMaxId = page.NextPageMaxId;
                recentIterations++;
            } while (nextPageMaxId != null && recentIterations < maxPages);

            return list;
        }
    }
}

