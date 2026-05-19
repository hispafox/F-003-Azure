using Desktop.Demo.Api.Desktop;

namespace Desktop.Demo.Api.Tests;

// CAPA 1 — redirect URIs para desktop/MSIX (slides 7, 11).
[Trait("Category", "Unit")]
public class Unit_RedirectUriAdvisorTests
{
    private const string Cid = "abc-123";

    [Fact]
    public void SystemBrowser_Es_Localhost()
        => Assert.Equal("http://localhost",
            RedirectUriAdvisor.Para(TipoApp.SystemBrowser, Cid));

    [Theory]
    [InlineData(TipoApp.Wam)]
    [InlineData(TipoApp.Msix)]
    public void Wam_Y_Msix_Usan_Broker_Plugin(TipoApp tipo)
    {
        var uri = RedirectUriAdvisor.Para(tipo, Cid);
        Assert.Equal($"ms-appx-web://microsoft.aad.brokerplugin/{Cid}", uri);
        Assert.True(RedirectUriAdvisor.EsBroker(uri));
    }

    [Fact]
    public void Legacy_Es_Oob()
    {
        var uri = RedirectUriAdvisor.Para(TipoApp.Legacy, Cid);
        Assert.Equal("urn:ietf:wg:oauth:2.0:oob", uri);
        Assert.True(RedirectUriAdvisor.EsLegacy(uri));
    }

    [Fact]
    public void Localhost_No_Es_Broker_Ni_Legacy()
    {
        Assert.False(RedirectUriAdvisor.EsBroker("http://localhost"));
        Assert.False(RedirectUriAdvisor.EsLegacy("http://localhost"));
    }

    [Fact]
    public void ClientId_Vacio_Lanza()
        => Assert.Throws<ArgumentException>(
            () => RedirectUriAdvisor.Para(TipoApp.Wam, "  "));
}
