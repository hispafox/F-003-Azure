using AzureFunctions.Demo.Functions;
using AzureFunctions.Demo.Models;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace AzureFunctions.Demo.Tests;

// Slide 5/26 — el orquestador es determinista y delega TODO en activities.
// Lo testeamos mockeando TaskOrchestrationContext (NSubstitute) y
// configurando qué devuelve cada CallActivityAsync por nombre.
public class ProcesarPedidoOrchestratorTests
{
    private static Pedido Pedido(decimal total) => new(
        Id: "ped-1",
        ClienteId: "c-A",
        ClienteEmail: "a@b.c",
        Total: total,
        Items: [new LineaPedido("SKU-1", 1, total)]);

    private static TaskOrchestrationContext NewContext(Pedido input)
    {
        var ctx = Substitute.For<TaskOrchestrationContext>();
        ctx.GetInput<Pedido>().Returns(input);
        ctx.CreateReplaySafeLogger<ProcesarPedidoOrchestrator>()
            .Returns(NullLogger<ProcesarPedidoOrchestrator>.Instance);
        ctx.CurrentUtcDateTime.Returns(new DateTime(2026, 5, 15, 10, 0, 0, DateTimeKind.Utc));

        // Defaults: las activities sin resultado devuelven Task.CompletedTask
        // (si no, NSubstitute devuelve null → NRE al await).
        ctx.CallActivityAsync(Arg.Any<TaskName>(), Arg.Any<object>(), Arg.Any<TaskOptions?>())
            .Returns(Task.CompletedTask);

        return ctx;
    }

    private static void SetupActivity<T>(
        TaskOrchestrationContext ctx, string nombre, T resultado)
    {
        ctx.CallActivityAsync<T>(
                Arg.Is<TaskName>(n => n.Name == nombre),
                Arg.Any<object>(),
                Arg.Any<TaskOptions?>())
            .Returns(Task.FromResult(resultado));
    }

    [Fact]
    public async Task Pedido_Normal_Completa_El_Chaining()
    {
        var pedido = Pedido(1200m);
        var ctx = NewContext(pedido);
        SetupActivity(ctx, nameof(PedidoActivities.ValidarPedido), pedido);
        SetupActivity(ctx, nameof(PedidoActivities.ReservarInventario),
            new Reserva("r1", "ped-1", true));
        SetupActivity(ctx, nameof(PedidoActivities.ProcesarPago),
            new Pago("txn-1", "ped-1", 1200m, true));

        var sut = new ProcesarPedidoOrchestrator();
        var resultado = await sut.ProcesarPedido(ctx);

        Assert.Equal("completado", resultado.Estado);
        Assert.Equal("txn-1", resultado.TransaccionId);

        // No se pide aprobación (1200 < 5000)
        await ctx.DidNotReceive().CallActivityAsync(
            Arg.Is<TaskName>(n => n.Name == nameof(PedidoActivities.NotificarManager)),
            Arg.Any<object>(), Arg.Any<TaskOptions?>());
        // Se envió confirmación
        await ctx.Received().CallActivityAsync(
            Arg.Is<TaskName>(n => n.Name == nameof(PedidoActivities.EnviarConfirmacion)),
            Arg.Any<object>(), Arg.Any<TaskOptions?>());
    }

    [Fact]
    public async Task Pago_Falla_Tras_Reintentos_Activa_La_Compensacion_Saga()
    {
        var pedido = Pedido(99.99m);
        var ctx = NewContext(pedido);
        SetupActivity(ctx, nameof(PedidoActivities.ValidarPedido), pedido);
        SetupActivity(ctx, nameof(PedidoActivities.ReservarInventario),
            new Reserva("r-comp", "ped-1", true));

        // El pago lanza TaskFailedException (lo que Durable lanza tras agotar
        // los reintentos de la RetryPolicy).
        ctx.CallActivityAsync<Pago>(
                Arg.Is<TaskName>(n => n.Name == nameof(PedidoActivities.ProcesarPago)),
                Arg.Any<object>(), Arg.Any<TaskOptions?>())
            .Returns<Task<Pago>>(_ => throw new TaskFailedException(
                nameof(PedidoActivities.ProcesarPago), 1,
                new InvalidOperationException("Tarjeta rechazada")));

        var sut = new ProcesarPedidoOrchestrator();
        var resultado = await sut.ProcesarPedido(ctx);

        Assert.Equal("compensado", resultado.Estado);
        Assert.Null(resultado.TransaccionId);

        // La saga libera la reserva con CompensarPedido(reservaId)
        await ctx.Received().CallActivityAsync(
            Arg.Is<TaskName>(n => n.Name == nameof(PedidoActivities.CompensarPedido)),
            "r-comp", Arg.Any<TaskOptions?>());
        await ctx.Received().CallActivityAsync(
            Arg.Is<TaskName>(n => n.Name == nameof(PedidoActivities.NotificarRechazo)),
            Arg.Any<object>(), Arg.Any<TaskOptions?>());
    }

