using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AppService.Demo.Api.Tests;

public class LogDemoEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task LogDemo_Scrubs_Email_From_Echo_Response()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/demo/log", new
        {
            message = "Cliente pedro@example.com lanzó pedido"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("pedro@example.com", body);
        Assert.Contains("[REDACTED:EMAIL]", body);

        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.GetProperty("redactionsApplied").GetBoolean());
    }

    [Fact]
    public async Task LogDemo_Reports_No_Redactions_On_Safe_Message()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/demo/log", new
        {
            message = "Pedido ORD-1 procesado correctamente"
        });

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(doc.RootElement.GetProperty("redactionsApplied").GetBoolean());
    }
}
