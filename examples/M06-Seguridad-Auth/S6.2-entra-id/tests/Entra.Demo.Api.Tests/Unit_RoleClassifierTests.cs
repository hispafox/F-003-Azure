using Entra.Demo.Api.Entra;

namespace Entra.Demo.Api.Tests;

// CAPA 1 — RBAC de Azure vs roles de Entra ID (slides 6-7).
[Trait("Category", "Unit")]
public class Unit_RoleClassifierTests
{
    [Theory]
    [InlineData("Owner", SistemaDeRoles.AzureRbac)]
    [InlineData("contributor", SistemaDeRoles.AzureRbac)]   // case-insensitive
    [InlineData("Storage Blob Data Contributor", SistemaDeRoles.AzureRbac)]
    [InlineData("Global Administrator", SistemaDeRoles.EntraId)]
    [InlineData("Application Administrator", SistemaDeRoles.EntraId)]
    [InlineData("Rol Inventado", SistemaDeRoles.Desconocido)]
    public void Clasificar(string rol, SistemaDeRoles esperado)
        => Assert.Equal(esperado, RoleClassifier.Clasificar(rol));

    [Fact]
    public void DondeSeAsigna_Distingue_IAM_De_EntraId()
    {
        Assert.Contains("Access Control (IAM)",
            RoleClassifier.DondeSeAsigna(SistemaDeRoles.AzureRbac));
        Assert.Contains("Entra ID",
            RoleClassifier.DondeSeAsigna(SistemaDeRoles.EntraId));
    }

    [Fact]
    public void Clasificar_Vacio_Lanza()
        => Assert.Throws<ArgumentException>(() => RoleClassifier.Clasificar("  "));
}
