using Azure.Core;
using Azure.Identity;

namespace Cosmos.Mi.Demo.Api.Security;

// Reutiliza el patrón de S5.4 — DefaultAzureCredential desde config:
// mismo código en local (az login) y en Azure (Managed Identity).
public static class CredentialFactory
{
    public const string KeyUserAssignedClientId = "Azure:UserAssignedClientId"; // slide MI
    public const string KeyTenantId = "Azure:TenantId";
    public const string KeyLocalDev = "Azure:LocalDev"; // acelera local (IMDS timeout)

    public static DefaultAzureCredentialOptions CrearOpciones(IConfiguration cfg)
    {
        var o = new DefaultAzureCredentialOptions();

        var uami = cfg[KeyUserAssignedClientId];
        if (!string.IsNullOrWhiteSpace(uami))
            o.ManagedIdentityClientId = uami;

        var tenant = cfg[KeyTenantId];
        if (!string.IsNullOrWhiteSpace(tenant))
            o.TenantId = tenant;

        if (cfg.GetValue<bool>(KeyLocalDev))
            o.ExcludeManagedIdentityCredential = true;

        return o;
    }

    public static TokenCredential Crear(IConfiguration cfg)
        => new DefaultAzureCredential(CrearOpciones(cfg));
}
