using KeyVault.Demo.Api.KeyVault;

namespace KeyVault.Demo.Api.Tests;

// CAPA 1 — política de rotación (slides 8-9).
[Trait("Category", "Unit")]
public class Unit_SecretRotationPolicyTests
{
    private static readonly DateTimeOffset Ahora =
        new(2026, 5, 19, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Vigente_Lejos_De_Expirar()
    {
        var e = SecretRotationPolicy.Evaluar(Ahora.AddDays(120), Ahora);
        Assert.Equal(EstadoSecreto.Vigente, e.Estado);
        Assert.False(e.DebeRotar);
        Assert.Equal(120, e.DiasRestantes);
    }

    [Fact]
    public void Proximo_A_Expirar_Dentro_De_Ventana_30d()
    {
        var e = SecretRotationPolicy.Evaluar(Ahora.AddDays(20), Ahora);
        Assert.Equal(EstadoSecreto.ProximoAExpirar, e.Estado);
        Assert.True(e.DebeRotar);   // dispara SecretNearExpiry (slide 9)
    }

    [Fact]
    public void Expirado_Debe_Rotar_Ya()
    {
        var e = SecretRotationPolicy.Evaluar(Ahora.AddDays(-3), Ahora);
        Assert.Equal(EstadoSecreto.Expirado, e.Estado);
        Assert.True(e.DebeRotar);
        Assert.True(e.DiasRestantes < 0);
    }

    [Fact]
    public void Ventana_Configurable()
    {
        // Con ventana de 7 días, a 20 días aún está Vigente.
        var e = SecretRotationPolicy.Evaluar(Ahora.AddDays(20), Ahora, ventanaDias: 7);
        Assert.Equal(EstadoSecreto.Vigente, e.Estado);
    }

    [Fact]
    public void Ventana_Negativa_Lanza()
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => SecretRotationPolicy.Evaluar(Ahora, Ahora, -1));
}
