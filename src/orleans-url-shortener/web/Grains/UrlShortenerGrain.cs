using Orleans.Runtime;
using Orleans.Samples.UrlShortener.Web.Models;
using Microsoft.Extensions.Logging;

namespace Orleans.Samples.UrlShortener.Web.Grains;

public interface IUrlShortenerGrain : IGrainWithStringKey
{
    Task SetUrl(string fullUrl);

    Task<string> GetUrl();
}

public sealed class UrlShortenerGrain(
    [PersistentState(
        stateName: "url",
        storageName: "urls")]
        IPersistentState<UrlDetails> state,
    ILogger<UrlShortenerGrain> logger)
    : Grain, IUrlShortenerGrain
{
    public async Task SetUrl(string fullUrl)
    {
        var grainKey = this.GetPrimaryKeyString();
        logger.LogInformation("Setting URL for grain {GrainKey}: {FullUrl}", grainKey, fullUrl);

        state.State = new()
        {
            ShortenedRouteSegment = grainKey,
            FullUrl = fullUrl
        };

        try
        {
            await state.WriteStateAsync();
            logger.LogInformation("Successfully persisted URL state for grain {GrainKey}", grainKey);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist URL state for grain {GrainKey}", grainKey);
            throw;
        }
    }

    public Task<string> GetUrl()
    {
        var grainKey = this.GetPrimaryKeyString();
        var url = state.State.FullUrl;
        
        if (string.IsNullOrWhiteSpace(url))
        {
            logger.LogWarning("No URL found in state for grain {GrainKey}", grainKey);
        }
        else
        {
            logger.LogInformation("Retrieved URL for grain {GrainKey}: {Url}", grainKey, url);
        }

        return Task.FromResult(url);
    }
}