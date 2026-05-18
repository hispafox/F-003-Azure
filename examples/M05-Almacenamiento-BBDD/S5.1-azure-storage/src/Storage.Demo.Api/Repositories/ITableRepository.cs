using Azure.Data.Tables;
using Storage.Demo.Api.Models;

namespace Storage.Demo.Api.Repositories;

// Slides 11-12 — Table Storage: clave compuesta PartitionKey+RowKey,
// query rápida por PartitionKey, batch por partición.
public interface ITableRepository
{
    Task<AuditEntity> InsertarAsync(CrearAuditDto dto);
    Task<AuditEntity?> ObtenerAsync(string particion, string rowKey);
    Task<IReadOnlyList<AuditEntity>> PorParticionAsync(string particion);
}

public sealed class TableRepository : ITableRepository
{
    private readonly TableClient _table;

    public TableRepository(TableServiceClient service)
    {
        _table = service.GetTableClient("AuditLog");
        _table.CreateIfNotExists();
    }

    public async Task<AuditEntity> InsertarAsync(CrearAuditDto dto)
    {
        var entity = new AuditEntity
        {
            PartitionKey = dto.Particion,
            RowKey = Guid.NewGuid().ToString("N"),
            Usuario = dto.Usuario,
            Accion = dto.Accion,
            Detalle = dto.Detalle,
        };
        await _table.AddEntityAsync(entity);
        return entity;
    }

    public async Task<AuditEntity?> ObtenerAsync(string particion, string rowKey)
    {
        try
        {
            var r = await _table.GetEntityAsync<AuditEntity>(particion, rowKey);
            return r.Value;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<AuditEntity>> PorParticionAsync(string particion)
    {
        var items = new List<AuditEntity>();
        // Query por PartitionKey = rápida (datos co-localizados, slide 11).
        await foreach (var e in _table.QueryAsync<AuditEntity>(
            x => x.PartitionKey == particion))
        {
            items.Add(e);
        }
        return items;
    }
}
