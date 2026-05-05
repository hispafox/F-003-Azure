using System.Collections.Concurrent;
using AzureFunctions.Demo.Models;

namespace AzureFunctions.Demo.Services;

public sealed class InMemoryResumenClienteService : IResumenClienteService
{
    private readonly ConcurrentDictionary<string, ResumenCliente> _resumenes = new();

    public void Upsert(IEnumerable<ResumenCliente> resumenes)
    {
        ArgumentNullException.ThrowIfNull(resumenes);
        foreach (var r in resumenes)
        {
            if (string.IsNullOrEmpty(r.ClienteId)) continue;
            _resumenes[r.ClienteId] = r;
        }
    }

    public ResumenCliente? Get(string clienteId)
        => _resumenes.TryGetValue(clienteId, out var r) ? r : null;

    public IReadOnlyCollection<ResumenCliente> ListarTodos()
        => _resumenes.Values.OrderBy(r => r.ClienteId).ToList();

    public int Total => _resumenes.Count;
}
