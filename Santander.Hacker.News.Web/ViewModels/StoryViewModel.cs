using System.Text.Json.Serialization;

namespace Santander.Hacker.News.Web.ViewModels
{
    public class StoryViewModel
    {
        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("uri")]
        public string? Uri { get; init; }

        [JsonPropertyName("postedBy")]
        public string? PostedBy { get; init; }

        // ISO 8601 string with offset, e.g. "2019-10-12T13:43:01+00:00"
        [JsonPropertyName("time")]
        public string? Time { get; init; }

        [JsonPropertyName("score")]
        public int Score { get; init; }

        [JsonPropertyName("commentCount")]
        public int CommentCount { get; init; }
    }
}
