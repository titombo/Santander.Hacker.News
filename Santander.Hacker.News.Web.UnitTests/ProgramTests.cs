//using System.Net;
//using System.Net.Http.Json;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.VisualStudio.TestPlatform.TestHost;
//using Santander.Hacker.News.Domains;
//using Santander.Hacker.News.Services;

//namespace Santander.Hacker.News.Web.UnitTests
//{
//    public class ProgramTests
//    {
//        private WebApplicationFactory<Program> _factory = null!;

//        [SetUp]
//        public void SetUp()
//        {
//            _factory = new CustomWebApplicationFactory();
//        }

//        [TearDown]
//        public void TearDown()
//        {
//            _factory.Dispose();
//        }

//        [Test]
//        public async Task GetBestStories_ReturnsOk_WithVersionHeader_AndRespectsLimit()
//        {
//            using var client = _factory.CreateClient();

//            var response = await client.GetAsync("/api/v1/stories/best?limit=2");
//            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Expected 200 OK");

//            // X-Api-Version header is set by the app
//            Assert.IsTrue(response.Headers.Contains("X-Api-Version"));
//            Assert.AreEqual("1.0", response.Headers.GetValues("X-Api-Version").First());

//            var stories = await response.Content.ReadFromJsonAsync<Story[]>();
//            Assert.IsNotNull(stories);
//            Assert.AreEqual(2, stories!.Length);
//            Assert.AreEqual(1, stories[0].Id);
//            Assert.AreEqual(2, stories[1].Id);
//        }

//        [Test]
//        public async Task GetBestStories_InvalidLimit_ReturnsBadRequest()
//        {
//            using var client = _factory.CreateClient();

//            var response = await client.GetAsync("/api/v1/stories/best?limit=0");
//            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
//        }

//        // Minimal custom factory that replaces IStoryService with a deterministic fake
//        private class CustomWebApplicationFactory : WebApplicationFactory<Program>
//        {
//            protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
//            {
//                builder.ConfigureTestServices(services =>
//                {
//                    services.AddScoped<IStoryService>(_ => new FakeStoryService());
//                });
//            }
//        }

//        // Fake service returns predictable stories so tests don't call the real Hacker News API
//        private class FakeStoryService : IStoryService
//        {
//            public Task<Story[]> GetBestStoriesAsync(int limit, CancellationToken cancellationToken = default)
//            {
//                var all = new[]
//                {
//                    new Story(1, "alice", "Title 1", 100, "https://example/1", 1, 0, "story", null),
//                    new Story(2, "bob", "Title 2", 90, "https://example/2", 2, 0, "story", null),
//                    new Story(3, "carol", "Title 3", 80, "https://example/3", 3, 0, "story", null)
//                };

//                return Task.FromResult(all.Take(limit).ToArray());
//            }
//        }
//    }
//}