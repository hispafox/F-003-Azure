using WizardMsix.Demo.Api.Wizard;

namespace WizardMsix.Demo.Api.Tests;

// CAPA 1 — catálogo de errores comunes (slide 16).
[Trait("Category", "Unit")]
public class Unit_TroubleshooterTests
{
    [Theory]
    [InlineData("0x80073CFD", "TrustedPeople")]
    [InlineData("MSB3325", "key")]
    [InlineData("NotSigned", "Get-AuthenticodeSignature")]
    public void Diagnostica_Por_Codigo_Exacto(string codigo, string fragmento)
    {
        var d = MsixErrorTroubleshooter.Diagnosticar(codigo);
        Assert.NotNull(d);
        Assert.Equal(codigo, d!.Codigo);
        Assert.True(
            d.Causa.Contains(fragmento, StringComparison.OrdinalIgnoreCase) ||
            d.Diagnostico.Contains(fragmento, StringComparison.OrdinalIgnoreCase) ||
            d.Fix.Contains(fragmento, StringComparison.OrdinalIgnoreCase),
            $"'{fragmento}' no aparece en la entrada de {codigo}");
    }

    [Fact]
    public void Diagnostica_Por_Mensaje_Que_Contiene_El_Codigo()
    {
        var d = MsixErrorTroubleshooter.Diagnosticar(
            "0x80073CFD: The current user does not have permission...");
        Assert.NotNull(d);
        Assert.Equal("0x80073CFD", d!.Codigo);
    }

    [Fact]
    public void Codigo_No_Catalogado_Devuelve_Null()
        => Assert.Null(MsixErrorTroubleshooter.Diagnosticar("0xDEADBEEF"));

    [Fact]
    public void Vacio_Lanza()
        => Assert.Throws<ArgumentException>(() =>
            MsixErrorTroubleshooter.Diagnosticar("   "));

    [Fact]
    public void Catalogo_Tiene_Las_6_Entradas_De_La_Slide_16()
        => Assert.Equal(6, MsixErrorTroubleshooter.Todos().Count);

    [Fact]
    public void Cada_Entrada_Tiene_Causa_Diagnostico_Y_Fix()
    {
        foreach (var d in MsixErrorTroubleshooter.Todos())
        {
            Assert.False(string.IsNullOrWhiteSpace(d.Causa));
            Assert.False(string.IsNullOrWhiteSpace(d.Diagnostico));
            Assert.False(string.IsNullOrWhiteSpace(d.Fix));
        }
    }
}
