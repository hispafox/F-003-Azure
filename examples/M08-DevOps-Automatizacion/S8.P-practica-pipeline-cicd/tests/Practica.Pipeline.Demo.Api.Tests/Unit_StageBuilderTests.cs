using Practica.Pipeline.Demo.Api.Pipeline;

namespace Practica.Pipeline.Demo.Api.Tests;

// CAPA 1 — esqueleto del pipeline (slides 4-6, 10, 17, 18).
[Trait("Category", "Unit")]
public class Unit_StageBuilderTests
{
    [Fact]
    public void Ado_Por_Defecto_Tiene_Build_DeployStaging_SwapProduction()
    {
        var p = PipelineStageBuilder.Construir(new OpcionesPipeline());
        var nombres = p.Etapas.Select(e => e.Nombre).ToArray();
        Assert.Contains("Build", nombres);
        Assert.Contains("DeployStaging", nombres);
        Assert.Contains("SwapProduction", nombres);
    }

    [Fact]
    public void Build_Incluye_Restore_Build_Test_Y_Publish()
    {
        var p = PipelineStageBuilder.Construir(new OpcionesPipeline());
        var build = p.Etapas.Single(e => e.Nombre == "Build");
        Assert.Contains(build.Pasos, s => s.Contains("dotnet restore", StringComparison.Ordinal));
        Assert.Contains(build.Pasos, s => s.Contains("dotnet build", StringComparison.Ordinal));
        Assert.Contains(build.Pasos, s => s.Contains("dotnet test", StringComparison.Ordinal));
        Assert.Contains(build.Pasos, s => s.Contains("dotnet publish", StringComparison.Ordinal));
    }

    [Fact]
    public void Swap_Production_Pide_Aprobacion_Por_Defecto()
    {
        var p = PipelineStageBuilder.Construir(new OpcionesPipeline());
        var swap = p.Etapas.Single(e => e.Nombre == "SwapProduction");
        Assert.True(swap.RequiereAprobacion);
    }

    [Fact]
    public void Sin_Aprobacion_En_Produccion_No_Pide_Approval()
    {
        var p = PipelineStageBuilder.Construir(new OpcionesPipeline(
            AprobacionEnProduccion: false));
        var swap = p.Etapas.Single(e => e.Nombre == "SwapProduction");
        Assert.False(swap.RequiereAprobacion);
    }

    [Fact]
    public void Auto_Rollback_Anade_Paso_De_Swap_Inverso()
    {
        var p = PipelineStageBuilder.Construir(new OpcionesPipeline(
            AutoRollbackEnFallo: true));
        var swap = p.Etapas.Single(e => e.Nombre == "SwapProduction");
        Assert.Contains(swap.Pasos, s => s.Contains("rollback", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Sin_Auto_Rollback_No_Hay_Paso_De_Rollback()
    {
        var p = PipelineStageBuilder.Construir(new OpcionesPipeline(
            AutoRollbackEnFallo: false));
        var swap = p.Etapas.Single(e => e.Nombre == "SwapProduction");
        Assert.DoesNotContain(swap.Pasos, s => s.Contains("rollback", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Escanear_Vulnerables_Inserta_Stage_Security_Antes_De_Deploy()
    {
        var p = PipelineStageBuilder.Construir(new OpcionesPipeline(
            EscanearVulnerables: true));
        int idxSecurity = p.Etapas.Select((e, i) => (e, i))
            .First(t => t.e.Nombre == "SecurityScan").i;
        int idxDeploy = p.Etapas.Select((e, i) => (e, i))
            .First(t => t.e.Nombre == "DeployStaging").i;
        Assert.True(idxSecurity < idxDeploy);
    }

    [Fact]
    public void Notificar_Teams_Anade_Stage_NotifyOnFailure()
    {
        var p = PipelineStageBuilder.Construir(new OpcionesPipeline(
            NotificarTeamsEnFallo: true));
        Assert.Contains(p.Etapas, e => e.Nombre == "NotifyOnFailure");
    }

    [Fact]
    public void GitHub_Actions_Usa_Tareas_Actions_En_Build()
    {
        var p = PipelineStageBuilder.Construir(new OpcionesPipeline(
            Plataforma: Plataforma.GitHubActions));
        Assert.Equal(Plataforma.GitHubActions, p.Plataforma);
        var build = p.Etapas.Single(e => e.Nombre == "Build");
        Assert.Contains(build.Pasos, s => s.Contains("actions/setup-dotnet", StringComparison.Ordinal));
        Assert.Contains(build.Pasos, s => s.Contains("actions/upload-artifact", StringComparison.Ordinal));
    }

    [Fact]
    public void Ado_Usa_AzureWebApp_Task()
    {
        var p = PipelineStageBuilder.Construir(new OpcionesPipeline(
            Plataforma: Plataforma.AzureDevOps));
        var deploy = p.Etapas.Single(e => e.Nombre == "DeployStaging");
        Assert.Contains(deploy.Pasos, s => s.Contains("AzureWebApp@1", StringComparison.Ordinal));
    }

    [Fact]
    public void Oidc_True_Menciona_Workload_Identity_Federation()
    {
        var p = PipelineStageBuilder.Construir(new OpcionesPipeline(
            UsarOidc: true));
        var deploy = p.Etapas.Single(e => e.Nombre == "DeployStaging");
        Assert.Contains(deploy.Pasos, s => s.Contains("Workload Identity", StringComparison.Ordinal));
    }

    [Fact]
    public void Sin_Oidc_Menciona_Service_Principal_Con_Secret()
    {
        var p = PipelineStageBuilder.Construir(new OpcionesPipeline(
            UsarOidc: false));
        var deploy = p.Etapas.Single(e => e.Nombre == "DeployStaging");
        Assert.Contains(deploy.Pasos, s => s.Contains("Service Principal", StringComparison.Ordinal));
    }
}
