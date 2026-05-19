using Distribution.Demo.Api.Distribution;

namespace Distribution.Demo.Api.Tests;

// CAPA 1 — comparativa de formatos (slides 4, 11, 26).
[Trait("Category", "Unit")]
public class Unit_ComparatorTests
{
    [Theory]
    [InlineData(FormatoDistribucion.ClickOnce, CaracteristicaDistribucion.AutoUpdate, true)]
    [InlineData(FormatoDistribucion.ClickOnce, CaracteristicaDistribucion.Sandboxing, false)]
    [InlineData(FormatoDistribucion.Msix, CaracteristicaDistribucion.Sandboxing, true)]
    [InlineData(FormatoDistribucion.Msix, CaracteristicaDistribucion.DotNet8Plus, true)]
    [InlineData(FormatoDistribucion.Msix, CaracteristicaDistribucion.IntuneCompatible, true)]
    [InlineData(FormatoDistribucion.ClickOnce, CaracteristicaDistribucion.IntuneCompatible, false)]
    [InlineData(FormatoDistribucion.Msi, CaracteristicaDistribucion.AdminRequired, true)]
    [InlineData(FormatoDistribucion.Winget, CaracteristicaDistribucion.FuturoMicrosoft, true)]
    public void Soporta(FormatoDistribucion f, CaracteristicaDistribucion c, bool esperado)
        => Assert.Equal(esperado, DistributionFormatComparator.Soporta(f, c));

    [Fact]
    public void Msix_Gana_Claramente_A_ClickOnce()
    {
        int ventajas = DistributionFormatComparator.VentajasMsixSobreClickOnce();
        // Slide 4 — al menos sandboxing, app identity, modern APIs,
        // .NET 8+, Intune, MS Store, differential updates, futuro MS.
        Assert.True(ventajas >= 7, $"Solo {ventajas} ventajas detectadas");
    }
}
