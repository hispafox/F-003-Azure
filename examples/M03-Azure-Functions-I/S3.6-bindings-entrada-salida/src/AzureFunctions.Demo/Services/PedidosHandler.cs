using AzureFunctions.Demo.Models;

namespace AzureFunctions.Demo.Services;

public sealed class PedidosHandler : IPedidosHandler
{
    public (IReadOnlyList<ValidationError> errores, Pedido? pedido) ValidarYConstruir(CrearPedidoDto? dto)
    {
        var errores = new List<ValidationError>();

        if (dto is null)
        {
            errores.Add(new ValidationError("body", "El body es obligatorio"));
            return (errores, null);
        }

        if (string.IsNullOrWhiteSpace(dto.ClienteId))
            errores.Add(new ValidationError(nameof(dto.ClienteId), "ClienteId es obligatorio"));
        else if (dto.ClienteId.Length is < 3 or > 64)
            errores.Add(new ValidationError(nameof(dto.ClienteId), "ClienteId debe tener entre 3 y 64 caracteres"));

        if (dto.Total <= 0)
            errores.Add(new ValidationError(nameof(dto.Total), "Total debe ser mayor que 0"));

        if (dto.Notas is { Length: > 500 })
            errores.Add(new ValidationError(nameof(dto.Notas), "Notas no puede superar los 500 caracteres"));

        if (errores.Count > 0)
            return (errores, null);

        var pedido = new Pedido
        {
            Id = Guid.NewGuid().ToString(),
            ClienteId = dto!.ClienteId,
            Total = dto.Total,
            Notas = dto.Notas,
            Estado = "nuevo",
            CreadoEn = DateTimeOffset.UtcNow,
        };

        return (Array.Empty<ValidationError>(), pedido);
    }
}
