using ClaudeCode.Infra.Demo.Api.Infra;

namespace ClaudeCode.Infra.Demo.Api.Tests;

// CAPA 1 — prompts canónicos por escenario (slides 2-17).
[Trait("Category", "Unit")]
public class Unit_PromptBuilderTests
{
    [Theory]
    [InlineData(EscenarioInfra.BicepDesdeRequirements, "Bicep modular")]
    [InlineData(EscenarioInfra.DockerfileMultiStage, "multi-stage")]
    [InlineData(EscenarioInfra.GhActionsPipeline, "azure/login@v2")]
    [InlineData(EscenarioInfra.ReverseArmABicep, "az bicep decompile")]
    [InlineData(EscenarioInfra.AuditarRecursos, "az resource list")]
    [InlineData(EscenarioInfra.RunbookOperaciones, "Síntomas")]
    [InlineData(EscenarioInfra.ScriptOps, "ops-toolkit.sh")]
    public void Cada_Escenario_Devuelve_Texto_Caracteristico(
        EscenarioInfra esc, string textoEsperado)
    {
        var p = InfraPromptBuilder.ParaEscenario(esc);
        Assert.Equal(esc, p.Escenario);
        Assert.Contains(textoEsperado, p.Texto, StringComparison.Ordinal);
    }

    [Fact]
    public void Bicep_Con_Requirements_Refleja_Recursos_Detectados()
    {
        var req = InfraRequirementsParser.Parsear(
            "App Service + Cosmos DB + Key Vault, multi-region UE.");
        var p = InfraPromptBuilder.ParaEscenario(
            EscenarioInfra.BicepDesdeRequirements, req);
        Assert.Contains("AppService", p.Texto, StringComparison.Ordinal);
        Assert.Contains("CosmosDb", p.Texto, StringComparison.Ordinal);
        Assert.Contains("multi-region", p.Texto, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bicep_Sin_Requirements_Tiene_Placeholder_Generico()
    {
        var p = InfraPromptBuilder.ParaEscenario(
            EscenarioInfra.BicepDesdeRequirements, req: null);
        Assert.Contains("rellena", p.Texto, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Gh_Actions_Menciona_Oidc_Y_Auto_Rollback()
    {
        var p = InfraPromptBuilder.ParaEscenario(EscenarioInfra.GhActionsPipeline);
        Assert.Contains("OIDC", p.Texto, StringComparison.Ordinal);
        Assert.Contains("rollback", p.Texto, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Reverse_Arm_Bicep_Pide_What_If_De_Verificacion()
    {
        var p = InfraPromptBuilder.ParaEscenario(EscenarioInfra.ReverseArmABicep);
        Assert.Contains("what-if", p.Texto, StringComparison.Ordinal);
    }

    [Fact]
    public void Audit_Prompt_Cubre_HTTPS_TLS_Tags_Y_Mi()
    {
        var p = InfraPromptBuilder.ParaEscenario(EscenarioInfra.AuditarRecursos);
        Assert.Contains("httpsOnly", p.Texto, StringComparison.Ordinal);
        Assert.Contains("TLS", p.Texto, StringComparison.Ordinal);
        Assert.Contains("tags", p.Texto, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Managed Identity", p.Texto, StringComparison.Ordinal);
    }
}
