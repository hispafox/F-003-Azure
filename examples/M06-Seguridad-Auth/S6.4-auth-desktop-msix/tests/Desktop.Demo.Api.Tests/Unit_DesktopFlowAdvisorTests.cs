using Desktop.Demo.Api.Desktop;

namespace Desktop.Demo.Api.Tests;

// CAPA 1 — método de auth desktop por contexto (slide 3-4).
[Trait("Category", "Unit")]
public class Unit_DesktopFlowAdvisorTests
{
    [Theory]
    [InlineData(ContextoDesktop.WindowsEntraJoined, MetodoAuthDesktop.Wam)]
    [InlineData(ContextoDesktop.WindowsGenerico, MetodoAuthDesktop.SystemBrowser)]
    [InlineData(ContextoDesktop.MultiPlataforma, MetodoAuthDesktop.SystemBrowser)]
    [InlineData(ContextoDesktop.KioscoOCli, MetodoAuthDesktop.DeviceCode)]
    public void Recomendar(ContextoDesktop c, MetodoAuthDesktop esperado)
        => Assert.Equal(esperado, DesktopFlowAdvisor.Recomendar(c));

    [Theory]
    [InlineData(MetodoAuthDesktop.Wam, true)]
    [InlineData(MetodoAuthDesktop.SystemBrowser, true)]
    [InlineData(MetodoAuthDesktop.DeviceCode, true)]
    [InlineData(MetodoAuthDesktop.EmbeddedBrowser, false)]   // solo "aceptable"
    public void EsRecomendado(MetodoAuthDesktop m, bool esperado)
        => Assert.Equal(esperado, DesktopFlowAdvisor.EsRecomendado(m));

    [Fact]
    public void Desktop_Siempre_Es_Cliente_Publico()
        => Assert.True(DesktopFlowAdvisor.EsClientePublico);
}
