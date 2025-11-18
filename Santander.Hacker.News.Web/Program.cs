using Santander.Hacker.News.Repositories;
using Santander.Hacker.News.Services;
using Santander.Hacker.News.Web.AutoMappers;
using Santander.Hacker.News.Web.ViewModels;
using System.Net;
using AutoMapper;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register AutoMapper via a single central call that delegates to per-entity mapping registrations
builder.Services.AddAutoMapper(cfg => MappingProfiles.Map(cfg));

// Rate limiting - per-client IP token-bucket limiter (tunable)
// TM: This is the configuration of rate limiting that is using a built-in middleware in .NET
builder.Services.AddRateLimiter(options =>
{
    // Named policy applied to endpoints
    options.AddPolicy("DefaultPolicy", context =>
        RateLimitPartition.GetTokenBucketLimiter(
            // partition by client IP (fallback to "anon")
            //TM: it is using the remote IP address of the client to identify it - so we have the congiruation per client IP
            context.Connection.RemoteIpAddress?.ToString() ?? "anon",
            key => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 10,                   // max tokens available (burst)
                TokensPerPeriod = 5,               // tokens added each period
                ReplenishmentPeriod = TimeSpan.FromSeconds(1), // refill period
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0                     // do not queue requests when exhausted
            }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;


    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.Headers["Retry-After"] = "1";
        await context.HttpContext.Response.WriteAsync("Rate limit exceeded!", token);
    };
});

// Read Hacker News base URL from configuration (appsettings.json / env vars) or fallback
var hackerNewsBaseUrl = builder.Configuration.GetValue<string>("HackerNews:BaseUrl") ?? "https://hacker-news.firebaseio.com/v0/";
if (!Uri.TryCreate(hackerNewsBaseUrl, UriKind.Absolute, out var hackerNewsUri))
{
    throw new InvalidOperationException($"Configuration value 'HackerNews:BaseUrl' is not a valid absolute URI: '{hackerNewsBaseUrl}'.");
}

// Register a named HttpClient for the repository
builder.Services.AddHttpClient("hackernews", c =>
{
    c.BaseAddress = hackerNewsUri;
});

// Memory cache for lightweight caching of ids/items
builder.Services.AddMemoryCache();

// DI for repository and service
builder.Services.AddScoped<IHackerNewsRepository, HackerNewsRepository>();
builder.Services.AddScoped<IStoryService, StoryService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

//TM: Enable the rate limiter middleware
app.UseRateLimiter();

// TODO: TM Consider API versioning package for more complex scenarios
// Simple, fixed API version in the route to keep things straightforward:
// GET /api/v1/stories/best?limit=10
app.MapGet("/api/v1/stories/best", async (IStoryService storyService, IMapper mapper, HttpContext httpContext, int limit = 10, CancellationToken cancellationToken = default) =>
{
    // Validate query parameter
    if (limit <= 0 || limit > 100)
    {
        return Results.BadRequest(new { error = "Query parameter 'limit' must be between 1 and 100." });
    }

    try
    {
        var stories = await storyService.GetBestStoriesAsync(limit, cancellationToken).ConfigureAwait(false);

        // Map domain models to view models using AutoMapper
        var view = mapper.Map<StoryViewModel[]>(stories);

        // Indicate the API version used in the response (helpful for clients and debugging)
        httpContext.Response.Headers["X-Api-Version"] = "1.0";

        return Results.Ok(view);
    }
    catch (ArgumentOutOfRangeException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (OperationCanceledException)
    {
        return Results.StatusCode((int)HttpStatusCode.RequestTimeout);
    }
    catch (Exception ex)
    {
        if (ex.InnerException is HttpRequestException || ex is HttpRequestException)
        {
            return Results.Problem(detail: "Failed to retrieve data from upstream Hacker News API.", statusCode: (int)HttpStatusCode.BadGateway);
        }

        return Results.Problem(detail: "An unexpected error occurred.", statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
// Apply the named rate-limit policy to this route
.RequireRateLimiting("DefaultPolicy")
.WithName("GetBestStories")
.Produces<IEnumerable<StoryViewModel>>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status502BadGateway);

app.Run();