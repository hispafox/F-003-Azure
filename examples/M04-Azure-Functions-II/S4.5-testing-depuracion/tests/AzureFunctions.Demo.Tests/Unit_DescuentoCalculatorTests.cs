using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;

namespace AzureFunctions.Demo.Tests;

// CAPA 1 de la pirámide — Unit tests de lógica de negocio (slide 7).
// Rápidos (ms), sin Azure, sin mocks. Es el ~80% de los tests.
[Trait("Category", "Unit")]
public class Unit_DescuentoCalculatorTests
{
    private readonly DescuentoCalculator _sut = new();

    // Slide 7 — la [Theory] escalonada: el caso de uso canónico de tests
    // tabla. Un solo método cubre toda la curva de decisión.
    [Theory]
    [InlineData(0, 0)]        // sin importe
    [InlineData(99, 0)]       // < 100€  → 0%
    [InlineData(100, 5)]      // 100€    → 5%
    [InlineData(400, 20)]     // tramo 5% (400 * 0.05)
    [InlineData(500, 50)]     // 500€    → 10%
    [InlineData(999, 99.90)]  // tramo 10%
    [InlineData(1000, 150)]   // 1000€   → 15%
    [InlineData(2000, 300)]   // tramo 15%
    public void CalcularDescuento_Escalonado(decimal total, decimal esperado)
        => Assert.Equal(esperado, _sut.CalcularDescuento(total));

    [Fact]
    public void CalcularDescuento_Total_Negativo_Lanza()
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => _sut.CalcularDescuento(-1m));

    [Fact]
    public void Aplicar_Devuelve_Total_Final_Coherente()
    {
        var r = _sut.Aplicar(new Pedido("p1", "c1", 1000m));

        Assert.Equal(150m, r.Descuento);
        Assert.Equal(850m, r.TotalFinal);
        Assert.Equal(1000m, r.Total);
    }
}
