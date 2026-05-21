namespace Bonus.IntroIaAgentica.Demo.Api.Intro;

public enum Herramienta { ClaudeCode, Cowork, Ambas }

public sealed record FilaComparativa(
    string Criterio, string ClaudeCode, string Cowork);

public sealed record RecomendacionHerramienta(
    Herramienta Cual, IReadOnlyList<string> Razones);

public sealed record EscenarioUso(
    bool TrabajaEnTerminal = false,
    bool EditaCodigo = false,
    bool EjecutaAzCli = false,
    bool EjecutaBicepDeploy = false,
    bool GeneraInformes = false,
    bool OrganizaArchivos = false,
    bool NecesitaScheduledTasks = false,
    bool QuiereProyectosPersistentes = false,
    bool EsDeveloper = false,
    bool EsKnowledgeWorker = false);

// Slide 9 — comparador Claude Code vs Cowork. Lógica pura. La tabla
// canónica del slide 9 + heurística por escenario. La regla típica:
// devs en terminal → Claude Code; PMs/ops/managers → Cowork; equipos
// mixtos → ambas.
public static class CcVsCoworkRecommender
{
    public static IReadOnlyList<FilaComparativa> Tabla { get; } =
    [
        new("Interfaz", "Terminal (CLI)", "App de escritorio (GUI)"),
        new("Público", "Desarrolladores", "Knowledge workers"),
        new("Skills", "Sí", "Sí"),
        new("MCP", "Sí", "Sí"),
        new("Edita código", "Sí (principal)", "Menos habitual"),
        new("Ejecuta az CLI", "Sí", "Con connector"),
        new("Ejecuta Bicep deploy", "Sí", "Con connector"),
        new("Genera informes", "OK", "Excelente"),
        new("Organiza archivos", "OK", "Excelente"),
        new("Scheduled tasks", "—", "Sí"),
        new("Proyectos persistentes", "—", "Sí"),
        new("Multi-agente paralelo", "Con plugins", "Sí nativo"),
    ];

    public static RecomendacionHerramienta Recomendar(EscenarioUso e)
    {
        ArgumentNullException.ThrowIfNull(e);

        bool senalesCc = e.TrabajaEnTerminal || e.EditaCodigo
            || e.EjecutaAzCli || e.EjecutaBicepDeploy || e.EsDeveloper;
        bool senalesCowork = e.GeneraInformes || e.OrganizaArchivos
            || e.NecesitaScheduledTasks || e.QuiereProyectosPersistentes
            || e.EsKnowledgeWorker;

        if (senalesCc && senalesCowork)
            return new RecomendacionHerramienta(
                Herramienta.Ambas,
                [
                    "Equipo mixto (devs + knowledge workers) → ambas herramientas " +
                        "no se solapan (slide 9).",
                    "Claude Code para los devs sobre código e infra.",
                    "Cowork para PMs/managers/ops que operan Azure sin entrar al código.",
                ]);

        if (senalesCc)
            return new RecomendacionHerramienta(
                Herramienta.ClaudeCode,
                [
                    "Trabajo principal en terminal + código + Bicep → Claude Code (slide 9).",
                    "MCP de ADO/GitHub conecta con el flujo de trabajo del developer.",
                ]);

        if (senalesCowork)
            return new RecomendacionHerramienta(
                Herramienta.Cowork,
                [
                    "Trabajo no-code (informes, costes, exports, scheduled tasks) → Cowork (slide 9).",
                    "GUI de escritorio + proyectos persistentes + multi-agente nativo.",
                ]);

        // Sin señales → por defecto Claude Code (es el más relevante para
        // un curso AZ-204).
        return new RecomendacionHerramienta(
            Herramienta.ClaudeCode,
            [
                "Sin señales claras de tu rol — para el ámbito AZ-204 / DevOps Claude Code " +
                    "es el punto de entrada por defecto (slide 9).",
                "Si tu rol es PM/manager/ops, valora Cowork.",
            ]);
    }
}
