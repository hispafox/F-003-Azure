using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AppService.Demo.Api.Tests;

public class OrdersEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Post_Order_Returns_OrderId_And_Computed_Amount()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/demo/orders", new
        {
            sku = "SKU-001",
            quantity = 3,
            unitPrice = 9.50m,
            priority = "high"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.StartsWith("ORD-", root.GetProperty("orderId").GetString());
        Assert.Equal(3, root.GetProperty("quantity").GetInt32());
        Assert.Equal(28.5, root.GetProperty("amount").GetDouble(), 2);
        Assert.Equal("high", root.GetProperty("priority").GetString());
    }

    [Fact]
    public async Task Post_Order_With_Zero_Quantity_Returns_400()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/demo/orders", new
        {
            sku = "SKU-001",
            quantity = 0,
            unitPrice = 9.50m
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
