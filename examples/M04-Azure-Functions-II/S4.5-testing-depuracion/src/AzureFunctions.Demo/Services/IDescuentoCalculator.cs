using AzureFunctions.Demo.Models;

namespace AzureFunctions.Demo.Services;

// Slide 7 — la LÓGICA DE NEGOCIO vive aquí, no en la función. Es lo que
// más importa testear: rápido, sin Azure, con [Theory] escalonada.
public interface IDescuentoCalculator
{
    decimal CalcularDescuento(decimal total);
    PedidoConDescuento Aplicar(Pedido pedido);
}

public sealed class DescuentoCalculator : IDescuentoCalculator
{
    // Descuento escalonado por importe:
    //   < 100€  → 0%
    //   [100,500) → 5%
    //   [500,1000) → 10%
    //   >= 1000€ → 15%
    public decimal CalcularDescuento(decimal total)
    {
        if (total < 0) throw new ArgumentOutOfRangeException(nameof(total), "Total no puede ser negativo");

        var pct = total switch
        {
            < 100m => 0m,
            < 500m => 0.05m,
            < 1000m => 0.10m,
            _ => 0.15m,
        };
        return Math.Round(total * pct, 2);
    }

    public PedidoConDescuento Aplicar(Pedido pedido)
    {
        var desc = CalcularDescuento(pedido.Total);
        return new PedidoConDescuento(pedido.Id, pedido.Total, desc, pedido.Total - desc);
    }
}
