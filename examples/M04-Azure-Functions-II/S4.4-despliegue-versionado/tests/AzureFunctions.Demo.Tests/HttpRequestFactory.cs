using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace AzureFunctions.Demo.Tests;

// Helper para fabricar HttpRequest con body JSON, query string y route values.
// Es lo que en Minimal API hace WebApplicationFactory; aquí lo armamos a mano
// porque WebApplicationFactory no aplica a Functions.
internal static class HttpRequestFactory
{
    public static HttpRequest WithQuery(string query)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.QueryString = new QueryString(query.StartsWith('?') ? query : "?" + query);
        return ctx.Request;
    }

    public static HttpRequest WithJsonBody<T>(T body)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.ContentType = "application/json";
        var json = JsonSerializer.Serialize(body);
        var bytes = Encoding.UTF8.GetBytes(json);
        ctx.Request.Body = new MemoryStream(bytes);
        ctx.Request.ContentLength = bytes.Length;
        return ctx.Request;
    }

    public static HttpRequest WithRawBody(string body, string contentType = "application/json")
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.ContentType = contentType;
        var bytes = Encoding.UTF8.GetBytes(body);
        ctx.Request.Body = new MemoryStream(bytes);
        ctx.Request.ContentLength = bytes.Length;
        return ctx.Request;
    }

    public static HttpRequest Empty() => new DefaultHttpContext().Request;
}
