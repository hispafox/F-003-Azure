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
        Assert.True(root.TryGetProperty("slotName", out _));

        var travels = root.GetProperty("travelsWithCode");
        Assert.True(travels.TryGetProperty("version", out _));
        Assert.True(travels.TryGetProperty("greeting", out _));
        Assert.True(travels.TryGetProperty("externalApiBaseUrl", out _));
        Assert.True(travels.TryGetProperty("requestTimeoutSeconds", out _));

        var sticky = root.GetProperty("stickyToSlot");
        Assert.True(sticky.TryGetProperty("environmentLabel", out _));
        Assert.True(sticky.TryGetProperty("dbConnectionLabel", out _));

        // Slide 28 — la connection string y la api key NUNCA aparecen en claro
        Assert.Equal("***REDACTED***", sticky.GetProperty("connectionString").GetString());
        Assert.Equal("***REDACTED***", sticky.GetProperty("apiKey").GetString());
    }
}
