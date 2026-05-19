using Desktop.Demo.Api.Desktop;

namespace Desktop.Demo.Api.Tests;

// CAPA 1 — ciclo de vida del token en desktop (slides 10, 12).
[Trait("Category", "Unit")]
public class Unit_TokenLifecycleTests
{
    [Fact]
    public void Sin_Cuenta_Interactive()                       // slide 10.1
        => Assert.Equal(AccionToken.Interactive,
            TokenLifecycle.Siguiente(new(false, false, false, false)));

    [Fact]
    public void Access_Valido_Cache_Silent()                   // slide 10.2
        => Assert.Equal(AccionToken.UsarCacheSilent,
            TokenLifecycle.Siguiente(new(true, true, true, false)));

    [Fact]
    public void Access_Caducado_Refresh_Valido_RefrescarSilent() // slide 10.3
        => Assert.Equal(AccionToken.RefrescarSilent,
            TokenLifecycle.Siguiente(new(true, false, true, false)));

    [Fact]
    public void Refresh_Caducado_Interactive()                 // slide 10.4
        => Assert.Equal(AccionToken.Interactive,
            TokenLifecycle.Siguiente(new(true, false, false, false)));

    [Fact]
    public void Reto_ConditionalAccess_Manda_Sobre_Todo()      // slide 12
        => Assert.Equal(AccionToken.InteractiveConClaims,
            TokenLifecycle.Siguiente(new(true, true, true, true)));

    [Theory]
    [InlineData(AccionToken.UsarCacheSilent, false)]
    [InlineData(AccionToken.RefrescarSilent, false)]
    [InlineData(AccionToken.Interactive, true)]
    [InlineData(AccionToken.InteractiveConClaims, true)]
    public void RequiereUi(AccionToken a, bool esperado)
        => Assert.Equal(esperado, TokenLifecycle.RequiereUi(a));

    [Fact]
    public void Estado_Null_Lanza()
        => Assert.Throws<ArgumentNullException>(
            () => TokenLifecycle.Siguiente(null!));
}
