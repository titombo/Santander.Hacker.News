using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Santander.Hacker.News.Domains;

namespace Santander.Hacker.News.Repositories
{
    public class HackerNewsRepository : IHackerNewsRepository
    {
        //TM - it is using the abstract factory design pattern and object pooling
        //in that case IHttpClientFactory can reuse the httpClient connections
        //this improves performance
        private readonly IHttpClientFactory _httpFactory;

        //TODO: TM - We could use a log aggregator in case we are logging in a centralized logging place
        private readonly ILogger<HackerNewsRepository> _logger;

        //TODO: - TM: The caching could be also something that also could be configurable on the 
        //appsettings.json instead of hard-coding them in here, also the option to turn it on or off
        //the caching time also could be adjustable in here
        //the use of memory cache in here is a good option, since the caching is independent of the user
        //it is requesting it, and won't use a lot of memory
        //this improves performance
        private readonly IMemoryCache _cache;

        //TM - this is the cache key for the hacker news best stories ids
        //at the moment is 1 minute for the ids, but could be configurable via appsettings in the future
        private const string BestIdsCacheKey = "hn_best_ids";

        //TM - this is the cache key for the hacker news items itself
        //at the moment it is 5 minutes for the item, but could be configurable via appsettings in future
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

                //TM - unfourtunetly there is no endpoint to get only part of the stories, so we need to get all beststories and then filter them in memory
                // also we could implement retry policies with Polly or similar libraries in case of transient errors
                // when implementing retry policies we need to be careful with the rate limiting of the upstream API - this API apparently doesnt have at the moment (documentation)
                // also add a circuit breaker to avoid overwhelming the API in case it continues failing and don't want to have infinite retries
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