using ClaudeCode.Mcp.Demo.Api.Mcp;

namespace ClaudeCode.Mcp.Demo.Api.Tests;

// CAPA 1 — parser del claude_desktop_config.json (slide 3).
[Trait("Category", "Unit")]
public class Unit_ConfigParserTests
{
    private const string ConfigCompleto = """
        {
          "mcpServers": {
            "filesystem": {
              "command": "npx",
              "args": ["-y", "@modelcontextprotocol/server-filesystem", "/home/dev/projects"]
            },
            "github": {
              "command": "npx",
              "args": ["-y", "@modelcontextprotocol/server-github"],
              "env": { "GITHUB_TOKEN": "${GH_TOKEN}" }
            }
          }
        }
        """;

    [Fact]
    public void Parsea_Dos_Servers_Con_Args_Y_Env()
    {
        var c = McpConfigParser.Parsear(ConfigCompleto);
        Assert.Equal(2, c.Servers.Count);

        var fs = c.Servers.Single(s => s.Nombre == "filesystem");
        Assert.Equal("npx", fs.Command);
        Assert.Equal(3, fs.Args.Count);
        Assert.Empty(fs.Env);

        var gh = c.Servers.Single(s => s.Nombre == "github");
        Assert.Equal("${GH_TOKEN}", gh.Env["GITHUB_TOKEN"]);
    }

    [Fact]
    public void Sin_Clave_McpServers_Reporta_Aviso()
    {
        var c = McpConfigParser.Parsear("{\"otraCosa\": {}}");
        Assert.Empty(c.Servers);
        Assert.Contains(c.Avisos, a => a.Contains("mcpServers", StringComparison.Ordinal));
    }

    [Fact]
    public void Server_Sin_Command_Genera_Aviso()
    {
        var c = McpConfigParser.Parsear(
            "{\"mcpServers\": {\"x\": { \"args\": [] }}}");
        Assert.Single(c.Servers);
        Assert.Contains(c.Avisos, a => a.Contains("command", StringComparison.Ordinal));
    }

    [Fact]
    public void Json_Invalido_Reporta_Aviso_Y_No_Lanza()
    {
        var c = McpConfigParser.Parsear("{ no json valido");
        Assert.Empty(c.Servers);
        Assert.Contains(c.Avisos, a => a.Contains("JSON inválido", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Server_No_Objeto_Genera_Aviso()
    {
        var c = McpConfigParser.Parsear(
            "{\"mcpServers\": {\"bad\": \"not an object\"}}");
        Assert.Empty(c.Servers);
        Assert.Contains(c.Avisos, a => a.Contains("no es un objeto", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Json_Vacio_Lanza_ArgumentException()
    {
        Assert.Throws<ArgumentException>(() => McpConfigParser.Parsear(" "));
    }
}
