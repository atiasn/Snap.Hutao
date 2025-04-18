// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Caching.Memory;
using Snap.Hutao.Core.DependencyInjection.Annotation.HttpClient;
using Snap.Hutao.Service.Game.Island;
using Snap.Hutao.Web.Endpoint.Hutao;
using System.Net.Http;
using System.Net.Http.Json;

namespace Snap.Hutao.Service.Feature;

[ConstructorGenerated]
[Injection(InjectAs.Singleton, typeof(IFeatureService))]
[HttpClient(HttpClientConfiguration.Default)]
internal sealed partial class FeatureService : IFeatureService
{
    private readonly IHutaoEndpointsFactory hutaoEndpointsFactory;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly IMemoryCache memoryCache;

    public async ValueTask<IslandFeature?> GetIslandFeatureAsync(string tag)
    {
        return await memoryCache.GetOrCreateAsync($"{nameof(FeatureService)}.IslandFeature.{tag}", async entry =>
        {
            entry.SetSlidingExpiration(TimeSpan.FromMinutes(5));
            using (IServiceScope scope = serviceScopeFactory.CreateScope())
            {
                IHttpClientFactory httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
                using (HttpClient httpClient = httpClientFactory.CreateClient(nameof(FeatureService)))
                {
                    string url = hutaoEndpointsFactory.Create().Feature($"UnlockerIsland_Compact2_{tag}");
                    return await httpClient.GetFromJsonAsync<IslandFeature>(url).ConfigureAwait(false);
                }
            }
        }).ConfigureAwait(false);
    }
}