using Distribution.Demo.Api.Distribution;

namespace Distribution.Demo.Api.Tests;

// CAPA 1 — decisión de migración (slides 12, 18).
[Trait("Category", "Unit")]
public class Unit_MigrationTests
{
    [Fact]
    public void Migrar_Si_Intune_Y_Problemas_De_Update()
    {
        // Slide 18 — drivers reales: Intune planeado + ClickOnce con
        // problemas; la columna "Esperar" pierde porque "funciona bien"
        // ya no se sostiene.
        var d = MigrationDecisionAdvisor.DebeMigrar(
            intunePlaneado: true, dotNet8Planeado: false,
            certAuthenticodeExpira: false, problemasActualizacion: true,
            clickOnceFuncionaBien: false, equipoSinBandwidth: false);
        Assert.True(d.Recomendado);
        Assert.Contains(d.Razones, r => r.Contains("Intune"));
    }

    [Fact]
    public void Migrar_Si_DotNet8_Y_Cert_Authenticode_Caduca()
    {
        var d = MigrationDecisionAdvisor.DebeMigrar(
            false, dotNet8Planeado: true,
            certAuthenticodeExpira: true, false, false, false);
        Assert.True(d.Recomendado);
    }

    [Fact]
    public void Indecision_Cuando_Driver_Aislado_Compite_Con_Funciona_Bien()
    {
        // Solo Intune planeado vs "ClickOnce funciona bien" → empate
        // → no recomendado (slide 18 — la decisión necesita más señales).
        var d = MigrationDecisionAdvisor.DebeMigrar(
            intunePlaneado: true, false, false, false,
            clickOnceFuncionaBien: true, equipoSinBandwidth: false);
        Assert.False(d.Recomendado);
    }

    [Fact]
    public void No_Migrar_Si_Solo_Funciona_Bien_Y_Sin_Bandwidth()
    {
        var d = MigrationDecisionAdvisor.DebeMigrar(
            false, false, false, false,
            clickOnceFuncionaBien: true, equipoSinBandwidth: true);
        Assert.False(d.Recomendado);
        Assert.Contains(d.Razones, r => r.Contains("bandwidth"));
    }

    [Theory]
    [InlineData(true, true, true, EscenarioMigracion.C_AppNuevaDirectaMsix)]
    [InlineData(false, true, true, EscenarioMigracion.B_DotNet8MasMsix)]
    [InlineData(false, true, false, EscenarioMigracion.A_EmpaquetarSinReescribir)]
    [InlineData(false, false, false, EscenarioMigracion.A_EmpaquetarSinReescribir)]
    public void Recomendar_Escenario(bool nueva, bool dnf, bool tiempo,
        EscenarioMigracion esperado)
        => Assert.Equal(esperado,
            MigrationDecisionAdvisor.RecomendarEscenario(nueva, dnf, tiempo));
}
