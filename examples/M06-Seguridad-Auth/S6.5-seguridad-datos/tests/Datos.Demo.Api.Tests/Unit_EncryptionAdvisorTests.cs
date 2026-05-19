using Datos.Demo.Api.Datos;

namespace Datos.Demo.Api.Tests;

// CAPA 1 — cifrado at-rest recomendado (slides 6-9).
[Trait("Category", "Unit")]
public class Unit_EncryptionAdvisorTests
{
    [Theory]
    [InlineData(Sensibilidad.Normal, false, EstrategiaCifrado.MmkAtRest)]
    [InlineData(Sensibilidad.Confidencial, false, EstrategiaCifrado.MmkAtRest)]
    [InlineData(Sensibilidad.Normal, true, EstrategiaCifrado.CmkAtRest)]
    [InlineData(Sensibilidad.Confidencial, true, EstrategiaCifrado.CmkAtRest)]
    // AltamenteConfidencial → Always Encrypted, gane o no la regulación.
    [InlineData(Sensibilidad.AltamenteConfidencial, false, EstrategiaCifrado.AlwaysEncrypted)]
    [InlineData(Sensibilidad.AltamenteConfidencial, true, EstrategiaCifrado.AlwaysEncrypted)]
    public void Recomendar(Sensibilidad s, bool reg, EstrategiaCifrado esperado)
        => Assert.Equal(esperado, EncryptionAdvisor.Recomendar(s, reg).Estrategia);

    [Fact]
    public void At_Rest_Siempre_Activo()
    {
        Assert.True(EncryptionAdvisor.AtRestSiempreActivo);
        Assert.True(EncryptionAdvisor.Recomendar(
            Sensibilidad.Normal, false).CifradoAtRestPorDefecto);
    }
}
