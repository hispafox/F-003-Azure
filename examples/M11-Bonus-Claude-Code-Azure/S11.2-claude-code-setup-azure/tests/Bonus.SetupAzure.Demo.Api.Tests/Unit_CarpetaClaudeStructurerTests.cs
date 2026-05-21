using Bonus.SetupAzure.Demo.Api.Setup;

namespace Bonus.SetupAzure.Demo.Api.Tests;

// CAPA 1 — slide 4: estructura del directorio `.claude/`.
[Trait("Category", "Unit")]
public class Unit_CarpetaClaudeStructurerTests
{
    [Fact]
    public void Estructura_Minima_Incluye_ClaudeMd_Y_Settings_Como_Obligatorios()
    {
        var r = CarpetaClaudeStructurer.Inventariar(new EscenarioEquipo());

        Assert.Contains(r.Items, i =>
            i.Ruta == "CLAUDE.md" && i.Prioridad == Prioridad.Obligatorio);
        Assert.Contains(r.Items, i =>
            i.Ruta == ".claude/settings.json" && i.Prioridad == Prioridad.Obligatorio);
    }

    [Fact]
    public void Settings_Local_Es_Recomendado_Siempre()
    {
        var r = CarpetaClaudeStructurer.Inventariar(new EscenarioEquipo());

        Assert.Contains(r.Items, i =>
            i.Ruta == ".claude/settings.local.json"
            && i.Prioridad == Prioridad.Recomendado);
    }

    [Fact]
    public void Equipo_Con_Agents_Skills_Mcp_Anade_Esos_Items()
    {
        var r = CarpetaClaudeStructurer.Inventariar(new EscenarioEquipo(
            TieneAgentsCustom: true,
            TieneSkillsPropios: true,
            UsaMcpServers: true));

        Assert.Contains(r.Items, i => i.Ruta == ".claude/agents/");
        Assert.Contains(r.Items, i => i.Ruta == ".claude/skills/");
        Assert.Contains(r.Items, i => i.Ruta == ".mcp.json");
    }

    [Fact]
    public void Equipo_Sin_Hooks_Avisa_De_Defensa_Determinista()
    {
        var r = CarpetaClaudeStructurer.Inventariar(new EscenarioEquipo(
            QuiereHooks: false));

        Assert.Contains(r.Avisos, a => a.Contains("hooks", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Equipo_Con_Hooks_No_Genera_Aviso_De_Hooks()
    {
        var r = CarpetaClaudeStructurer.Inventariar(new EscenarioEquipo(
            QuiereHooks: true));

        Assert.Contains(r.Items, i => i.Ruta == ".claude/hooks/");
        Assert.DoesNotContain(r.Avisos, a =>
            a.Contains("Sin hooks", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SlashCommands_Custom_Marca_Commands_Como_Opcional()
    {
        var r = CarpetaClaudeStructurer.Inventariar(new EscenarioEquipo(
            TieneSlashCommandsCustom: true));

        var commands = r.Items.SingleOrDefault(i => i.Ruta == ".claude/commands/");
        Assert.NotNull(commands);
        Assert.Equal(Prioridad.Opcional, commands!.Prioridad);
    }

    [Fact]
    public void Skills_Sin_Mcp_Genera_Aviso_De_Mcp()
    {
        var r = CarpetaClaudeStructurer.Inventariar(new EscenarioEquipo(
            TieneSkillsPropios: true,
            UsaMcpServers: false));

        Assert.Contains(r.Avisos, a =>
            a.Contains("MCP", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TrabajoIndividual_Con_Agents_Sugiere_Global()
    {
        var r = CarpetaClaudeStructurer.Inventariar(new EscenarioEquipo(
            TieneAgentsCustom: true,
            TrabajoIndividual: true));

        Assert.Contains(r.Avisos, a =>
            a.Contains("individual", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Items_Tienen_Texto_ParaQueSirve_No_Vacio()
    {
        var r = CarpetaClaudeStructurer.Inventariar(new EscenarioEquipo(
            TieneAgentsCustom: true,
            TieneSkillsPropios: true,
            TieneSlashCommandsCustom: true,
            QuiereHooks: true,
            UsaMcpServers: true));

        Assert.All(r.Items, i => Assert.False(string.IsNullOrWhiteSpace(i.ParaQueSirve)));
    }

    [Fact]
    public void Inventariar_Con_Null_Lanza()
    {
        Assert.Throws<ArgumentNullException>(() =>
            CarpetaClaudeStructurer.Inventariar(null!));
    }
}
