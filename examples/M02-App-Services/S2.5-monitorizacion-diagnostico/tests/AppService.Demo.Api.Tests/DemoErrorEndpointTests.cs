using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AppService.Demo.Api.Tests;

public class DemoErrorEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Type_500_Returns_500()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/demo/error?type=500");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task Type_Exception_Surfaces_As_500()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            // Por defecto WebApplicationFactory propaga la excepción al test;
            // queremos que la conversión a 500 que hace ASP.NET sea visible.
            HandleCookies = false
        });

        var response = await client.GetAsync("/demo/error?type=exception");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_Type_Returns_400()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/demo/error?type=foo");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Type_DependencyFail_Returns_502()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/demo/error?type=dependency-fail");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }
}
