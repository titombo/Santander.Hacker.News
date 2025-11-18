using Santander.Hacker.News.Domains;

namespace Santander.Hacker.News.TestFixtures
{
    /// <summary>
    /// Static test data helper used by unit tests to provide deterministic Hacker News IDs and Story objects.
    /// </summary>
    public static class FakeHackerNewsData
    {
        // Always return three deterministic IDs for tests
        public static int[] BestIds => new[] { 1, 2, 3 };

        // Return a Story for a known id, or null for unknown ids
        public static Story? GetStory(int id) => id switch
        {
            1 => new Story(
                Id: 1,
                By: "ismaildonmez",
                Title: "A uBlock Origin update was rejected from the Chrome Web Store",
                Score: 1716,
                Url: "https://github.com/uBlockOrigin/uBlock-issues/issues/745",
                Time: 1570881781,    // epoch seconds (example)
                Descendants: 572,
                Type: "story",
                Kids: null),
            2 => new Story(
                Id: 2,
                By: "alice",
                Title: "Example Story Two",
                Score: 900,
                Url: "https://example.com/2",
                Time: 1600000000,
                Descendants: 120,
                Type: "story",
                Kids: null),
            3 => new Story(
                Id: 3,
                By: "bob",
                Title: "Example Story Three",
                Score: 500,
                Url: "https://example.com/3",
                Time: 1600001000,
                Descendants: 42,
                Type: "story",
                Kids: null),
            _ => null
        };

        // Convenience: return all Story objects for the three ids
        public static Story[] GetStories() => new[]
        {
            GetStory(1)!,
            GetStory(2)!,
            GetStory(3)!
        };
    }
}
