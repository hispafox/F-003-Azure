using System.Text.Json;

namespace ClaudeCode.Mcp.Demo.Api.Mcp;

public sealed record McpServer(
    string Nombre,
    string Command,
    IReadOnlyList<string> Args,
    IReadOnlyDictionary<string, string> Env);

public sealed record McpConfig(
    IReadOnlyList<McpServer> Servers,
    IReadOnlyList<string> Avisos);

// Slide 3 — parser del `claude_desktop_config.json` (root con la clave
// `mcpServers`). Lógica pura. Extrae cada server con su comando, args
// y env vars. Reporta avisos si el JSON no tiene la forma esperada.
public static class McpConfigParser
{
    public static McpConfig Parsear(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        JsonDocument doc;
        try { doc = JsonDocument.Parse(json); }
        catch (JsonException ex)
        {
            return new McpConfig([], [$"JSON inválido: {ex.Message}"]);
        }

        var raiz = doc.RootElement;
        var avisos = new List<string>();
        var servers = new List<McpServer>();

        if (!raiz.TryGetProperty("mcpServers", out var serversEl)
            || serversEl.ValueKind != JsonValueKind.Object)
        {
            avisos.Add(
                "Falta la clave `mcpServers` en la raíz, o no es un objeto (slide 3).");
            return new McpConfig(servers, avisos);
        }

        foreach (var prop in serversEl.EnumerateObject())
        {
            string nombre = prop.Name;
            var sv = prop.Value;
            if (sv.ValueKind != JsonValueKind.Object)
            {
                avisos.Add($"Server `{nombre}` no es un objeto JSON.");
                continue;
            }

            string command = sv.TryGetProperty("command", out var c)
                ? c.GetString() ?? ""
                : "";

            var args = new List<string>();
            if (sv.TryGetProperty("args", out var argsEl)
                && argsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var a in argsEl.EnumerateArray())
                    if (a.ValueKind == JsonValueKind.String)
                        args.Add(a.GetString() ?? "");
            }

            var env = new Dictionary<string, string>();
            if (sv.TryGetProperty("env", out var envEl)
                && envEl.ValueKind == JsonValueKind.Object)
            {
                foreach (var ev in envEl.EnumerateObject())
                    env[ev.Name] = ev.Value.GetString() ?? "";
            }

            if (string.IsNullOrWhiteSpace(command))
                avisos.Add($"Server `{nombre}` sin `command` definido.");

            servers.Add(new McpServer(nombre, command, args, env));
        }

        return new McpConfig(servers, avisos);
    }
}
