using Security.Demo.Api.Security;

namespace Security.Demo.Api.Tests;

// CAPA 1 — STRIDE (slide 20).
[Trait("Category", "Unit")]
public class Unit_StrideAnalyzerTests
{
    [Theory]
    [InlineData(Stride.Spoofing, 'S')]
    [InlineData(Stride.Tampering, 'T')]
    [InlineData(Stride.Repudiation, 'R')]
    [InlineData(Stride.InformationDisclosure, 'I')]
    [InlineData(Stride.DenialOfService, 'D')]
    [InlineData(Stride.ElevationOfPrivilege, 'E')]
    public void Describir_InicialCorrecta_Y_Con_Mitigaciones(Stride s, char inicial)
    {
        var a = StrideAnalyzer.Describir(s);
        Assert.Equal(inicial, a.Inicial);
        Assert.NotEmpty(a.Mitigaciones);
        Assert.False(string.IsNullOrWhiteSpace(a.Amenaza));
    }

    [Theory]
    [InlineData('s', Stride.Spoofing)]
    [InlineData('E', Stride.ElevationOfPrivilege)]
    public void PorInicial(char c, Stride esperado)
        => Assert.Equal(esperado, StrideAnalyzer.PorInicial(c));

    [Fact]
    public void PorInicial_Invalida_Lanza()
        => Assert.Throws<ArgumentOutOfRangeException>(() => StrideAnalyzer.PorInicial('Z'));

    [Fact]
    public void Las_6_Iniciales_Forman_STRIDE()
    {
        var iniciales = Enum.GetValues<Stride>()
            .Select(s => StrideAnalyzer.Describir(s).Inicial);
        Assert.Equal("STRIDE", new string([.. iniciales]));
    }
}
