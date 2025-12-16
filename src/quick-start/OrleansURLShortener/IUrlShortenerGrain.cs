using Orleans;
using System.Threading.Tasks;

namespace OrleansURLShortener
{
    [Alias("IUrlShortenerGrain")]
    public interface IUrlShortenerGrain : IGrainWithStringKey
    {
        [Alias("SetUrl")]
        Task SetUrl(string fullUrl);

        [Alias("GetUrl")]
        Task<string> GetUrl();
    }
}