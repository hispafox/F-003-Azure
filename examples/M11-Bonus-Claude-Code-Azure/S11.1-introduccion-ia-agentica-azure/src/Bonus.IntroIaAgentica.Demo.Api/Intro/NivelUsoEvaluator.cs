namespace Bonus.IntroIaAgentica.Demo.Api.Intro;

public enum NivelUso
{
    Nivel1_Ayudante,         // slide 10
    Nivel2_Colega,
    Nivel3_AgenteAutonomo,
}

public sealed record EvaluacionNivel(
    NivelUso Nivel,
    string Descripcion,
    int PrincipiosCumplidos,        // de los 4 del slide 18
    IReadOnlyList<string> ProximosPasos);

public sealed record EscenarioEquipo(
    bool UsaPromptsConcretos = false,
    bool ConfiguraSkills = false,
    bool ConfiguraMcp = false,
    bool TieneAgentsPropios = false,
    bool EjecutaWorkflowsAutomaticos = false,
    // 4 principios del slide 18.
    bool SkillsEnGit = false,
    bool AgentsConPermisosMinimos = false,
    bool HumanoEnLoopAccionesConImpacto = false,
    bool AuditaElUsoDeIa = false);

// Slide 10/18 — evalúa el nivel de madurez del equipo en uso de IA
// agéntica. Nivel 1 (ayudante) si solo pide cosas concretas; Nivel 2
// (colega) si configura skills + MCP; Nivel 3 (agente autónomo) si
// tiene agents propios y workflows automáticos. Lógica pura. Reporta
// también cuántos de los 4 principios del slide 18 cumple.
public static class NivelUsoEvaluator
{
    public static EvaluacionNivel Evaluar(EscenarioEquipo e)
    {
        ArgumentNullException.ThrowIfNull(e);

        int principios = 0;
        if (e.SkillsEnGit) principios++;
        if (e.AgentsConPermisosMinimos) principios++;
        if (e.HumanoEnLoopAccionesConImpacto) principios++;
        if (e.AuditaElUsoDeIa) principios++;

        NivelUso nivel;
        string descripcion;
        if (e.TieneAgentsPropios && e.EjecutaWorkflowsAutomaticos)
        {
            nivel = NivelUso.Nivel3_AgenteAutonomo;
            descripcion = "Workflows definidos (agents, plugins, scheduled tasks) que " +
                "se ejecutan sin supervisión continua (slide 10).";
        }
        else if (e.ConfiguraSkills && e.ConfiguraMcp)
        {
            nivel = NivelUso.Nivel2_Colega;
            descripcion = "Skills y MCP configurados; Claude Code actúa con contexto y trabaja " +
                "en múltiples archivos iterando contigo (slide 10).";
        }
        else
        {
            nivel = NivelUso.Nivel1_Ayudante;
            descripcion = "Le pides cosas concretas y revisas/aplicas tú. Sois vosotros el " +
                "conductor (slide 10).";
        }

        var proximos = new List<string>();
        if (nivel == NivelUso.Nivel1_Ayudante)
        {
            if (!e.ConfiguraSkills)
                proximos.Add("Configura skills del equipo en `.claude/skills/` (slide 10/18 #1).");
            if (!e.ConfiguraMcp)
                proximos.Add("Habilita MCP servers (filesystem + ADO/GitHub) para subir a Nivel 2 (slide 10).");
        }
        if (nivel == NivelUso.Nivel2_Colega)
        {
            if (!e.TieneAgentsPropios)
                proximos.Add("Crea agents propios (`.claude/agents/`) para tareas paralelas (slide 10).");
            if (!e.EjecutaWorkflowsAutomaticos)
                proximos.Add("Define workflows / plugins para llegar al Nivel 3 (slide 10).");
        }

        // Recordatorio de principios slide 18 que falten.
        if (!e.SkillsEnGit)
            proximos.Add("Versiona skills en Git como código (principio #1 slide 18).");
        if (!e.AgentsConPermisosMinimos)
            proximos.Add("Configura agents con permisos mínimos (principio #2 slide 18).");
        if (!e.HumanoEnLoopAccionesConImpacto)
            proximos.Add("Humano en el loop para acciones con impacto: deploy a prod, " +
                "DROP TABLE, etc. (principio #3 slide 18).");
        if (!e.AuditaElUsoDeIa)
            proximos.Add("Audita el uso: logs de comandos + archivos modificados + coste de API " +
                "(principio #4 slide 18).");

        return new EvaluacionNivel(nivel, descripcion, principios, proximos);
    }
}
