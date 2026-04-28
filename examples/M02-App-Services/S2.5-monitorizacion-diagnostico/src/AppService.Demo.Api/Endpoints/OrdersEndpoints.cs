using System.Diagnostics;
using AppService.Demo.Api.Telemetry;

namespace AppService.Demo.Api.Endpoints;

public sealed record OrderRequest(string Sku, int Quantity, decimal UnitPrice, string Priority = "normal");

public static class OrdersEndpoints
{
    public static IEndpointRouteBuilder MapOrders(this IEndpointRouteBuilder app)
    {
        // Slide 22 — Cada llamada incrementa los counters de AppMeter y registra
        // la duración en el histogram. En Application Insights estas métricas
        // aparecen como "Custom metrics" y se pueden graficar y alertar.
        // Slide 21 — SetTag enriquece el span de tracing con datos de negocio.
        app.MapPost("/demo/orders", (
            OrderRequest request,
            AppMeter metrics,
            ILogger<Program> logger) =>
        {
            if (request.Quantity <= 0)
            {
                return Results.BadRequest(new { error = "Quantity must be positive" });
            }

            var sw = Stopwatch.StartNew();

            // Simular trabajo de creación del pedido (sin tocar BD ni nada externo).
            var orderId = $"ORD-{Guid.NewGuid().ToString("N")[..10].ToUpperInvariant()}";
            var amount = (double)(request.UnitPrice * request.Quantity);

            // Slide 21 — distributed tracing: tags que aparecen en cada span
            var activity = Activity.Current;
            activity?.SetTag("order.id", orderId);
            activity?.SetTag("order.sku", request.Sku);
            activity?.SetTag("order.priority", request.Priority);

            // Slide 22 — custom metrics con dimensiones (tags)
            var priorityTag = new KeyValuePair<string, object?>("priority", request.Priority);
            metrics.OrdersCreated.Add(1, priorityTag);
            metrics.OrderAmountTotal.Add(amount, priorityTag);

            sw.Stop();
            metrics.OrderProcessingDuration.Record(sw.Elapsed.TotalMilliseconds, priorityTag);

            // Slide 23 — structured logging: cada {placeholder} es un campo
            // separado en App Insights, no parte del mensaje.
            logger.LogInformation(
                "Order {OrderId} created with sku {Sku} x{Quantity} ({Priority})",
                orderId, request.Sku, request.Quantity, request.Priority);

            return Results.Ok(new
            {
                orderId,
                sku = request.Sku,
                quantity = request.Quantity,
                amount,
                priority = request.Priority,
                processingMs = sw.Elapsed.TotalMilliseconds
            });
        });

        return app;
    }
}
