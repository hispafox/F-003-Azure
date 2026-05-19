using Security.Demo.Api.Security;

namespace Security.Demo.Api.Tests;

// CAPA 1 — Secure Score del checklist (slides 10, 17).
[Trait("Category", "Unit")]
public class Unit_SecureScoreTests
{
    private static ChecklistSeguridad Todo(bool v) =>
        new(v, v, v, v, v, v, v, v, v, v, v);

    private readonly ISecureScore _svc = new SecureScoreCalculator();

    [Fact]
    public void Todo_Cumplido_100()
    {
        var r = _svc.Calcular(Todo(true));
        Assert.Equal(100, r.Puntuacion);
        Assert.Empty(r.Faltantes);
        Assert.Equal("Excelente", r.Veredicto);
    }

    [Fact]
    public void Nada_Cumplido_0_Critico()
    {
        var r = _svc.Calcular(Todo(false));
        Assert.Equal(0, r.Puntuacion);
        Assert.Equal(r.Total, r.Faltantes.Count);
        Assert.StartsWith("Crítico", r.Veredicto);
    }

    [Fact]
    public void Parcial_Calcula_Porcentaje_Y_Faltantes()
    {
        // 8 de 11 true → 73 → "Aceptable" (≥70).
        var c = new ChecklistSeguridad(
            true, true, true, true, true, true, true, true,
            false, false, false);
        var r = _svc.Calcular(c);
        Assert.Equal(8, r.Cumplidos);
        Assert.Equal(11, r.Total);
        Assert.Equal(73, r.Puntuacion);
        Assert.Equal(3, r.Faltantes.Count);
        Assert.StartsWith("Aceptable", r.Veredicto);
    }

    [Fact]
    public void Null_Lanza()
        => Assert.Throws<ArgumentNullException>(() => _svc.Calcular(null!));
}
