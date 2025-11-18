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
        public async Task GetBestStoriesAsync_FiltersNonStoryTypes_AndSkipsNullItems()
        {
            // Arrange
            var ids = new[] { 10, 11, 12 };
            _repoMock.Setup(r => r.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
                     .ReturnsAsync(ids);

            // id 10: valid story
            _repoMock.Setup(r => r.GetItemAsync(10, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new Story(10, "x", "S1", 5, "u10", 100, 1, "story", null));
            // id 11: non-story type should be excluded
            _repoMock.Setup(r => r.GetItemAsync(11, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new Story(11, "y", "NotStory", 100, "u11", 200, 2, "job", null));
            // id 12: upstream returned null -> skip
            _repoMock.Setup(r => r.GetItemAsync(12, It.IsAny<CancellationToken>()))
                     .ReturnsAsync((Story?)null);

            // Act
            var result = await _service.GetBestStoriesAsync(10, CancellationToken.None);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(10, result[0].Id);
        }

        [Test]
        public void GetBestStoriesAsync_InvalidLimit_ThrowsArgumentOutOfRangeException()
        {
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await _service.GetBestStoriesAsync(0));
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await _service.GetBestStoriesAsync(101));
        }

        [Test]
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