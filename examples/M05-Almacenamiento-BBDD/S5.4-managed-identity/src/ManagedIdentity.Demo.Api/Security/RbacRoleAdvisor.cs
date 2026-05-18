namespace ManagedIdentity.Demo.Api.Security;

public enum ServicioDestino
{
    CosmosDb, BlobStorage, QueueStorage, TableStorage,
    KeyVault, ServiceBus, EventHub, AzureSql,
}

public enum Acceso { Lectura, LecturaEscritura, Propietario }

// Slides 17, 23, 27 — least privilege: el rol RBAC mínimo por servicio y
// nivel de acceso, y el sufijo de App Setting que activa MI. Nunca roles
// de plano de control amplios (Owner/Contributor) — anti-pattern slide 27.
public static class RbacRoleAdvisor
{
    public static string Recomendar(ServicioDestino servicio, Acceso acceso) => servicio switch
    {
        ServicioDestino.BlobStorage => acceso switch
        {
            Acceso.Lectura => "Storage Blob Data Reader",
            Acceso.LecturaEscritura => "Storage Blob Data Contributor",
            _ => "Storage Blob Data Owner",
        },
        ServicioDestino.QueueStorage => acceso == Acceso.Lectura
            ? "Storage Queue Data Reader"
            : "Storage Queue Data Contributor",
        ServicioDestino.TableStorage => acceso == Acceso.Lectura
            ? "Storage Table Data Reader"
            : "Storage Table Data Contributor",
        ServicioDestino.CosmosDb => acceso == Acceso.Lectura
            ? "Cosmos DB Built-in Data Reader"
            : "Cosmos DB Built-in Data Contributor",
        ServicioDestino.KeyVault => acceso == Acceso.Propietario
            ? "Key Vault Secrets Officer"
            : "Key Vault Secrets User",
        ServicioDestino.ServiceBus => acceso switch
        {
            Acceso.Lectura => "Azure Service Bus Data Receiver",
            Acceso.LecturaEscritura => "Azure Service Bus Data Sender",
            _ => "Azure Service Bus Data Owner",
        },
        ServicioDestino.EventHub => acceso switch
        {
            Acceso.Lectura => "Azure Event Hubs Data Receiver",
            Acceso.LecturaEscritura => "Azure Event Hubs Data Sender",
            _ => "Azure Event Hubs Data Owner",
        },
        // Azure SQL no usa rol RBAC de Azure: se crea un usuario EXTERNAL
        // PROVIDER y se le dan db_datareader/db_datawriter (slide 14).
        ServicioDestino.AzureSql => acceso == Acceso.Lectura
            ? "db_datareader"
            : "db_datareader + db_datawriter",
        _ => throw new ArgumentOutOfRangeException(nameof(servicio)),
    };

    // Slide 17 — sufijo del App Setting que le dice a la app/Functions
    // "usa MI, no connection string".
    public static string SufijoAppSetting(ServicioDestino servicio) => servicio switch
    {
        ServicioDestino.CosmosDb => "__accountEndpoint",
        ServicioDestino.BlobStorage => "__blobServiceUri",
        ServicioDestino.QueueStorage => "__queueServiceUri",
        ServicioDestino.TableStorage => "__tableServiceUri",
        ServicioDestino.ServiceBus or ServicioDestino.EventHub => "__fullyQualifiedNamespace",
        ServicioDestino.KeyVault => "__vaultUri",
        ServicioDestino.AzureSql => "Authentication=Active Directory Default",
        _ => throw new ArgumentOutOfRangeException(nameof(servicio)),
    };

    // Slide 27 (anti-pattern 3) — Owner/Contributor de plano de control
    // para una MI es excesivo: marca el rol como peligroso.
    public static bool EsRolPeligroso(string rol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rol);
        var r = rol.Trim();
        return r.Equals("Owner", StringComparison.OrdinalIgnoreCase)
            || r.Equals("Contributor", StringComparison.OrdinalIgnoreCase)
            || r.Equals("User Access Administrator", StringComparison.OrdinalIgnoreCase);
    }
}
