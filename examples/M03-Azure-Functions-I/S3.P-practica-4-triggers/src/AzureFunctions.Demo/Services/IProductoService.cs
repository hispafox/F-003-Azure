using AzureFunctions.Demo.Models;

namespace AzureFunctions.Demo.Services;

public interface IProductoService
{
    IReadOnlyList<Producto> Listar();
    Producto? GetById(string id);
    Producto Crear(CrearProductoDto dto);
    int Total { get; }
}
