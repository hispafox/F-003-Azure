using Bonus.IntroIaAgentica.Demo.Api.Intro;

namespace Bonus.IntroIaAgentica.Demo.Api.Tests;

// CAPA 1 — clasificador por generación (slide 3).
[Trait("Category", "Unit")]
public class Unit_GeneracionTests
{
    [Theory]
    [InlineData("Uso Claude Code en terminal para refactorizar", GeneracionIa.Gen3Agente)]
    [InlineData("Cursor con MCP edita el repo entero", GeneracionIa.Gen3Agente)]
    [InlineData("Cowork desktop con scheduled tasks", GeneracionIa.Gen3Agente)]
    [InlineData("Copy paste de mi código a ChatGPT", GeneracionIa.Gen2Chat)]
    [InlineData("Conversación con la IA en claude.ai", GeneracionIa.Gen2Chat)]
    [InlineData("GitHub Copilot inline sugerencias línea a línea", GeneracionIa.Gen1Autocompletado)]
    [InlineData("Autocompletado en VS Code mientras tecleo", GeneracionIa.Gen1Autocompletado)]
    public void Clasifica_Cada_Generacion_Correctamente(string descripcion, GeneracionIa esperada)
    {
        var r = GeneracionIaClassifier.Clasificar(descripcion);
        Assert.Equal(esperada, r.Generacion);
    }

    [Fact]
    public void Descripcion_Sin_Palabras_Clave_Es_Desconocida()
    {
        var r = GeneracionIaClassifier.Clasificar("uso algo de ia para mi trabajo");
        Assert.Equal(GeneracionIa.Desconocida, r.Generacion);
    }

    [Fact]
    public void Gen3_Agente_Tiene_Anios_2025_2026()
    {
        var r = GeneracionIaClassifier.Clasificar("Claude Code edita 15 archivos");
        Assert.Contains("2025", r.Anios, StringComparison.Ordinal);
    }

    [Fact]
    public void Cada_Generacion_Tiene_Contexto_Y_Accion()
    {
        var r = GeneracionIaClassifier.Clasificar("Claude Code en terminal");
        Assert.False(string.IsNullOrWhiteSpace(r.Contexto));
        Assert.False(string.IsNullOrWhiteSpace(r.Accion));
    }

    [Fact]
    public void Descripcion_Vacia_Lanza_ArgumentException()
    {
        Assert.Throws<ArgumentException>(() => GeneracionIaClassifier.Clasificar(" "));
    }
}
