using ClaudeCode.Limites.Demo.Api.Limites;

namespace ClaudeCode.Limites.Demo.Api.Tests;

// CAPA 1 — detector de anti-patterns (slide 13).
[Trait("Category", "Unit")]
public class Unit_AntiPatternTests
{
    [Theory]
    [InlineData("Hazme todo el sistema de una vez", AntiPattern.EscribemeTodoElSistema)]
    [InlineData("Funciona, no toco", AntiPattern.AceptarSinEntender)]
    [InlineData("Cada vez de cero, sin claude.md", AntiPattern.SinContextoDeProyecto)]
    [InlineData("Tests luego, primero merge", AntiPattern.SkipTestsPorVelocidad)]
    [InlineData("Confío en el primer output sin verificar", AntiPattern.ConfianzaEnPrimerOutput)]
    [InlineData("Sin memory ni subagent, repito el contexto", AntiPattern.SinMemoryNiContext)]
    [InlineData("Que decida Claude, no pienso yo", AntiPattern.ClaudeLoArreglaTodo)]
    [InlineData("Genero código ignorando el dominio del negocio", AntiPattern.SinContextoDeNegocio)]
    [InlineData("Le paso la connection string real en el prompt", AntiPattern.SecretosOPiiEnPrompt)]
    [InlineData("Claude commitea directo a main sin review humano", AntiPattern.IaEnCiSinGuardrails)]
    public void Detecta_Cada_Anti_Pattern_Por_Su_Frase_Canonica(
        string descripcion, AntiPattern esperado)
    {
        var r = AntiPatternDetector.Detectar(descripcion);
        Assert.False(r.Limpio);
        Assert.Contains(r.Hallazgos, h => h.Pattern == esperado);
    }

    [Fact]
    public void Detecta_Multiples_Anti_Patterns_En_La_Misma_Descripcion()
    {
        var r = AntiPatternDetector.Detectar(
            "Le paso la connection string real, sin tests, y Claude mergea directo.");
        Assert.True(r.Hallazgos.Count >= 3);
    }

    [Fact]
    public void No_Duplica_Mismo_Anti_Pattern_Si_Aparece_Varias_Veces()
    {
        var r = AntiPatternDetector.Detectar(
            "Todo el sistema. Todo el código. Todo el proyecto generado por Claude.");
        Assert.Equal(1, r.Hallazgos.Count(h => h.Pattern == AntiPattern.EscribemeTodoElSistema));
    }

    [Fact]
    public void Descripcion_Limpia_Sin_Hallazgos()
    {
        var r = AntiPatternDetector.Detectar(
            "Itero en chunks pequeños, reviso cada línea, tengo CLAUDE.md actualizado.");
        Assert.True(r.Limpio);
        Assert.Empty(r.Hallazgos);
    }

    [Fact]
    public void Cada_Hallazgo_Lleva_Causa_Y_Fix_No_Vacios()
    {
        var r = AntiPatternDetector.Detectar("Hazme todo el sistema sin tests");
        Assert.All(r.Hallazgos, h =>
        {
            Assert.False(string.IsNullOrWhiteSpace(h.Causa));
            Assert.False(string.IsNullOrWhiteSpace(h.Fix));
        });
    }

    [Fact]
    public void Descripcion_Vacia_Lanza_ArgumentException()
    {
        Assert.Throws<ArgumentException>(() => AntiPatternDetector.Detectar(" "));
    }
}
