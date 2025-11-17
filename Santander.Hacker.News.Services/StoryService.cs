using Santander.Hacker.News.Domains;
using Santander.Hacker.News.Repositories;

namespace Santander.Hacker.News.Services
{
    public class StoryService : IStoryService
    {
        private readonly IHackerNewsRepository _repo;
        private const int MaxLimit = 100;               // enforce a reasonable maximum requested by caller
        private const int MaxIdsToFetch = 500;          // cap number of ids to inspect from beststories
        private const int MaxParallelism = 20;

        public StoryService(IHackerNewsRepository repo)
        {
            _repo = repo;
        }

        public async Task<Story[]> GetBestStoriesAsync(int limit, CancellationToken cancellationToken = default)
        {
            if (limit <= 0) throw new ArgumentOutOfRangeException(nameof(limit), "limit must be > 0");
            if (limit > MaxLimit) throw new ArgumentOutOfRangeException(nameof(limit), $"limit must be <= {MaxLimit}");

            var ids = await _repo.GetBestStoryIdsAsync(cancellationToken).ConfigureAwait(false)
                      ?? Array.Empty<int>();

            if (ids.Length == 0) return Array.Empty<Story>();

            // Limit how many ids we will fetch details for to avoid long-running operations.
            var idsToFetch = ids.Take(Math.Min(ids.Length, MaxIdsToFetch)).ToArray();

            var stories = new List<Story>(idsToFetch.Length);

            using var semaphore = new SemaphoreSlim(MaxParallelism);

            var tasks = idsToFetch.Select(async id =>
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    var item = await _repo.GetItemAsync(id, cancellationToken).ConfigureAwait(false);
                    if (item is not null && string.Equals(item.Type, "story", StringComparison.OrdinalIgnoreCase))
                    {
                        lock (stories)
                        {
                            stories.Add(item);
                        }
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToArray();

            await Task.WhenAll(tasks).ConfigureAwait(false);

            var ordered = stories
                .OrderByDescending(s => s.Score ?? 0)
                .ThenByDescending(s => s.Time ?? 0) // if scores are equal, newer stories first
                .Take(limit)
                .ToArray();

            return ordered;
        }
    }
}