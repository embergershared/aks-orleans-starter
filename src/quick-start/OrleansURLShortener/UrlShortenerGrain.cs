using Orleans;
using Orleans.Runtime;
using System.Threading.Tasks;

namespace OrleansURLShortener
{
    public sealed class UrlShortenerGrain(
        [PersistentState(
        stateName: "url",
        storageName: "urls")]
        IPersistentState<UrlDetails> state)
        : Grain, IUrlShortenerGrain
    {
        public async Task SetUrl(string fullUrl)
        {
            state.State = new()
            {
                ShortenedRouteSegment = this.GetPrimaryKeyString(),
                FullUrl = fullUrl
            };

            await state.WriteStateAsync();
        }

        public Task<string> GetUrl() =>
            Task.FromResult(state.State.FullUrl);
    }
}
