namespace AzureFunctions.Demo.Models;

// Slide 7 — fan-out/fan-in: se procesan N facturas en paralelo y luego
// se consolidan en un ResumenLote.
public sealed record Factura(string Id, string ClienteId, decimal Importe);

public sealed record ResultadoFactura(string FacturaId, bool Exito, decimal Importe, string? Error);

public sealed record ResumenLote(
    int Total,
    int Exitosas,
    int Fallidas,
    decimal ImporteTotal);
