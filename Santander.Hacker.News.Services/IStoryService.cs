using Santander.Hacker.News.Domains;

namespace Santander.Hacker.News.Services
{
    public interface IStoryService
    {
        /// <summary>
        /// Return the top N best stories as determined by Hacker News 'beststories' list,
        /// sorted by score descending (then by time descending as a deterministic tie break).
        /// </summary>
        Task<Story[]> GetBestStoriesAsync(int limit, CancellationToken cancellationToken = default);
    }
}