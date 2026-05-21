using Bonus.SetupAzure.Demo.Api.Setup;

namespace Bonus.SetupAzure.Demo.Api.Tests;

// CAPA 1 — slide 5/6: evaluador de calidad del CLAUDE.md.
[Trait("Category", "Unit")]
public class Unit_ClaudeMdQualityEvaluatorTests
{
    private const string Vago = "# Mi proyecto\n\nUna app.";

    private const string Bien =
        "# Proyecto Ventas\n\n## Stack\n- .NET 8 con Cosmos DB y Azure Functions\n" +
        "## Convenciones\n- async/await siempre\n- ILogger con logging estructurado\n" +
        "## Comandos\n- dotnet build\n- dotnet test\n" +
        "## Arquitectura\nVer docs/architecture.md.\n" +
        "## Glosario\n- Ventas: bounded context.\n" +
        "## No tocar sin preguntar\n- infrastructure/modules/rbac.bicep\n";

    [Fact]
    public void Vago_Tiene_Puntuacion_Baja_Y_Muchas_Sugerencias()
    {
        var r = ClaudeMdQualityEvaluator.Evaluar(Vago);

        Assert.True(r.Puntuacion < 30);
        Assert.True(r.Sugerencias.Count >= 4);
    }

    [Fact]
    public void ClaudeMd_Completo_Suma_100_Puntos()
    {
        var r = ClaudeMdQualityEvaluator.Evaluar(Bien);

        Assert.Equal(100, r.Puntuacion);
        Assert.Empty(r.SeccionesFaltantes);
    }

    [Fact]
    public void Detecta_Secreto_Password_Literal()
    {
        var r = ClaudeMdQualityEvaluator.Evaluar(
            "# Proyecto\n\n## Stack\n.NET 8.\n\nConnection string: Server=tcp:sql.db;Password=secret123;");

        Assert.NotEmpty(r.AvisosDeAntiPatrones);
        Assert.Contains(r.AvisosDeAntiPatrones, a =>
            a.Contains("password", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Detecta_Connection_String_Literal()
    {
        var r = ClaudeMdQualityEvaluator.Evaluar(
            "# Proyecto\n\n## Stack\n.NET 8.\n\nConnection string: Server=tcp:sql.db;");

        Assert.Contains(r.AvisosDeAntiPatrones, a =>
            a.Contains("connection string", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Detecta_Placeholder_Xxxxx()
    {
        var r = ClaudeMdQualityEvaluator.Evaluar(
            "# Proyecto\n## Stack\n.NET 8.\n## Convenciones\nasync.\nApiKey: xxxxxxxx\n");

        Assert.Contains(r.AvisosDeAntiPatrones, a =>
            a.Contains("xxxxxxxx", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ClaudeMd_Muy_Largo_Genera_Aviso_De_Tamano()
    {
        var bloque = string.Join("\n", Enumerable.Repeat("- item.", 90));
        var r = ClaudeMdQualityEvaluator.Evaluar(
            "# Proyecto\n## Stack\n.NET 8.\n## Convenciones\nasync.\n" + bloque);

        Assert.Contains(r.AvisosDeAntiPatrones, a =>
            a.Contains("líneas", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Stack_Convenciones_ZonasFragiles_Pesan_70_Puntos()
    {
        var r = ClaudeMdQualityEvaluator.Evaluar(
            "# X\n## Stack\n.NET 8.\n## Convenciones\nasync/await.\n## No tocar sin preguntar\nrbac.bicep");

        Assert.Equal(70, r.Puntuacion);
    }

    [Fact]
    public void Sugerencias_Incluyen_Las_Secciones_Faltantes()
    {
        var r = ClaudeMdQualityEvaluator.Evaluar(Vago);

        Assert.Contains(r.Sugerencias, s =>
            s.Contains("Stack", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(r.Sugerencias, s =>
            s.Contains("Convenciones", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Evaluar_Con_Vacio_Lanza()
    {
        Assert.Throws<ArgumentException>(() =>
            ClaudeMdQualityEvaluator.Evaluar("   "));
    }
}