    [Fact]
    public async Task Pedido_Sobre_Umbral_Espera_Aprobacion_Y_Continua_Si_Aprobado()
    {
        var pedido = Pedido(8500m);
        var ctx = NewContext(pedido);
        SetupActivity(ctx, nameof(PedidoActivities.ValidarPedido), pedido);
        SetupActivity(ctx, nameof(PedidoActivities.ReservarInventario),
            new Reserva("r1", "ped-1", true));
        SetupActivity(ctx, nameof(PedidoActivities.ProcesarPago),
            new Pago("txn-9", "ped-1", 8500m, true));

        // El evento de aprobación llega con true; el timer nunca completa.
        ctx.WaitForExternalEvent<bool>(
                ProcesarPedidoOrchestrator.EventoAprobacion, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        ctx.CreateTimer(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new TaskCompletionSource().Task); // nunca completa

        var sut = new ProcesarPedidoOrchestrator();
        var resultado = await sut.ProcesarPedido(ctx);

        Assert.Equal("completado", resultado.Estado);
        await ctx.Received().CallActivityAsync(
            Arg.Is<TaskName>(n => n.Name == nameof(PedidoActivities.NotificarManager)),
            Arg.Any<object>(), Arg.Any<TaskOptions?>());
    }

    [Fact]
    public async Task Pedido_Sobre_Umbral_Rechazado_Compensa_Y_Devuelve_Rechazado()
    {
        var pedido = Pedido(8500m);
        var ctx = NewContext(pedido);
        SetupActivity(ctx, nameof(PedidoActivities.ValidarPedido), pedido);
        SetupActivity(ctx, nameof(PedidoActivities.ReservarInventario),
            new Reserva("r-rej", "ped-1", true));

        // El manager rechaza (evento = false).
        ctx.WaitForExternalEvent<bool>(
                ProcesarPedidoOrchestrator.EventoAprobacion, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        ctx.CreateTimer(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new TaskCompletionSource().Task);

        var sut = new ProcesarPedidoOrchestrator();
        var resultado = await sut.ProcesarPedido(ctx);

        Assert.Equal("rechazado", resultado.Estado);
        // Nunca se intentó cobrar
        await ctx.DidNotReceive().CallActivityAsync<Pago>(
            Arg.Is<TaskName>(n => n.Name == nameof(PedidoActivities.ProcesarPago)),
            Arg.Any<object>(), Arg.Any<TaskOptions?>());
        // Se compensó la reserva
        await ctx.Received().CallActivityAsync(
            Arg.Is<TaskName>(n => n.Name == nameof(PedidoActivities.CompensarPedido)),
            "r-rej", Arg.Any<TaskOptions?>());
    }

    [Fact]
    public async Task Pedido_Sobre_Umbral_Timeout_Tambien_Rechaza()
    {
        var pedido = Pedido(9000m);
        var ctx = NewContext(pedido);
        SetupActivity(ctx, nameof(PedidoActivities.ValidarPedido), pedido);
        SetupActivity(ctx, nameof(PedidoActivities.ReservarInventario),
            new Reserva("r-to", "ped-1", true));

        // El evento nunca llega; el timer completa → timeout.
        ctx.WaitForExternalEvent<bool>(
                ProcesarPedidoOrchestrator.EventoAprobacion, Arg.Any<CancellationToken>())
            .Returns(new TaskCompletionSource<bool>().Task); // nunca completa
        ctx.CreateTimer(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask); // expira ya

        var sut = new ProcesarPedidoOrchestrator();
        var resultado = await sut.ProcesarPedido(ctx);

        Assert.Equal("rechazado", resultado.Estado);
        Assert.Contains("72h", resultado.Motivo);
    }
}
