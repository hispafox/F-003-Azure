using AzureFunctions.Demo.Models;

namespace AzureFunctions.Demo.Services;

// Slide 5/6 — TODO el I/O y la lógica de negocio vive en servicios, NUNCA
// en el orquestador (que debe ser determinista). Las activities son adaptadores
// finos que delegan en estos servicios; los servicios se testean directos.

public interface IPedidoValidador
{
    // Lanza InvalidOperationException si el pedido no es válido.
    void Validar(Pedido pedido);
}

public interface IInventarioService
{
    Reserva Reservar(Pedido pedido);
    void Liberar(string reservaId);
}

public interface IPagoService
{
    // Lanza PagoRechazadoException si el pago falla de forma permanente.
    Pago Cobrar(Pedido pedido, Reserva reserva);
}

public interface INotificacionService
{
    void EnviarConfirmacion(Pedido pedido, Pago pago);
    void NotificarManager(Pedido pedido);
    void NotificarRechazo(Pedido pedido, string motivo);
}

public sealed class PagoRechazadoException(string mensaje) : Exception(mensaje);
