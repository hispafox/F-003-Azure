using AzureFunctions.Demo.Models;

namespace AzureFunctions.Demo.Services;

// Slide 9 — espejo en memoria del contenedor "resumenes-clientes".
// El segundo trigger escribe a Cosmos vía [CosmosDBOutput]; este servicio
// guarda una copia local para que los tests y los endpoints HTTP puedan
// inspeccionar lo que el Change Feed produjo (en producción harías un
// query directo a Cosmos).
public interface IResumenClienteService
{
    void Upsert(IEnumerable<ResumenCliente> resumenes);

    ResumenCliente? Get(string clienteId);

    IReadOnlyCollection<ResumenCliente> ListarTodos();

    int Total { get; }
}
