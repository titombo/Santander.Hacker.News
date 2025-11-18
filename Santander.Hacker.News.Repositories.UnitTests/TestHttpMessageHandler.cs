using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Santander.Hacker.News.TestFixtures;

namespace Santander.Hacker.News.Repositories.UnitTests
{
    // Test handler that returns fixture JSON for beststories.json and item/{id}.json
    internal class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, int> _counts
            = new System.Collections.Concurrent.ConcurrentDictionary<string, int>();

        public int CallCount => _counts.Values.Sum();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            _counts.AddOrUpdate(path, 1, (_, cur) => cur + 1);

            if (path.EndsWith("/beststories.json"))
            {
                var json = JsonSerializer.Serialize(FakeHackerNewsData.BestIds);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                });
            }

            // Match /item/{id}.json anywhere in the path (handles /v0/item/1.json and /item/1.json)
            var m = Regex.Match(path, @"/item/(?<id>\d+)\.json$", RegexOptions.Compiled);
            if (m.Success && int.TryParse(m.Groups["id"].Value, out var id))
            {
                var story = FakeHackerNewsData.GetStory(id);
                if (story is null)
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
                }

                var json = JsonSerializer.Serialize(story);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        public int GetRequestCountForPath(string relativePath)
            => _counts.TryGetValue(relativePath, out var c) ? c : 0;
    }
}
