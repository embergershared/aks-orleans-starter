using Orleans;

namespace OrleansURLShortener
{
    [GenerateSerializer, Alias(nameof(UrlDetails))]
    public sealed record class UrlDetails
    {
        [Id(0)]
        public string FullUrl { get; set; } = "";

        [Id(1)]
        public string ShortenedRouteSegment { get; set; } = "";
    }
}
