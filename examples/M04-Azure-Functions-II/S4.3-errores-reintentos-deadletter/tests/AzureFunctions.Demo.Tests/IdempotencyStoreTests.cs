using AzureFunctions.Demo.Services;

namespace AzureFunctions.Demo.Tests;

public class IdempotencyStoreTests
{
    [Fact]
    public void TryRegistrar_Primera_Vez_Es_True_Segunda_Es_False()
    {
        var store = new InMemoryIdempotencyStore();

        Assert.True(store.TryRegistrar("ped-1"));
        Assert.False(store.TryRegistrar("ped-1"));
        Assert.Equal(1, store.Total);
    }

    [Fact]
    public void YaProcesado_Refleja_El_Registro()
    {
        var store = new InMemoryIdempotencyStore();
        Assert.False(store.YaProcesado("ped-9"));

        store.TryRegistrar("ped-9");

        Assert.True(store.YaProcesado("ped-9"));
    }

    [Fact]
    public void TryRegistrar_Concurrente_Solo_Una_Llamada_Gana()
    {
        // Slide 10 — la entrega at-least-once de SB puede dar el mismo
        // mensaje a dos instancias a la vez. Exactamente UNA debe ganar.
        var store = new InMemoryIdempotencyStore();

        var ganadores = Enumerable.Range(0, 200).AsParallel()
            .Select(_ => store.TryRegistrar("ped-race"))
            .Count(ok => ok);

        Assert.Equal(1, ganadores);
        Assert.Equal(1, store.Total);
    }

    [Fact]
    public void Claves_Distintas_No_Colisionan()
    {
        var store = new InMemoryIdempotencyStore();
        Assert.True(store.TryRegistrar("a"));
        Assert.True(store.TryRegistrar("b"));
        Assert.Equal(2, store.Total);
    }
}
