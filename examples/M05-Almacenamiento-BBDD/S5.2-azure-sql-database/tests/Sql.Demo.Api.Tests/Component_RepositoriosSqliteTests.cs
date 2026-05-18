using Sql.Demo.Api.Domain;
using Sql.Demo.Api.Repositories;

namespace Sql.Demo.Api.Tests;

// CAPA 2 — el modelo EF Core + la lógica de los repos contra una BD
// relacional REAL (SQLite in-memory), sin Docker ni Azure. Aquí vive el
// valor: la regla de negocio de "crear pedido" (slide 7, 12, 31).
[Trait("Category", "Component")]
public sealed class Component_RepositoriosSqliteTests : IDisposable
{
    private readonly SqliteContextFactory _factory = new();

    [Fact]
    public async Task Producto_Crud_Completo()
    {
        var crear = new ProductoRepository(_factory.NewContext());
        var p = await crear.CrearAsync(new CrearProductoDto("Monitor 27\"", 199.99m, 10));
        Assert.True(p.Id > 0);

        var get = await new ProductoRepository(_factory.NewContext()).GetAsync(p.Id);
        Assert.Equal("Monitor 27\"", get!.Nombre);

        var act = await new ProductoRepository(_factory.NewContext())
            .ActualizarAsync(p.Id, new ActualizarProductoDto(149.99m, 5));
        Assert.Equal(149.99m, act!.Precio);
        Assert.Equal(5, act.Stock);

        Assert.True(await new ProductoRepository(_factory.NewContext()).BorrarAsync(p.Id));
        Assert.Null(await new ProductoRepository(_factory.NewContext()).GetAsync(p.Id));
    }

    [Fact]
    public async Task Listar_Ordena_Por_Nombre()
    {
        var repo = new ProductoRepository(_factory.NewContext());
        await repo.CrearAsync(new CrearProductoDto("Zapatos", 50m, 1));
        await new ProductoRepository(_factory.NewContext())
            .CrearAsync(new CrearProductoDto("Abrigo", 80m, 1));

        var lista = await new ProductoRepository(_factory.NewContext()).ListarAsync();
        Assert.Equal(["Abrigo", "Zapatos"], lista.Select(p => p.Nombre));
    }

    [Fact]
    public async Task CrearPedido_Ok_Descuenta_Stock_Y_Calcula_Total()
    {
        var prod = await new ProductoRepository(_factory.NewContext())
            .CrearAsync(new CrearProductoDto("Silla", 120.00m, 10));

        var (resultado, pedido) = await new PedidoRepository(_factory.NewContext())
            .CrearAsync(new CrearPedidoDto(prod.Id, 3));

        Assert.Equal(CrearPedidoResultado.Ok, resultado);
        Assert.Equal(360.00m, pedido!.Total);          // 120 × 3
        Assert.Equal("Silla", pedido.ProductoNombre);

        var tras = await new ProductoRepository(_factory.NewContext()).GetAsync(prod.Id);
        Assert.Equal(7, tras!.Stock);                  // 10 − 3
    }

    [Fact]
    public async Task CrearPedido_StockInsuficiente_No_Toca_Stock()
    {
        var prod = await new ProductoRepository(_factory.NewContext())
            .CrearAsync(new CrearProductoDto("Mesa", 200m, 2));

        var (resultado, pedido) = await new PedidoRepository(_factory.NewContext())
            .CrearAsync(new CrearPedidoDto(prod.Id, 5));

        Assert.Equal(CrearPedidoResultado.StockInsuficiente, resultado);
        Assert.Null(pedido);
        Assert.Equal(2, (await new ProductoRepository(_factory.NewContext())
            .GetAsync(prod.Id))!.Stock);
    }

    [Fact]
    public async Task CrearPedido_ProductoNoExiste()
    {
        var (resultado, _) = await new PedidoRepository(_factory.NewContext())
            .CrearAsync(new CrearPedidoDto(9999, 1));
        Assert.Equal(CrearPedidoResultado.ProductoNoExiste, resultado);
    }

    [Fact]
    public async Task Listar_Pedidos_Trae_Nombre_Producto_Sin_NMas1()
    {
        var prod = await new ProductoRepository(_factory.NewContext())
            .CrearAsync(new CrearProductoDto("Lámpara", 30m, 100));
        await new PedidoRepository(_factory.NewContext())
            .CrearAsync(new CrearPedidoDto(prod.Id, 2));

        var pedidos = await new PedidoRepository(_factory.NewContext()).ListarAsync();

        var dto = Assert.Single(pedidos);
        Assert.Equal("Lámpara", dto.ProductoNombre); // proyección con Include
    }

    public void Dispose() => _factory.Dispose();
}
