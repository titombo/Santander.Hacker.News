using Santander.Hacker.News.Domains;

namespace Santander.Hacker.News.Repositories
{
    public interface IHackerNewsRepository
    {
        /// <summary>
        /// Retrieve the current list of best story IDs from Hacker News.
        /// </summary>
        Task<int[]?> GetBestStoryIdsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieve a single item (story/item) by id from Hacker News.
        /// </summary>
        Task<Story?> GetItemAsync(int id, CancellationToken cancellationToken = default);
    }
}