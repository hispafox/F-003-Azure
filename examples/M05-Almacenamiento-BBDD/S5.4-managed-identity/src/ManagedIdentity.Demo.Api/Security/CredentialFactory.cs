using Azure.Core;
using Azure.Identity;

namespace ManagedIdentity.Demo.Api.Security;

// Slides 6-7, 16, 22, 25 — DefaultAzureCredential es el pegamento que
// hace que el MISMO código funcione en local (az login) y en Azure (MI).
// La construcción de las opciones a partir de config es lógica pura y
// testeable; la credencial en sí se inyecta como singleton (slide 21).
public static class CredentialFactory
{
    // Slide 22 — User-Assigned MI: clientId concreto.
    public const string KeyUserAssignedClientId = "Azure:UserAssignedClientId";
    // Slide 25 — cross-tenant: tenant del recurso destino.
    public const string KeyTenantId = "Azure:TenantId";
    // Slide 16 — en local, saltar el intento de MI (IMDS timeout) acelera.
    public const string KeyLocalDev = "Azure:LocalDev";

    public static DefaultAzureCredentialOptions CrearOpciones(IConfiguration cfg)
    {
        var opciones = new DefaultAzureCredentialOptions();

        var uami = cfg[KeyUserAssignedClientId];
        if (!string.IsNullOrWhiteSpace(uami))
            opciones.ManagedIdentityClientId = uami;

        var tenant = cfg[KeyTenantId];
        if (!string.IsNullOrWhiteSpace(tenant))
            opciones.TenantId = tenant;

        // Slide 16 — solo en desarrollo local: evita el timeout de IMDS.
        if (cfg.GetValue<bool>(KeyLocalDev))
            opciones.ExcludeManagedIdentityCredential = true;

        return opciones;
    }

    public static TokenCredential Crear(IConfiguration cfg)
        => new DefaultAzureCredential(CrearOpciones(cfg));
}
