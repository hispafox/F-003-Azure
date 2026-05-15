using System.Text.Json;
using AzureFunctions.Demo.Models;

namespace AzureFunctions.Demo.Services;

public sealed class PedidosOrquestador : IPedidosOrquestador
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public (IReadOnlyList<string> errores, Pedido? pedido, string? mensajeSerializado)
        ValidarYPreparar(CrearPedidoDto? dto)
    {
        var errores = new List<string>();

        if (dto is null)
        {
            errores.Add("Body es obligatorio");
            return (errores, null, null);
        }

        if (string.IsNullOrWhiteSpace(dto.ClienteId)) errores.Add("ClienteId es obligatorio");
        if (string.IsNullOrWhiteSpace(dto.ClienteEmail)) errores.Add("ClienteEmail es obligatorio");
        else if (!dto.ClienteEmail.Contains('@')) errores.Add("ClienteEmail no tiene formato válido");
        if (dto.Total <= 0) errores.Add("Total debe ser mayor que 0");

        if (errores.Count > 0) return (errores, null, null);

        var pedido = new Pedido(
            Id: Guid.NewGuid().ToString(),
            ClienteId: dto!.ClienteId,
            ClienteEmail: dto.ClienteEmail,
            Total: dto.Total,
            Notas: dto.Notas,
            CreadoEn: DateTimeOffset.UtcNow);

        var mensajeSerializado = JsonSerializer.Serialize(pedido, JsonOpts);

        return (Array.Empty<string>(), pedido, mensajeSerializado);
    }
}
