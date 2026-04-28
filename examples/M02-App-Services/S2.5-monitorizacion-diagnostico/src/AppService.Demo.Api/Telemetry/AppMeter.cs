using System.Diagnostics.Metrics;

namespace AppService.Demo.Api.Telemetry;

// Slide 22 — Custom metrics con IMeterFactory. El nombre del Meter
// ("AppService.Demo.Api") es el filtro que registramos en OTel para que
// estas métricas viajen al exporter de Azure Monitor.
public sealed class AppMeter
{
    public const string MeterName = "AppService.Demo.Api";

    public AppMeter(IMeterFactory factory)
    {
        var meter = factory.Create(MeterName);

        OrdersCreated = meter.CreateCounter<long>(
            name: "demo.orders.created",
            unit: "{order}",
            description: "Total orders processed by the demo endpoint");

        OrderAmountTotal = meter.CreateCounter<double>(
            name: "demo.orders.amount.total",
            unit: "EUR",
            description: "Sum of EUR processed across orders");

        OrderProcessingDuration = meter.CreateHistogram<double>(
            name: "demo.orders.duration",
            unit: "ms",
            description: "Time spent processing each order");
    }

    public Counter<long> OrdersCreated { get; }
    public Counter<double> OrderAmountTotal { get; }
    public Histogram<double> OrderProcessingDuration { get; }
}
