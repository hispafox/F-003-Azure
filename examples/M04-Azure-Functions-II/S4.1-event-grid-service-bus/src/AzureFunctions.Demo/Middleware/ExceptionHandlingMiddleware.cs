using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Demo.Middleware;

// Slides 14 + 24 — Middleware que captura excepciones no controladas y
// devuelve Problem Details (RFC 7807) en lugar de un 500 genérico.
public sealed class ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger)
    : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Excepcion no controlada en {FunctionName}",
                context.FunctionDefinition.Name);

            var http = context.GetHttpContext();
            if (http is null) throw; // si no es HTTP, deja que la excepción suba

            var problem = new ProblemDetails
            {
                Type = "https://aka.ms/functions/errors/unhandled",
                Title = "Internal Server Error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "Se ha producido un error inesperado procesando la peticion.",
                Instance = http.Request.Path
            };
            problem.Extensions["traceId"] = context.InvocationId;

            http.Response.StatusCode = StatusCodes.Status500InternalServerError;
            http.Response.ContentType = "application/problem+json";
            await http.Response.WriteAsJsonAsync(problem);
        }
    }
}
