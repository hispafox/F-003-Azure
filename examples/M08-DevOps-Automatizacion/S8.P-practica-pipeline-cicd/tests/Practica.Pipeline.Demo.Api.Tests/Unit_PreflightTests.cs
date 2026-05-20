using Practica.Pipeline.Demo.Api.Pipeline;

namespace Practica.Pipeline.Demo.Api.Tests;

// CAPA 1 — preflight de la práctica (slide 3).
[Trait("Category", "Unit")]
public class Unit_PreflightTests
{
    private static EscenarioPreflight TodoOk() => new(
        TieneOrgADO: true,
        TieneRepoConPushAccess: true,
        TieneSuscripcionAzure: true,
        EsAdminProyectoADO: true,
        EsOwnerOUserAccessAdmin: true,
        PlanS1OSuperior: true,
        SlotStagingExiste: true,
        TieneServiceConnectionOidc: true,
        TieneAppRegistration: true,
        TieneAzCliInstalado: true);

    [Fact]
    public void Todo_Ok_Es_Listo_Y_Sin_Bloqueantes()
    {
        var r = PreflightChecker.Comprobar(TodoOk());
        Assert.True(r.ListoParaArrancar);
        Assert.DoesNotContain(r.Hallazgos, h => h.Nivel == HallazgoNivel.Bloqueante);
    }

    [Fact]
    public void Sin_Slot_Staging_Es_Bloqueante()
    {
        var e = TodoOk() with { SlotStagingExiste = false };
        var r = PreflightChecker.Comprobar(e);
        Assert.False(r.ListoParaArrancar);
        Assert.Contains(r.Hallazgos, h =>
            h.Comprobacion.Contains("staging", StringComparison.OrdinalIgnoreCase)
            && h.Nivel == HallazgoNivel.Bloqueante);
    }

    [Fact]
    public void Plan_Free_Bloquea_Practica()
    {
        var e = TodoOk() with { PlanS1OSuperior = false };
        var r = PreflightChecker.Comprobar(e);
        Assert.False(r.ListoParaArrancar);
        Assert.Contains(r.Hallazgos, h =>
            h.Comprobacion.Contains("Plan", StringComparison.OrdinalIgnoreCase)
            && h.Nivel == HallazgoNivel.Bloqueante);
    }

    [Fact]
    public void Sin_Service_Connection_Oidc_Es_Aviso_No_Bloqueante()
    {
        var e = TodoOk() with { TieneServiceConnectionOidc = false };
        var r = PreflightChecker.Comprobar(e);
        Assert.True(r.ListoParaArrancar);
        Assert.Contains(r.Hallazgos, h =>
            h.Comprobacion.Contains("Workload Identity", StringComparison.OrdinalIgnoreCase)
            && h.Nivel == HallazgoNivel.Aviso);
    }

    [Fact]
    public void Sin_Az_Cli_Es_Aviso()
    {
        var e = TodoOk() with { TieneAzCliInstalado = false };
        var r = PreflightChecker.Comprobar(e);
        Assert.Contains(r.Hallazgos, h =>
            h.Comprobacion.Contains("Azure CLI", StringComparison.OrdinalIgnoreCase)
            && h.Nivel == HallazgoNivel.Aviso);
    }

    [Fact]
    public void Sin_Org_Sin_Push_Sin_Sub_Tres_Bloqueantes()
    {
        var e = TodoOk() with
        {
            TieneOrgADO = false,
            TieneRepoConPushAccess = false,
            TieneSuscripcionAzure = false,
        };
        var r = PreflightChecker.Comprobar(e);
        Assert.False(r.ListoParaArrancar);
        Assert.Equal(3, r.Hallazgos.Count(h =>
            h.Nivel == HallazgoNivel.Bloqueante &&
            (h.Comprobacion.Contains("Organization", StringComparison.Ordinal)
             || h.Comprobacion.Contains("push", StringComparison.OrdinalIgnoreCase)
             || h.Comprobacion.Contains("Suscripción", StringComparison.Ordinal))));
    }
}
