using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace AzureFunctions.Demo.Middleware;

// Slide 14 — Lee X-Correlation-Id del request (o lo genera) y lo añade a la
// respuesta. Patrón estándar para correlacionar logs de extremo a extremo.
public sealed class CorrelationIdMiddleware : IFunctionsWorkerMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var http = context.GetHttpContext();
        if (http is not null)
        {
            var correlationId = http.Request.Headers[HeaderName].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = Guid.NewGuid().ToString("N");
            }

            http.Response.Headers[HeaderName] = correlationId;
            http.Items[HeaderName] = correlationId;
        }

        await next(context);
    }
}
