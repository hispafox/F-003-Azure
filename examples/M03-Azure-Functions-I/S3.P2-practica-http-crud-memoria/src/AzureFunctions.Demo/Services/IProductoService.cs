using AzureFunctions.Demo.Models;

namespace AzureFunctions.Demo.Services;

// Slide 5 — Repositorio en memoria. CRUD completo:
//   Listar    -> GET /productos
//   GetById   -> GET /productos/{id}
//   Crear     -> POST /productos
//   Actualizar-> PUT /productos/{id}
//   Borrar    -> DELETE /productos/{id}
public interface IProductoService
{
    IReadOnlyList<Producto> Listar();
    Producto? GetById(string id);
    Producto Crear(CrearProductoDto dto);
    Producto? Actualizar(string id, CrearProductoDto dto);
    bool Borrar(string id);
    int Total { get; }
}
