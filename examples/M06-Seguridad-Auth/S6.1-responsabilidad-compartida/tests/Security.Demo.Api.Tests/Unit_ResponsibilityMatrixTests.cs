using Security.Demo.Api.Security;

namespace Security.Demo.Api.Tests;

// CAPA 1 — el modelo de responsabilidad compartida (slide 3).
[Trait("Category", "Unit")]
public class Unit_ResponsibilityMatrixTests
{
    [Theory]
    // La línea que NUNCA cambia: datos/identidades/dispositivos = TÚ.
    [InlineData(Capa.DatosYClasificacion, ModeloServicio.SaaS, Responsable.Tu)]
    [InlineData(Capa.CuentasEIdentidades, ModeloServicio.PaaS, Responsable.Tu)]
    [InlineData(Capa.DispositivosCliente, ModeloServicio.IaaS, Responsable.Tu)]
    // Aplicación: tuya salvo en SaaS.
    [InlineData(Capa.Aplicacion, ModeloServicio.PaaS, Responsable.Tu)]
    [InlineData(Capa.Aplicacion, ModeloServicio.SaaS, Responsable.Azure)]
    // Controles de red: mixto en PaaS.
    [InlineData(Capa.ControlesDeRed, ModeloServicio.IaaS, Responsable.Tu)]
    [InlineData(Capa.ControlesDeRed, ModeloServicio.PaaS, Responsable.Mixto)]
    [InlineData(Capa.ControlesDeRed, ModeloServicio.SaaS, Responsable.Azure)]
    // SO: Azure desde PaaS.
    [InlineData(Capa.SistemaOperativo, ModeloServicio.IaaS, Responsable.Tu)]
    [InlineData(Capa.SistemaOperativo, ModeloServicio.PaaS, Responsable.Azure)]
    // Hosts/red/datacenter: Azure salvo On-Prem.
    [InlineData(Capa.Datacenter, ModeloServicio.OnPrem, Responsable.Tu)]
    [InlineData(Capa.Datacenter, ModeloServicio.IaaS, Responsable.Azure)]
    [InlineData(Capa.HostsFisicos, ModeloServicio.IaaS, Responsable.Azure)]
    public void Responsable_TablaSlide3(Capa c, ModeloServicio m, Responsable esperado)
        => Assert.Equal(esperado, ResponsibilityMatrix.Responsable(c, m));

    [Theory]
    [InlineData(Capa.DatosYClasificacion, true)]
    [InlineData(Capa.CuentasEIdentidades, true)]
    [InlineData(Capa.DispositivosCliente, true)]
    [InlineData(Capa.Aplicacion, false)]
    [InlineData(Capa.Datacenter, false)]
    public void SiempreTuya(Capa c, bool esperado)
        => Assert.Equal(esperado, ResponsibilityMatrix.SiempreTuya(c));

    [Fact]
    public void OnPrem_Todo_Es_Tuyo()
    {
        foreach (var c in Enum.GetValues<Capa>())
            Assert.Equal(Responsable.Tu,
                ResponsibilityMatrix.Responsable(c, ModeloServicio.OnPrem));
    }
}
