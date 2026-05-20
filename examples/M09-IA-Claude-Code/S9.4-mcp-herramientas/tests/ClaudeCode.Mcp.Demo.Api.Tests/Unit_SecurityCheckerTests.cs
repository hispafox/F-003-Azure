using ClaudeCode.Mcp.Demo.Api.Mcp;

namespace ClaudeCode.Mcp.Demo.Api.Tests;

// CAPA 1 — security checker (slide 9).
[Trait("Category", "Unit")]
public class Unit_SecurityCheckerTests
{
    private static McpConfig Parse(string json) =>
        McpConfigParser.Parsear(json);

    [Fact]
    public void Token_Github_Hardcoded_Es_Critico()
    {
        var c = Parse("""
            {
              "mcpServers": {
                "github": {
                  "command": "npx",
                  "args": ["-y", "x"],
                  "env": { "GITHUB_TOKEN": "ghp_abcdefghijklmnopqrstuvwxyz0123456789" }
                }
              }
            }
            """);
        var r = McpSecurityChecker.Comprobar(c);
        Assert.False(r.Seguro);
        Assert.Contains(r.Hallazgos, h =>
            h.Riesgo == NivelRiesgo.Critico
            && h.Causa.Contains("secreto", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Referencia_A_Variable_De_Entorno_No_Es_Critico()
    {
        var c = Parse("""
            {
              "mcpServers": {
                "github": {
                  "command": "npx", "args": ["-y", "x"],
                  "env": { "GITHUB_TOKEN": "${GH_TOKEN}" }
                }
              }
            }
            """);
        var r = McpSecurityChecker.Comprobar(c);
        Assert.DoesNotContain(r.Hallazgos, h =>
            h.Riesgo == NivelRiesgo.Critico
            && h.Causa.Contains("secreto", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Env_Sensible_Vacio_Es_Alto()
    {
        var c = Parse("""
            {
              "mcpServers": {
                "github": {
                  "command": "npx", "args": ["-y", "x"],
                  "env": { "GITHUB_TOKEN": "" }
                }
              }
            }
            """);
        var r = McpSecurityChecker.Comprobar(c);
        Assert.Contains(r.Hallazgos, h =>
            h.Riesgo == NivelRiesgo.Alto
            && h.Causa.Contains("vacío", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Filesystem_Con_Raiz_Es_Critico()
    {
        var c = Parse("""
            {
              "mcpServers": {
                "filesystem": {
                  "command": "npx",
                  "args": ["-y", "@modelcontextprotocol/server-filesystem", "/"]
                }
              }
            }
            """);
        var r = McpSecurityChecker.Comprobar(c);
        Assert.Contains(r.Hallazgos, h =>
            h.Riesgo == NivelRiesgo.Critico
            && h.Causa.Contains("filesystem", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Filesystem_Con_Path_Restringido_No_Es_Critico()
    {
        var c = Parse("""
            {
              "mcpServers": {
                "filesystem": {
                  "command": "npx",
                  "args": ["-y", "@modelcontextprotocol/server-filesystem", "/home/dev/projects/mi-repo"]
                }
              }
            }
            """);
        var r = McpSecurityChecker.Comprobar(c);
        Assert.DoesNotContain(r.Hallazgos, h =>
            h.Riesgo == NivelRiesgo.Critico
            && h.Causa.Contains("filesystem", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Server_Git_Genera_Aviso_Medio_De_Rotacion()
    {
        var c = Parse("""
            {
              "mcpServers": {
                "github": {
                  "command": "npx", "args": ["-y", "x"],
                  "env": { "GITHUB_TOKEN": "${GH}" }
                }
              }
            }
            """);
        var r = McpSecurityChecker.Comprobar(c);
        Assert.Contains(r.Hallazgos, h =>
            h.Riesgo == NivelRiesgo.Medio
            && h.Mitigacion.Contains("rotación", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Config_Limpia_Seguro_True_Cuando_No_Hay_Critico_Ni_Alto()
    {
        var c = Parse("""
            {
              "mcpServers": {
                "filesystem": {
                  "command": "npx",
                  "args": ["-y", "@modelcontextprotocol/server-filesystem", "/home/dev/projects/x"]
                }
              }
            }
            """);
        var r = McpSecurityChecker.Comprobar(c);
        Assert.True(r.Seguro);
        Assert.Equal(0, r.Criticos);
        Assert.Equal(0, r.Altos);
    }
}
