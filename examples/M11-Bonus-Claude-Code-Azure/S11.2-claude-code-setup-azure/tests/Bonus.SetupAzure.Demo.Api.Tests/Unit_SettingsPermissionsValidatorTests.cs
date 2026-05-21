using Bonus.SetupAzure.Demo.Api.Setup;

namespace Bonus.SetupAzure.Demo.Api.Tests;

// CAPA 1 — slide 7/9: validador de permissions.
[Trait("Category", "Unit")]
public class Unit_SettingsPermissionsValidatorTests
{
    private static EscenarioSettings Bien() => new(
        Allow: ["Bash(dotnet *)", "Bash(az *)", "Read(**)"],
        Deny:
        [
            "Bash(rm -rf *)",
            "Bash(az group delete *)",
            "Bash(az resource delete *)",
            "Bash(drop database *)",
            "Read(**/*.env)",
            "Read(**/*.pfx)",
            "Read(**/*.key)",
            "Read(**/local.settings.json)",
        ],
        Model: "claude-sonnet-4-6");

    [Fact]
    public void Settings_Bien_Configurados_Es_Seguro()
    {
        var r = SettingsPermissionsValidator.Validar(Bien());

        Assert.True(r.Seguro);
        Assert.DoesNotContain(r.Hallazgos, h =>
            h.Nivel is NivelRiesgoSettings.Critico or NivelRiesgoSettings.Alto);
    }

    [Fact]
    public void Bash_Asterisco_Es_Critico()
    {
        var s = Bien() with { Allow = ["Bash(*)"] };
        var r = SettingsPermissionsValidator.Validar(s);

        Assert.False(r.Seguro);
        Assert.Contains(r.Hallazgos, h =>
            h.Nivel == NivelRiesgoSettings.Critico
            && h.Mensaje.Contains("comando shell", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Write_Doble_Asterisco_Es_Critico()
    {
        var s = Bien() with { Allow = ["Write(**)"] };
        var r = SettingsPermissionsValidator.Validar(s);

        Assert.False(r.Seguro);
        Assert.Contains(r.Hallazgos, h =>
            h.Nivel == NivelRiesgoSettings.Critico
            && h.Comprobacion.Contains("Write(**)", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("rm -rf")]
    [InlineData("az group delete")]
    [InlineData("az resource delete")]
    [InlineData("drop database")]
    public void Falta_Deny_De_Comando_Destructivo_Es_Alto(string cmd)
    {
        var s = Bien() with { Deny = [.. Bien().Deny.Where(d => !d.Contains(cmd))] };
        var r = SettingsPermissionsValidator.Validar(s);

        Assert.False(r.Seguro);
        Assert.Contains(r.Hallazgos, h =>
            h.Nivel == NivelRiesgoSettings.Alto
            && h.Comprobacion.Contains(cmd, StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("*.env")]
    [InlineData("*.pfx")]
    [InlineData("*.key")]
    [InlineData("local.settings.json")]
    public void Falta_Exclude_De_Archivo_Sensible_Es_Alto(string patron)
    {
        var s = Bien() with { Deny = [.. Bien().Deny.Where(d => !d.Contains(patron))] };
        var r = SettingsPermissionsValidator.Validar(s);

        Assert.False(r.Seguro);
        Assert.Contains(r.Hallazgos, h =>
            h.Nivel == NivelRiesgoSettings.Alto
            && h.Comprobacion.Contains(patron, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Sin_Modelo_Es_Medio_Pero_Sigue_Seguro()
    {
        var s = Bien() with { Model = null };
        var r = SettingsPermissionsValidator.Validar(s);

        Assert.True(r.Seguro);
        Assert.Contains(r.Hallazgos, h =>
            h.Nivel == NivelRiesgoSettings.Medio
            && h.Comprobacion.Contains("model", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Allow_Vacio_Y_Deny_Vacio_Genera_Varios_Altos()
    {
        var r = SettingsPermissionsValidator.Validar(new EscenarioSettings(
            Allow: [], Deny: [], Model: "claude-sonnet-4-6"));

        Assert.False(r.Seguro);
        Assert.True(r.Hallazgos.Count(h => h.Nivel == NivelRiesgoSettings.Alto) >= 8);
    }

    [Fact]
    public void Validar_Con_Null_Lanza()
    {
        Assert.Throws<ArgumentNullException>(() =>
            SettingsPermissionsValidator.Validar(null!));
    }
}
