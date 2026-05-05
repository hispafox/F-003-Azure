using AzureFunctions.Demo.Models;

namespace AzureFunctions.Demo.Services;

public interface ICsvProductosImporter
{
    // Procesa un CSV de productos. Cabecera esperada:
    //   nombre,categoria,precio,stock
    //
    // Por cada línea válida llama a IProductoService.Crear().
    // Líneas inválidas (campos faltantes, números mal formados, etc.) se
    // contabilizan en LineasError y se reportan en Errores.
    ImportResultado Import(string nombreArchivo, string csvContent);
}
