using Orleans.Samples.UrlShortener.Web.Grains;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseOrleans(static siloBuilder =>
{
    siloBuilder
        .UseLocalhostClustering()
        .AddMemoryGrainStorage("urls");
});

using var app = builder.Build();

var logger = app.Logger;
logger.LogInformation("Application starting up...");

app.MapGet("/", () => Results.File(Path.Combine(builder.Environment.WebRootPath, "index.html"), "text/html"));

app.MapGet("shorten", Shorten);

app.MapGet("/go/{shortenedRouteSegment:required}", Redirect);

logger.LogInformation("Application configured and ready to handle requests");

await app.RunAsync();


#region Methods
static async Task<IResult> Shorten(IGrainFactory grains, HttpRequest request, string url, ILogger<Program> logger)
{
    logger.LogInformation("Received URL shortening request for: {Url}", url);

    // Gets the base URL for the current request
    var host = $"{request.Scheme}://{request.Host.Value}";

    // Validates the URL query string
    if (string.IsNullOrWhiteSpace(url) && Uri.IsWellFormedUriString(url, UriKind.Absolute) is false)
    {
        logger.LogWarning("Invalid URL provided: {Url}", url);
        return Results.BadRequest($"""
            The URL query string is required and needs to be well formed.
            Consider, ${host}/shorten?url=https://www.microsoft.com.
            """);
    }

    // Create a unique, short ID
    var shortenedRouteSegment = Guid.NewGuid().GetHashCode().ToString("X");

    logger.LogInformation("Generated shortened route segment: {ShortenedRouteSegment} for URL: {Url}", shortenedRouteSegment, url);

    // Create and persist a grain with the shortened ID and full URL
    var shortenerGrain =
        grains.GetGrain<IUrlShortenerGrain>(shortenedRouteSegment);

    try
    {
        // Sets the URL in the grain
        await shortenerGrain.SetUrl(url);
        logger.LogInformation("Successfully stored URL mapping: {ShortenedRouteSegment} -> {Url}", shortenedRouteSegment, url);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to store URL mapping for {ShortenedRouteSegment}", shortenedRouteSegment);
        return Results.Problem("Failed to create shortened URL. Please try again.");
    }

    // Return the shortened URL for later use
    var resultBuilder = new UriBuilder(host)
    {
        Path = $"/go/{shortenedRouteSegment}"
    };

    logger.LogInformation("Returning shortened URL: {ShortenedUrl} for original URL: {OriginalUrl}", resultBuilder.Uri, url);

    // Returns a 200 OK response with the shortened URL in the JSON body
    return Results.Json(new
    {
        original = url,
        shortened = resultBuilder.Uri
    });
}

static async Task<IResult> Redirect(IGrainFactory grains, string shortenedRouteSegment, ILogger<Program> logger)
{
    logger.LogInformation("Received redirect request for shortened route: {ShortenedRouteSegment}", shortenedRouteSegment);

    // Retrieve the grain using the shortened ID and url to the original URL
    var shortenerGrain = grains.GetGrain<IUrlShortenerGrain>(shortenedRouteSegment);

    string url;
    try
    {
        // Gets the URL from the grain
        url = await shortenerGrain.GetUrl();
        
        if (string.IsNullOrWhiteSpace(url))
        {
            logger.LogWarning("No URL found for shortened route: {ShortenedRouteSegment}", shortenedRouteSegment);
            return Results.NotFound($"Shortened URL '{shortenedRouteSegment}' not found.");
        }

        logger.LogInformation("Found URL: {Url} for shortened route: {ShortenedRouteSegment}", url, shortenedRouteSegment);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to retrieve URL for shortened route: {ShortenedRouteSegment}", shortenedRouteSegment);
        return Results.Problem("Failed to retrieve URL. Please try again.");
    }

    // Handles missing schemes, defaults to "http://"
    var redirectBuilder = new UriBuilder(url);

    logger.LogInformation("Redirecting to: {RedirectUrl}", redirectBuilder.Uri);

    // Returns a 302 Found response with a redirect to the original URL in the Location header
    return Results.Redirect(
        url: redirectBuilder.Uri.ToString(),
        permanent: false,
        preserveMethod: false
    );
}
#endregion
