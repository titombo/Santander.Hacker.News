using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Santander.Hacker.News.TestFixtures;

namespace Santander.Hacker.News.Repositories.UnitTests
{
    [TestFixture]
    public class HackerNewsRepositoryTests
    {
        private const string BaseAddress = "https://hacker-news.firebaseio.com/v0/";

        [Test]
        //TM: Test if the repo is returning properly the list of best story IDs
        public async Task GetBestStoryIdsAsync_Returns_FixtureIds()
        {
            // arrange
            //TM: The IHttpClientFactory is really good to be able to mock the HttpClient used in the repository
            // but usually we need to fake two parts: the HttpClient itself and the HttpMessageHandler that is used by the HttpClient to send the requests
            var handler = new TestHttpMessageHandler();
            var client = new HttpClient(handler) { BaseAddress = new System.Uri(BaseAddress) };
            var factory = new SimpleHttpClientFactory(client);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var logger = NullLogger<HackerNewsRepository>.Instance;

            var repo = new HackerNewsRepository(factory, cache, logger);

            // act
            var ids = await repo.GetBestStoryIdsAsync(CancellationToken.None);

            // assert
            Assert.IsNotNull(ids);
            CollectionAssert.AreEqual(FakeHackerNewsData.BestIds, ids);
            Assert.Greater(handler.CallCount, 0);
        }

        [Test]
        //TM: Test if the repo is returning properly the list of items and caching them
        public async Task GetItemAsync_Returns_Story_And_IsCached()
        {
            // arrange
            var handler = new TestHttpMessageHandler();
            var client = new HttpClient(handler) { BaseAddress = new System.Uri(BaseAddress) };
            var factory = new SimpleHttpClientFactory(client);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var logger = NullLogger<HackerNewsRepository>.Instance;

            var repo = new HackerNewsRepository(factory, cache, logger);

            // act - first call should hit HTTP handler
            var item1 = await repo.GetItemAsync(1, CancellationToken.None);

            // TM: test if the caching is working properly
            // second call for same id should be served from cache (handler.CallCount unchanged)
            var item2 = await repo.GetItemAsync(1, CancellationToken.None);

            // assert
            Assert.IsNotNull(item1);
            Assert.AreEqual(1, item1!.Id);
            Assert.IsNotNull(item2);
            Assert.AreEqual(item1.Id, item2.Id);

            // normalize expected path using client's BaseAddress so test matches handler's recorded keys
            var expectedPathFor1 = new Uri(client.BaseAddress, "item/1.json").AbsolutePath;
            Assert.AreEqual(1, handler.GetRequestCountForPath(expectedPathFor1)); // one distinct item request
        }

        [Test]
        //TM: Test if the repo is returning null for unknown item IDs
        public async Task GetItemAsync_UnknownId_ReturnsNull()
        {
            // arrange
            var handler = new TestHttpMessageHandler();
            var client = new HttpClient(handler) { BaseAddress = new System.Uri(BaseAddress) };
            var factory = new SimpleHttpClientFactory(client);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var logger = NullLogger<HackerNewsRepository>.Instance;

            var repo = new HackerNewsRepository(factory, cache, logger);

            // act
            var item = await repo.GetItemAsync(999, CancellationToken.None);

            // assert
            Assert.IsNull(item);

            // use client's BaseAddress so the expected path includes the version segment the handler records
            var expectedPathFor999 = new Uri(client.BaseAddress, "item/999.json").AbsolutePath;
            Assert.AreEqual(1, handler.GetRequestCountForPath(expectedPathFor999));
        }
    }
}