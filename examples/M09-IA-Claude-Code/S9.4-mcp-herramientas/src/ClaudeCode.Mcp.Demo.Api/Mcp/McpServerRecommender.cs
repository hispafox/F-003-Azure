namespace ClaudeCode.Mcp.Demo.Api.Mcp;

public enum CategoriaMcp { Desarrollo, Productividad, BaseDatos, Cloud, Observabilidad }

public sealed record ServerSugerido(
    string Nombre,
    CategoriaMcp Categoria,
    string Slide,
    string Porque,
    IReadOnlyList<string> PermisosMinimos);

public sealed record EscenarioMcp(
    bool UsaAzureDevOps = false,
    bool UsaGitHub = false,
    bool UsaJiraOLinear = false,
    bool UsaCosmosDb = false,
    bool UsaSqlServer = false,
    bool UsaPostgres = false,
    bool UsaNotionODocs = false,
    bool UsaSlackOTeams = false,
    bool NecesitaBrowserAutomation = false,
    bool NecesitaObservabilidad = false,
    bool EquipoEnM365 = false);

// Slide 4-7, 11, 15 — recomendador de MCP servers según el escenario
// del equipo. Devuelve la lista de servers a habilitar + los permisos
// mínimos (least privilege).
public static class McpServerRecommender
{
    public static IReadOnlyList<ServerSugerido> Recomendar(EscenarioMcp e)
    {
        ArgumentNullException.ThrowIfNull(e);

        var lista = new List<ServerSugerido>();

        // Filesystem siempre — Claude necesita leer/escribir archivos.
        lista.Add(new(
            Nombre: "filesystem",
            Categoria: CategoriaMcp.Desarrollo,
            Slide: "3",
            Porque: "Necesario para que Claude lea y edite el código.",
            PermisosMinimos: ["leer y escribir SOLO en el path del proyecto"]));

        if (e.UsaAzureDevOps)
            lista.Add(new(
                Nombre: "azure-devops",
                Categoria: CategoriaMcp.Desarrollo,
                Slide: "4",
                Porque: "Work items + PRs + builds desde el terminal (slide 4).",
                PermisosMinimos: [
                    "Work Items: Read",
                    "Code: Read",
                    "Build: Read",
                    "Code: Write SOLO si Claude crea PRs",
                ]));

        if (e.UsaGitHub)
            lista.Add(new(
                Nombre: "github",
                Categoria: CategoriaMcp.Desarrollo,
                Slide: "5",
                Porque: "Issues, PRs y repos desde el terminal (slide 5).",
                PermisosMinimos: [
                    "repo (read)",
                    "pull_requests (write SOLO si Claude crea PRs)",
                ]));

        if (e.UsaCosmosDb)
            lista.Add(new(
                Nombre: "azure-cosmos",
                Categoria: CategoriaMcp.BaseDatos,
                Slide: "6/11",
                Porque: "Queries Cosmos DB para diagnóstico y análisis (slide 6).",
                PermisosMinimos: ["Cosmos DB Built-in Data Reader (read-only)"]));

        if (e.UsaSqlServer)
            lista.Add(new(
                Nombre: "sql-server",
                Categoria: CategoriaMcp.BaseDatos,
                Slide: "11",
                Porque: "Queries SQL desde el terminal (slide 11).",
                PermisosMinimos: ["db_datareader (read-only)"]));

        if (e.UsaPostgres)
            lista.Add(new(
                Nombre: "postgres",
                Categoria: CategoriaMcp.BaseDatos,
                Slide: "3",
                Porque: "Queries Postgres oficiales de Anthropic (slide 3).",
                PermisosMinimos: ["usuario read-only por defecto"]));

        if (e.UsaNotionODocs)
            lista.Add(new(
                Nombre: "notion",
                Categoria: CategoriaMcp.Productividad,
                Slide: "7/11",
                Porque: "Documentación y bases de datos en Notion (slide 7).",
                PermisosMinimos: ["read+write SOLO sobre el workspace del equipo"]));

        if (e.UsaSlackOTeams)
            lista.Add(new(
                Nombre: "slack",
                Categoria: CategoriaMcp.Productividad,
                Slide: "7/14",
                Porque: "Avisos al canal del equipo desde un workflow (slide 14).",
                PermisosMinimos: ["chat:write SOLO en canales del bot"]));

        if (e.UsaJiraOLinear)
            lista.Add(new(
                Nombre: "linear",
                Categoria: CategoriaMcp.Productividad,
                Slide: "15",
                Porque: "Issues + roadmap como Linear/Jira/Asana (slide 15).",
                PermisosMinimos: ["read + comment; write si Claude crea issues"]));

        if (e.NecesitaBrowserAutomation)
            lista.Add(new(
                Nombre: "puppeteer",
                Categoria: CategoriaMcp.Desarrollo,
                Slide: "11",
                Porque: "Automatización de navegador (E2E, smoke).",
                PermisosMinimos: ["restringido a localhost o dominios concretos"]));

        if (e.NecesitaObservabilidad)
            lista.Add(new(
                Nombre: "sentry",
                Categoria: CategoriaMcp.Observabilidad,
                Slide: "15",
                Porque: "Errores y traces para troubleshooting (slide 15).",
                PermisosMinimos: ["read-only del proyecto"]));

        return lista;
    }
}
