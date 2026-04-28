using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AppService.Demo.Api.Tests;

public class LoadEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task LoadCpu_Burns_For_At_Least_The_Requested_Time()
    {
        using var client = factory.CreateClient();

        var sw = Stopwatch.StartNew();
        var response = await client.GetAsync("/load/cpu?ms=80");
        sw.Stop();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(sw.ElapsedMilliseconds >= 75,
            $"expected to burn at least 75ms but elapsed was {sw.ElapsedMilliseconds}ms");

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(80, doc.RootElement.GetProperty("generatedMs").GetInt32());
        Assert.True(doc.RootElement.GetProperty("primesFound").GetInt32() > 0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    [InlineData(60_001)]
    [InlineData(99_999_999)]
    public async Task LoadCpu_Returns_400_For_OutOfRange_Ms(int ms)
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/load/cpu?ms={ms}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
