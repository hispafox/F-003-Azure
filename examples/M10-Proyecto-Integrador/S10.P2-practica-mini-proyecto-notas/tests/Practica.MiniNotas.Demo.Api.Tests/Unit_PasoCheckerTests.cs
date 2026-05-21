using Practica.MiniNotas.Demo.Api.MiniNotas;

namespace Practica.MiniNotas.Demo.Api.Tests;

// CAPA 1 — evaluador de los 11 pasos (slides 4-14).
[Trait("Category", "Unit")]
public class Unit_PasoCheckerTests
{
    [Fact]
    public void Comando_Y_Output_Es_Pasa()
    {
        var r = PasoChecker.Evaluar(new EvidenciaPaso(
            Paso.CrearSolucion, ComandoEjecutado: true, OutputEsperadoVisible: true));
        Assert.Equal(ResultadoPaso.Pasa, r.Resultado);
    }

    [Fact]
    public void Ni_Comando_Ni_Output_Es_Falla()
    {
        var r = PasoChecker.Evaluar(new EvidenciaPaso(
            Paso.CrearSolucion, ComandoEjecutado: false, OutputEsperadoVisible: false));
        Assert.Equal(ResultadoPaso.Falla, r.Resultado);
    }

    [Fact]
    public void Comando_Pero_Output_No_Es_Pendiente()
    {
        var r = PasoChecker.Evaluar(new EvidenciaPaso(
            Paso.DesplegarApp, ComandoEjecutado: true, OutputEsperadoVisible: false));
        Assert.Equal(ResultadoPaso.Pendiente, r.Resultado);
    }

    [Theory]
    [InlineData(Paso.DisenarModelo, "4")]
    [InlineData(Paso.CrearSolucion, "5")]
    [InlineData(Paso.ImplementarModelo, "6")]
    [InlineData(Paso.ImplementarRepositorio, "7")]
    [InlineData(Paso.EndpointsCrud, "8")]
    [InlineData(Paso.TestsUnitarios, "9")]
    [InlineData(Paso.SmokeTests, "10")]
    [InlineData(Paso.CrearInfra, "11")]
    [InlineData(Paso.DesplegarApp, "12")]
    [InlineData(Paso.ValidarEndToEnd, "13")]
    [InlineData(Paso.Limpiar, "14")]
    public void Cada_Paso_Mapea_A_Su_Slide(Paso p, string slide)
    {
        var r = PasoChecker.Evaluar(new EvidenciaPaso(p, true, true));
        Assert.Equal(slide, r.Slide);
    }

    [Fact]
    public void Crear_Solucion_Sugiere_Dotnet_New_Sln()
    {
        var r = PasoChecker.Evaluar(new EvidenciaPaso(
            Paso.CrearSolucion, ComandoEjecutado: false, OutputEsperadoVisible: false));
        Assert.Contains(r.AccionesSugeridas, s =>
            s.Contains("dotnet new sln", StringComparison.Ordinal));
    }

    [Fact]
    public void Endpoints_Crud_Sugiere_5_Endpoints()
    {
        var r = PasoChecker.Evaluar(new EvidenciaPaso(
            Paso.EndpointsCrud, ComandoEjecutado: false, OutputEsperadoVisible: false));
        Assert.Contains(r.AccionesSugeridas, s =>
            s.Contains("/notes", StringComparison.Ordinal));
    }

    [Fact]
    public void Limpiar_Sugiere_Az_Group_Delete()
    {
        var r = PasoChecker.Evaluar(new EvidenciaPaso(
            Paso.Limpiar, ComandoEjecutado: false, OutputEsperadoVisible: false));
        Assert.Contains(r.AccionesSugeridas, s =>
            s.Contains("az group delete", StringComparison.Ordinal));
    }
}
