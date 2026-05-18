using AzureFunctions.Demo.Functions;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace AzureFunctions.Demo.Tests;

// Orquestador testeado con TaskOrchestrationContext mockeado (NSubstitute).
// Gotcha (S4.2): CreateReplaySafeLogger<T>() devuelve null por defecto en
// el mock → configurarlo a NullLogger o el orquestador peta al loguear.
public class SaludosOrchestratorTests
{
    private static TaskOrchestrationContext NewContext(List<string>? input)
    {
        var ctx = Substitute.For<TaskOrchestrationContext>();
        ctx.GetInput<List<string>>().Returns(input);
        ctx.CreateReplaySafeLogger<SaludosOrchestrator>()
            .Returns(NullLogger<SaludosOrchestrator>.Instance);
        return ctx;
    }

    [Fact]
    public async Task Fan_Out_Fan_In_Consolida_Un_Saludo_Por_Nombre()
    {
        var ctx = NewContext(["Ana", "Luis", "Marta"]);
        // Cada CallActivityAsync<string>(Saludar, nombre) devuelve el saludo.
        ctx.CallActivityAsync<string>(
                Arg.Is<TaskName>(n => n.Name == nameof(SaludarActivity.Saludar)),
                Arg.Any<object>(), Arg.Any<TaskOptions?>())
            .Returns(ci => Task.FromResult($"¡Hola, {ci.ArgAt<object>(1)}!"));

        var sut = new SaludosOrchestrator();
        var saludos = await sut.SaludarATodos(ctx);

        Assert.Equal(3, saludos.Count);
        Assert.Contains("¡Hola, Ana!", saludos);
        Assert.Contains("¡Hola, Luis!", saludos);
        Assert.Contains("¡Hola, Marta!", saludos);
        // 3 activities lanzadas (fan-out).
        await ctx.Received(3).CallActivityAsync<string>(
            Arg.Is<TaskName>(n => n.Name == nameof(SaludarActivity.Saludar)),
            Arg.Any<object>(), Arg.Any<TaskOptions?>());
    }

    [Fact]
    public async Task Lista_Vacia_Devuelve_Vacio_Sin_Llamar_Activities()
    {
        var ctx = NewContext([]);
        var sut = new SaludosOrchestrator();

        var saludos = await sut.SaludarATodos(ctx);

        Assert.Empty(saludos);
        await ctx.DidNotReceive().CallActivityAsync<string>(
            Arg.Any<TaskName>(), Arg.Any<object>(), Arg.Any<TaskOptions?>());
    }

    [Fact]
    public async Task Input_Null_Se_Trata_Como_Vacio()
    {
        var ctx = NewContext(null);
        var sut = new SaludosOrchestrator();

        Assert.Empty(await sut.SaludarATodos(ctx));
    }
}
