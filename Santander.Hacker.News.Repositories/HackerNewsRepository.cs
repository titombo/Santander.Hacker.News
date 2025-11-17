using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Santander.Hacker.News.Domains;

namespace Santander.Hacker.News.Repositories
{
    public class HackerNewsRepository : IHackerNewsRepository
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly IMemoryCache _cache;
        private readonly ILogger<HackerNewsRepository> _logger;
        private const string BestIdsCacheKey = "hn_best_ids";
        private const string ItemCacheKeyPrefix = "hn_item_";

        public HackerNewsRepository(IHttpClientFactory httpFactory, IMemoryCache cache, ILogger<HackerNewsRepository> logger)
        {
            _httpFactory = httpFactory;
            _cache = cache;
            _logger = logger;
        }

        public async Task<int[]?> GetBestStoryIdsAsync(CancellationToken cancellationToken = default)
        {
            // Cache the IDs briefly to avoid hammering the upstream API on repeated calls.
            if (_cache.TryGetValue<int[]?>(BestIdsCacheKey, out var cached) && cached is not null)
            {
                return cached;
            }

            try
            {
                var client = _httpFactory.CreateClient("hackernews");
                var ids = await client.GetFromJsonAsync<int[]>("beststories.json", cancellationToken).ConfigureAwait(false);

                // Keep IDs short-lived since Hacker News changes frequently.
                _cache.Set(BestIdsCacheKey, ids, TimeSpan.FromSeconds(60));
                return ids;
            }
            catch (OperationCanceledException)
            {
                // preserve cancellation semantics for callers
                throw;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve best story IDs from Hacker News.");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while retrieving best story IDs from Hacker News.");
                return null;
            }
        }

        public async Task<Story?> GetItemAsync(int id, CancellationToken cancellationToken = default)
        {
            var cacheKey = ItemCacheKeyPrefix + id;
            if (_cache.TryGetValue<Story?>(cacheKey, out var cached) && cached is not null)
            {
                return cached;
            }

            var client = _httpFactory.CreateClient("hackernews");
            Story? item;
            try
            {
                item = await client.GetFromJsonAsync<Story>($"item/{id}.json", cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // preserve cancellation semantics for callers
                throw;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve item {Id} from Hacker News.", id);
                item = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while retrieving item {Id} from Hacker News.", id);
                item = null;
            }

            if (item is not null)
            {
                // Item details don't change often; cache for a short-to-medium window.
                _cache.Set(cacheKey, item, TimeSpan.FromMinutes(5));
            }

            return item;
        }
    }
}