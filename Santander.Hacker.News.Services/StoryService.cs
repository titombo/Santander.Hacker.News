using Santander.Hacker.News.Domains;
using Santander.Hacker.News.Repositories;

namespace Santander.Hacker.News.Services
{
    public class StoryService : IStoryService
    {
        private readonly IHackerNewsRepository _repo;

        // TODO: TM - Those values could be in a configuration that could be mapped on appsettings with hot-reload and change according with the environment
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

            //TM - After checking the limits we request the repo, we are using the repository pattern, so 
            //we don't care much what is behind it - database, requests to other services, etc...
            var ids = await _repo.GetBestStoryIdsAsync(cancellationToken).ConfigureAwait(false)
                      ?? Array.Empty<int>();

            if (ids.Length == 0) return Array.Empty<Story>();

            // Limit how many ids we will fetch details for to avoid long-running operations.
            var idsToFetch = ids.Take(Math.Min(ids.Length, MaxIdsToFetch)).ToArray();

            var stories = new List<Story>(idsToFetch.Length);

            //TM: We are using semaphore to limit the use of resources wit parallel threads
            using var semaphore = new SemaphoreSlim(MaxParallelism);

            var tasks = idsToFetch.Select(async id =>
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    var item = await _repo.GetItemAsync(id, cancellationToken).ConfigureAwait(false);

                    //TM: is filtering by the type story...
                    // according to it's documentation it can be various types
                    // The type of item. One of "job", "story", "comment", "poll", or "pollopt".
                    if (item is not null && string.Equals(item.Type, "story", StringComparison.OrdinalIgnoreCase))
                    {
                        //TM: To avoid concurrency issues we lock the list when adding new items
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
                .ThenByDescending(s => s.Time ?? 0) // TM:if scores are equal, newer stories first
                .Take(limit)
                .ToArray();

            return ordered;
        }
    }
}