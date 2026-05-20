using ClaudeCode.Infra.Demo.Api.Infra;

namespace ClaudeCode.Infra.Demo.Api.Tests;

// CAPA 1 — parser de requisitos IaC (slides 2/3/17).
[Trait("Category", "Unit")]
public class Unit_RequirementsParserTests
{
    [Fact]
    public void Detecta_App_Service_Cosmos_Service_Bus_Key_Vault()
    {
        var r = InfraRequirementsParser.Parsear(
            "API REST con App Service, Cosmos DB serverless, Service Bus para colas " +
            "async y Key Vault para secretos.");
        var tipos = r.Recursos.Select(x => x.Tipo).ToHashSet();
        Assert.Contains(TipoRecurso.AppService, tipos);
        Assert.Contains(TipoRecurso.CosmosDb, tipos);
        Assert.Contains(TipoRecurso.ServiceBus, tipos);
        Assert.Contains(TipoRecurso.KeyVault, tipos);
    }

    [Fact]
    public void No_Duplica_Recurso_Aunque_Tenga_Varias_Palabras_Clave()
    {
        var r = InfraRequirementsParser.Parsear(
            "App Service web app con HTTPS para la api rest pública.");
        Assert.Equal(1, r.Recursos.Count(x => x.Tipo == TipoRecurso.AppService));
    }

    [Fact]
    public void Detecta_Multi_Region_Por_Las_Dos_Regiones()
    {
        var r = InfraRequirementsParser.Parsear(
            "Quiero desplegar en West Europe y North Europe con replicación.");
        Assert.True(r.MultiRegion);
    }

    [Fact]
    public void Detecta_Gdpr_Como_ComplianceEuropa()
    {
        var r = InfraRequirementsParser.Parsear(
            "Necesito compliance GDPR para los datos de usuarios.");
        Assert.True(r.ComplianceEuropa);
    }

    [Fact]
    public void Detecta_Slots_Y_Autoscale()
    {
        var r = InfraRequirementsParser.Parsear(
            "Con slot de staging y auto-scale en horas pico.");
        Assert.True(r.ConSlots);
        Assert.True(r.ConAutoscale);
    }

    [Fact]
    public void Sin_Https_Only_Genera_Aviso()
    {
        var r = InfraRequirementsParser.Parsear("Quiero una App Service");
        Assert.False(r.ConHttpsOnly);
        Assert.Contains(r.Avisos, a =>
            a.Contains("HTTPS only", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void App_Service_Sin_Mi_Genera_Aviso()
    {
        var r = InfraRequirementsParser.Parsear("Quiero una App Service para mi API");
        Assert.False(r.ConManagedIdentity);
        Assert.Contains(r.Avisos, a =>
            a.Contains("Managed Identity", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Storage_Sin_Private_Endpoint_Genera_Aviso()
    {
        var r = InfraRequirementsParser.Parsear(
            "Necesito un storage account para archivos.");
        Assert.Contains(r.Avisos, a =>
            a.Contains("Private Endpoint", StringComparison.Ordinal));
    }

    [Fact]
    public void Multi_Region_Mas_Gdpr_Pide_Confirmar_Region_Ue()
    {
        var r = InfraRequirementsParser.Parsear(
            "Multi-region (West Europe y North Europe) con GDPR.");
        Assert.True(r.MultiRegion);
        Assert.True(r.ComplianceEuropa);
        Assert.Contains(r.Avisos, a =>
            a.Contains("UE", StringComparison.Ordinal));
    }

    [Fact]
    public void Descripcion_Vacia_Lanza_ArgumentException()
    {
        Assert.Throws<ArgumentException>(() => InfraRequirementsParser.Parsear(" "));
    }
}
