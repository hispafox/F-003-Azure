namespace Entra.Demo.Api.Entra;

public enum SistemaDeRoles { AzureRbac, EntraId, Desconocido }

// Slides 6-7 — RBAC de Azure (roles sobre recursos) vs roles de Entra ID
// (roles sobre el directorio) son DOS sistemas distintos. Clasifica un
// nombre de rol en uno u otro. Lógica pura.
public static class RoleClassifier
{
    private static readonly HashSet<string> AzureRbac =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Owner", "Contributor", "Reader",
            "User Access Administrator",
            "Storage Blob Data Contributor", "Cosmos DB Built-in Data Contributor",
        };

    private static readonly HashSet<string> EntraId =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Global Administrator", "User Administrator",
            "Application Administrator", "Cloud Application Administrator",
            "Security Administrator", "Privileged Role Administrator",
            "Billing Administrator",
        };

    public static SistemaDeRoles Clasificar(string rol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rol);
        var r = rol.Trim();
        if (AzureRbac.Contains(r)) return SistemaDeRoles.AzureRbac;
        if (EntraId.Contains(r)) return SistemaDeRoles.EntraId;
        return SistemaDeRoles.Desconocido;
    }

    // Slide 6 — un rol RBAC de Azure NO se gestiona donde un rol de Entra
    // (IAM del recurso vs Entra ID → Roles and administrators).
    public static string DondeSeAsigna(SistemaDeRoles s) => s switch
    {
        SistemaDeRoles.AzureRbac => "Portal → Recurso → Access Control (IAM)",
        SistemaDeRoles.EntraId => "Portal → Entra ID → Roles and administrators",
        _ => "Desconocido",
    };
}
