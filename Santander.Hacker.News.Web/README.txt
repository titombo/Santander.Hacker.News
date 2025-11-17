Santander.Hacker.News - README
=============================

Overview
--------
This solution exposes a small REST API that returns the top N "best" Hacker News stories (as provided by the Hacker News API),
sorted by score (descending). The web project uses a Repository → Service → Web-layer separation, AutoMapper for mapping
domain models to view models, in-memory caching, and a simple per-client rate limiter.

Project layout (relevant projects)
- Santander.Hacker.News.Web       : Minimal API (Program.cs), Swagger, AutoMapper registration, endpoint(s).
- Santander.Hacker.News.Services : Business logic (IStoryService / StoryService).
- Santander.Hacker.News.Repositories : Hacker News HTTP access (IHackerNewsRepository / HackerNewsRepository).
- Santander.Hacker.News.Domains  : Domain models (Story).
- Santander.Hacker.News.Web.UnitTests : Integration/unit tests (WebApplicationFactory + fake services).
- Santander.Hacker.News.Web.ViewModels : API view models (StoryViewModel).
- Santander.Hacker.News.Web.AutoMappers : AutoMapper registration classes.

Requirements
------------
- .NET 8 SDK
- Visual Studio 2022 or VSCode (optional)
- Internet access to query the Hacker News API (unless tests use fakes)

NuGet packages you should have (common)
- Swashbuckle.AspNetCore
- AutoMapper
- AutoMapper.Extensions.Microsoft.DependencyInjection
- Microsoft.Extensions.Caching.Memory
- Microsoft.AspNetCore.Mvc.Testing (for tests)
- Microsoft.AspNetCore.TestHost (for tests)
- NUnit, NUnit3TestAdapter, Microsoft.NET.Test.Sdk (for tests)
- Polly (optional for resilience)
If you see any LuckyPennySoftware.AutoMapper package, remove it and replace with the official AutoMapper packages.

Configuration
-------------
Configuration values live in appsettings.json (and can be overridden via environment variables).

Key setting:
  "HackerNews": {
    "BaseUrl": "https://hacker-news.firebaseio.com/v0/"
  }

You can override the base URL with environment variables:
  set HackerNews__BaseUrl="https://..."        (Windows PowerShell/DevShell)
  export HackerNews__BaseUrl="https://..."     (Linux/macOS)

How it works (high level)
-------------------------
- Repository calls Hacker News endpoints:
  - GET /v0/beststories.json -> list of story IDs
  - GET /v0/item/{id}.json -> story details
- Service fetches a capped number of IDs, fetches details with bounded concurrency, filters to "story" items,
  sorts by score (then by time), and returns the top N requested.
- Repository uses IMemoryCache to cache IDs and items briefly to reduce upstream calls.
- AutoMapper maps domain Story -> StoryViewModel for the API response.
- API endpoint exposes: GET /api/v1/stories/best?limit={n}

Endpoint: GET /api/v1/stories/best
----------------------------------
Query parameter:
- limit (int) — how many stories to return. Defaults to 10. Allowed range: 1..100.

Response (JSON array) — each element is StoryViewModel:
{
  "title": "…",
  "uri": "https://…",
  "postedBy": "username",
  "time": "2019-10-12T13:43:01+00:00",
  "score": 1716,
  "commentCount": 572
}

Example:
  curl "http://localhost:5000/api/v1/stories/best?limit=5" -H "Accept: application/json"

Rate limiting
-------------
The app can be configured with a token-bucket rate limiter (per-client IP) in Program.cs.
Default behaviour when rate-limited:
- HTTP 429 Too Many Requests
- Optional Retry-After header

Adjust TokenLimit / TokensPerPeriod / ReplenishmentPeriod in Program.cs to suit your needs.

Run locally
-----------
1. Restore packages and build:
   dotnet restore
   dotnet build

2. Run the web project (from solution root):
   dotnet run --project Santander.Hacker.News.Web

3. Open Swagger UI (if running in Development):
   https://localhost:{port}/swagger

Run tests
---------
From solution root:
  dotnet test

Notes & troubleshooting
-----------------------
- If you see "An invalid request URI..." or missing BaseAddress, ensure you register the HttpClient consistently:
  - Either use a typed client:
      builder.Services.AddHttpClient<IHackerNewsRepository, HackerNewsRepository>(c => c.BaseAddress = new Uri(...));
    and let the repository accept HttpClient in its ctor, or
  - Use a named client and call IHttpClientFactory.CreateClient("hackernews") in the repository.
  Do not mix both approaches for the same service.
- If you see a TypeLoadException for Microsoft.OpenApi.*: check Swashbuckle and Microsoft.OpenApi package versions.
  Remove any explicit old Microsoft.OpenApi package; install a Swashbuckle version compatible with .NET 8.
- If you see messages about LuckyPennySoftware.AutoMapper: remove that package and install the official AutoMapper packages:
  dotnet remove <project> package LuckyPennySoftware.AutoMapper
  dotnet add <project> package AutoMapper
  dotnet add <project> package AutoMapper.Extensions.Microsoft.DependencyInjection

Extending the project
---------------------
- Add more mappings: add a new Mapping class and register it in MappingProfiles.Map(cfg).
- Move rate-limit / HTTP client policy settings to configuration.
- Add Polly resiliency policies to HttpClient registration (retry, timeout, circuit-breaker).
- Add per-version Swagger docs if you later enable API versioning.

Contact / References
--------------------
- Hacker News API: https://github.com/HackerNews/API
- AutoMapper: https://automapper.org/
- .NET Rate Limiting: https://learn.microsoft.com/dotnet/standard/threading/rate-limiting

----