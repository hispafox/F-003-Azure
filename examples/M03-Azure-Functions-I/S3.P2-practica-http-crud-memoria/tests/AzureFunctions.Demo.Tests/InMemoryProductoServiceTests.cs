using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;

namespace AzureFunctions.Demo.Tests;

// Slide 15 — Tests del repositorio. La capa más simple del proyecto:
// in-memory, sin dependencias externas, ideal para introducir xUnit a
// quien venga del M02.
public class InMemoryProductoServiceTests
{
    [Fact]
    public void Listar_Tras_Seed_Devuelve_3_Productos()
    {
        var repo = new InMemoryProductoService();

        Assert.Equal(3, repo.Listar().Count);
        Assert.Equal(3, repo.Total);
    }

    [Fact]
    public void GetById_Con_Id_Seed_Devuelve_Producto()
    {
        var repo = new InMemoryProductoService();

        var p = repo.GetById("p001");

        Assert.NotNull(p);
        Assert.Equal("Laptop Dell", p!.Nombre);
    }

    [Fact]
    public void GetById_Inexistente_Devuelve_Null()
    {
        var repo = new InMemoryProductoService();

        Assert.Null(repo.GetById("no-existe"));
    }

    [Fact]
    public void Crear_Genera_Id_Unico_Con_Prefijo_p()
    {
        var repo = new InMemoryProductoService();

        var nuevo = repo.Crear(new CrearProductoDto("Nuevo", 10m, 1));

        Assert.StartsWith("p", nuevo.Id);
        Assert.NotEqual("p001", nuevo.Id);
        Assert.Equal("Nuevo", nuevo.Nombre);
        Assert.Equal(4, repo.Total);
    }

    [Fact]
    public void Actualizar_Con_Id_Existente_Reemplaza()
    {
        var repo = new InMemoryProductoService();

        var actualizado = repo.Actualizar("p001", new CrearProductoDto("Laptop XPS", 1499m, 2));

        Assert.NotNull(actualizado);
        Assert.Equal("Laptop XPS", actualizado!.Nombre);
        Assert.Equal(3, repo.Total); // sigue habiendo 3
    }

    [Fact]
    public void Actualizar_Con_Id_Inexistente_Devuelve_Null_Y_No_Crea()
    {
        var repo = new InMemoryProductoService();

        var actualizado = repo.Actualizar("zzz", new CrearProductoDto("X", 1m, 1));

        Assert.Null(actualizado);
        Assert.Null(repo.GetById("zzz"));
        Assert.Equal(3, repo.Total);
    }

    [Fact]
    public void Borrar_Con_Id_Existente_Devuelve_True()
    {
        var repo = new InMemoryProductoService();

        Assert.True(repo.Borrar("p001"));
        Assert.Null(repo.GetById("p001"));
        Assert.Equal(2, repo.Total);
    }

    [Fact]
    public void Borrar_Con_Id_Inexistente_Devuelve_False()
    {
        var repo = new InMemoryProductoService();

        Assert.False(repo.Borrar("no-existe"));
        Assert.Equal(3, repo.Total);
    }

    [Fact]
    public void Operaciones_Sobre_Diccionario_Son_Thread_Safe()
    {
        // El ConcurrentDictionary tras 100 inserts paralelos no pierde
        // datos. Esto es lo que justifica usar Singleton + Concurrent
        // en vez de Dictionary normal.
        var repo = new InMemoryProductoService();

        Enumerable.Range(0, 100).AsParallel()
            .ForAll(i => repo.Crear(new CrearProductoDto($"Item-{i}", 1m, 1)));

        Assert.Equal(103, repo.Total); // 3 seed + 100 nuevos
    }
}
