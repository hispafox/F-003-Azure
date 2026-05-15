using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;
using Microsoft.Azure.Functions.Worker;

namespace AzureFunctions.Demo.Functions;

// Slide 7 — la unidad de trabajo del fan-out. Una activity por factura;
// el orquestador lanza N en paralelo y consolida.
public sealed class FacturaActivities(InMemoryFacturacionService facturacion)
{
    [Function(nameof(ProcesarFactura))]
    public ResultadoFactura ProcesarFactura([ActivityTrigger] Factura factura)
        => facturacion.Procesar(factura);
}
