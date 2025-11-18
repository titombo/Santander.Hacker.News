using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Santander.Hacker.News.Domains;
using Santander.Hacker.News.Repositories;
using Santander.Hacker.News.Services;
using Santander.Hacker.News.TestFixtures;

namespace Santander.Hacker.News.Services.UnitTests
{
    [TestFixture]
    public class StoryServiceTests
    {
        private Mock<IHackerNewsRepository> _repoMock = null!;
        private StoryService _service = null!;

        [SetUp]
        public void SetUp()
        {
            _repoMock = new Mock<IHackerNewsRepository>(MockBehavior.Strict);
            _service = new StoryService(_repoMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _repoMock.VerifyAll();
        }

        [Test]
        //TM: Test the GetBestStoriesAsync method to ensure it returns top N stories sorted by score - the main functionality
        public async Task GetBestStoriesAsync_ReturnsTopN_SortedByScore()
        {
            // Arrange
            var ids = new[] { 1, 2, 3, 4 };
            _repoMock.Setup(r => r.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
                     .ReturnsAsync(ids);

            foreach (var id in ids)
            {
                _repoMock.Setup(r => r.GetItemAsync(id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(FakeHackerNewsData.GetStory(id));
            }

            // Act - request top 2
            var result = await _service.GetBestStoriesAsync(2, CancellationToken.None);

            // Assert - Fake data scores: id=1 (1716), id=2 (900), id=3 (500)
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(1, result[0].Id); // highest score (1716)
            Assert.AreEqual(2, result[1].Id); // next highest (900)
        }

        [Test]
        //TM: Test that GetBestStoriesAsync filters out non-story types and skips null items - sometimes it can be items that are not stories
        public async Task GetBestStoriesAsync_FiltersNonStoryTypes_AndSkipsNullItems_UsingFakeData()
        {
            // Arrange - use fake ids but override some items to simulate non-story & null
            var ids = FakeHackerNewsData.BestIds;
            _repoMock.Setup(r => r.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
                     .ReturnsAsync(ids);

            // id=1 -> valid story
            _repoMock.Setup(r => r.GetItemAsync(1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(FakeHackerNewsData.GetStory(1));

            // id=2 -> pretend upstream returned a non-story type
            var nonStory = FakeHackerNewsData.GetStory(2) with { Type = "job" };
            _repoMock.Setup(r => r.GetItemAsync(2, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(nonStory);

            // id=3 -> upstream returned null
            _repoMock.Setup(r => r.GetItemAsync(3, It.IsAny<CancellationToken>()))
                     .ReturnsAsync((Story?)null);

            // Act
            var result = await _service.GetBestStoriesAsync(10, CancellationToken.None);

            // Assert - only id=1 should remain
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(1, result[0].Id);
        }

        [Test]
        //TM: Test that GetBestStoriesAsync throws ArgumentOutOfRangeException for invalid limits - 1-100 is the valid ones now - can be variable in the future
        public void GetBestStoriesAsync_InvalidLimit_ThrowsArgumentOutOfRangeException()
        {
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await _service.GetBestStoriesAsync(0));
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await _service.GetBestStoriesAsync(101));
        }

        [Test]
        //TM: Test that GetBestStoriesAsync handles empty or null ID list from repo - and it should still works
        public async Task GetBestStoriesAsync_EmptyIds_ReturnsEmptyArray()
        {
            // Arrange - repo returns null
            _repoMock.Setup(r => r.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
                     .ReturnsAsync((int[]?)null);

            // Act
            var result = await _service.GetBestStoriesAsync(5, CancellationToken.None);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsEmpty(result);
        }
    }
}