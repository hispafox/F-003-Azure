using AzureFunctions.Demo.Functions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AzureFunctions.Demo.Tests;

public class PingFunctionTests
{
    [Fact]
    public void Ping_Returns_Pong()
    {
        var function = new PingFunction();

        var result = function.Ping(new DefaultHttpContext().Request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var status = ok.Value!.GetType().GetProperty("status")!.GetValue(ok.Value);
        Assert.Equal("pong", status);
    }
}
