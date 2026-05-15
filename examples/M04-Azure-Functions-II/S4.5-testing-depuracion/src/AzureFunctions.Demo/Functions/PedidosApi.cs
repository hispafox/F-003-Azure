using System.Text.Json;
using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace AzureFunctions.Demo.Functions;

// Slide 6 — la función es "pegamento": deserializa, delega en el servicio,
// formatea la respuesta. NADA de lógica de negocio aquí → el test mockea
// IDescuentoCalculator y solo verifica el wiring (200/400/forma).
public sealed class PedidosApi
{
    private readonly IDescuentoCalculator _calculator;

    public PedidosApi(IDescuentoCalculator calculator)
    {
        _calculator = calculator;
    }

    [Function(nameof(CalcularDescuento))]
    public async Task<IActionResult> CalcularDescuento(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "pedidos/descuento")]
        HttpRequest req)
    {
        Pedido? pedido;
        try
        {
            pedido = await JsonSerializer.DeserializeAsync<Pedido>(
                req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return new BadRequestObjectResult(new { error = "Body JSON inválido" });
        }

        if (pedido is null || string.IsNullOrWhiteSpace(pedido.Id))
            return new BadRequestObjectResult(new { error = "Pedido con Id obligatorio" });

        if (pedido.Total < 0)
            return new BadRequestObjectResult(new { error = "Total no puede ser negativo" });

        return new OkObjectResult(_calculator.Aplicar(pedido));
    }
}
