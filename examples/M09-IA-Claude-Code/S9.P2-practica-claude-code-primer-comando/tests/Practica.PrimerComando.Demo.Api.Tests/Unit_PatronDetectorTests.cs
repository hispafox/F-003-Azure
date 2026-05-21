using Practica.PrimerComando.Demo.Api.PrimerComando;

namespace Practica.PrimerComando.Demo.Api.Tests;

// CAPA 1 — detector de patterns de prompt (slide 12).
[Trait("Category", "Unit")]
public class Unit_PatronDetectorTests
{
    [Fact]
    public void Mejora_El_Codigo_Es_Anti_Muy_Generico()
    {
        var r = PromptPatronDetector.Analizar("Mejora el código por favor");
        Assert.True(r.TieneAntiPatterns);
        Assert.Contains(r.Hallazgos, h => h.Patron == PatronPrompt.AntiMuyGenerico);
    }

    [Fact]
    public void Arregla_Bugs_Es_Anti_Adivinar()
    {
        var r = PromptPatronDetector.Analizar("Arregla los bugs que veas");
        Assert.True(r.TieneAntiPatterns);
        Assert.Contains(r.Hallazgos, h => h.Patron == PatronPrompt.AntiPedirleAdivinar);
    }

    [Fact]
    public void Crea_Api_Completa_Es_Anti_Todo_De_Golpe()
    {
        var r = PromptPatronDetector.Analizar(
            "Crea una API REST con auth, BBDD, tests, CI/CD y deploy");
        Assert.True(r.TieneAntiPatterns);
        Assert.Contains(r.Hallazgos, h => h.Patron == PatronPrompt.AntiTodoDeGolpe);
    }

    [Fact]
    public void Antes_De_Implementar_Es_Pattern_Positivo()
    {
        var r = PromptPatronDetector.Analizar(
            "Antes de implementar, dime cómo lo harías y por qué");
        Assert.False(r.TieneAntiPatterns);
        Assert.Contains(r.Hallazgos, h => h.Patron == PatronPrompt.BuenoConfirmacionPrevia);
    }

    [Fact]
    public void Rubber_Duck_Es_Pattern_Positivo()
    {
        var r = PromptPatronDetector.Analizar(
            "Estoy intentando hacer X. Mi enfoque actual es Z. ¿Me explico mal en algo?");
        Assert.False(r.TieneAntiPatterns);
        Assert.Contains(r.Hallazgos, h => h.Patron == PatronPrompt.BuenoRubberDuck);
    }

    [Fact]
    public void Prompt_Neutro_Tiene_Puntuacion_50()
    {
        var r = PromptPatronDetector.Analizar(
            "Refactoriza Program.cs y extrae la llamada HTTP a FetchAsync");
        Assert.False(r.TieneAntiPatterns);
        Assert.Empty(r.Hallazgos);
        Assert.Equal(50, r.PuntuacionEstimada);
    }

    [Fact]
    public void Dos_Anti_Patterns_Restan_50_Puntos()
    {
        // Base 50, -25 por mejora el código, -25 por arregla los bugs = 0.
        var r = PromptPatronDetector.Analizar("Mejora el código y arregla los bugs");
        Assert.Equal(0, r.PuntuacionEstimada);
    }

    [Fact]
    public void Anti_Y_Pattern_Positivo_Se_Compensan_Parcialmente()
    {
        // Base 50, -25 mejora el código, +25 antes de implementar = 50.
        var r = PromptPatronDetector.Analizar(
            "Antes de implementar, mejora el código");
        Assert.Equal(50, r.PuntuacionEstimada);
    }

    [Fact]
    public void Prompt_Vacio_Lanza_ArgumentException()
    {
        Assert.Throws<ArgumentException>(() => PromptPatronDetector.Analizar(" "));
    }

    [Fact]
    public void Cada_Hallazgo_Lleva_Causa_Y_Fix()
    {
        var r = PromptPatronDetector.Analizar("Mejora el código");
        Assert.All(r.Hallazgos, h =>
        {
            Assert.False(string.IsNullOrWhiteSpace(h.Causa));
            Assert.False(string.IsNullOrWhiteSpace(h.SugerenciaFix));
        });
    }
}
