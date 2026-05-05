using AzureFunctions.Demo.Models;

namespace AzureFunctions.Demo.Services;

public interface IProductoService
{
    IReadOnlyList<Producto> Buscar(BuscarProductosQuery query, out int totalSinPaginar);
    Producto? GetById(string id);
    Producto Crear(CrearProductoDto dto);
    Producto? Actualizar(string id, ActualizarProductoDto dto);
    bool Eliminar(string id);

    // Estadísticas agregadas usadas por los Timer triggers (slide 9).
    CatalogoStats GetStats();
}
