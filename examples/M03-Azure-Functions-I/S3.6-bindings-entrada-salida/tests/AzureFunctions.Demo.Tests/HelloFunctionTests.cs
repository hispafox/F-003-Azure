using AzureFunctions.Demo.Functions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AzureFunctions.Demo.Tests;

// Tests de Functions isolated worker NO usan WebApplicationFactory.
// Construimos la clase de la funcion directamente y le pasamos un HttpRequest
// fabricado con DefaultHttpContext. Es lo mas simple y rapido.
public class HelloFunctionTests
{
    [Fact]
    public void Hello_With_Name_Returns_Greeting()
    {
        var function = new HelloFunction(NullLogger<HelloFunction>.Instance);
        var ctx = new DefaultHttpContext();
        ctx.Request.QueryString = new QueryString("?name=Pedro");

        var result = function.Hello(ctx.Request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = ok.Value!;
        var mensaje = payload.GetType().GetProperty("mensaje")!.GetValue(payload) as string;
        Assert.Equal("Hello Pedro desde Azure Functions", mensaje);
    }

    [Fact]
    public void Hello_Without_Name_Defaults_To_Azure()
    {
        var function = new HelloFunction(NullLogger<HelloFunction>.Instance);
        var ctx = new DefaultHttpContext();

        var result = function.Hello(ctx.Request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = ok.Value!;
        var mensaje = payload.GetType().GetProperty("mensaje")!.GetValue(payload) as string;
        Assert.Equal("Hello Azure desde Azure Functions", mensaje);
    }

    [Fact]
    public void Hello_Includes_Diagnostic_Fields()
    {
        var function = new HelloFunction(NullLogger<HelloFunction>.Instance);

        var result = function.Hello(new DefaultHttpContext().Request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = ok.Value!;
        var props = payload.GetType().GetProperties().Select(p => p.Name).ToHashSet();

        // Slide 9 — los seis campos diagnosticos esperados estan presentes
        Assert.Contains("entorno", props);
        Assert.Contains("servidor", props);
        Assert.Contains("hora_utc", props);
        Assert.Contains("runtime", props);
        Assert.Contains("os", props);
        Assert.Contains("workerRuntime", props);
    }
}
