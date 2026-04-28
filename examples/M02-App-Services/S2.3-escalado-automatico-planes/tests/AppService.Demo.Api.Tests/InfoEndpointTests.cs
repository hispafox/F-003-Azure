using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AppService.Demo.Api.Tests;

public class InfoEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Info_Exposes_SlotName_And_Splits_Sticky_From_Travelling_Settings()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/info");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("machineName", out _));
        Assert.True(root.TryGetProperty("slotName", out var slot));
        Assert.Equal("local", slot.GetString());

        Assert.True(root.TryGetProperty("travelsWithCode", out var travels));
        Assert.True(travels.TryGetProperty("version", out _));
        Assert.True(travels.TryGetProperty("greeting", out _));

        Assert.True(root.TryGetProperty("stickyToSlot", out var sticky));
        Assert.True(sticky.TryGetProperty("environmentLabel", out _));
        Assert.True(sticky.TryGetProperty("dbConnectionLabel", out _));
        Assert.True(sticky.TryGetProperty("appInsightsLabel", out _));
    }
}
