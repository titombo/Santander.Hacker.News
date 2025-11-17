using System.Text.Json.Serialization;

namespace Santander.Hacker.News.Domains
{
    public record Story(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("by")] string? By,
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("score")] int? Score,
        [property: JsonPropertyName("url")] string? Url,
        [property: JsonPropertyName("time")] long? Time,
        [property: JsonPropertyName("descendants")] int? Descendants,
        [property: JsonPropertyName("type")] string? Type,
        [property: JsonPropertyName("kids")] int[]? Kids
    );
}