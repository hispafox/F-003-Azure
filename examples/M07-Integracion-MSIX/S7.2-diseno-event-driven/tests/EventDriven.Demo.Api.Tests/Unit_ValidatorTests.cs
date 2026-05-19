using EventDriven.Demo.Api.EventDriven;

namespace EventDriven.Demo.Api.Tests;

// CAPA 1 — anti-patterns de eventos (slide 20).
[Trait("Category", "Unit")]
public class Unit_ValidatorTests
{
    [Fact]
    public void Evento_Correcto_Es_Valido()
    {
        var r = EventValidator.Validar(
            new DefinicionEvento("PedidoCreado", ["pedidoId", "clienteId", "version"]));
        Assert.True(r.Valido);
        Assert.Empty(r.Problemas);
    }

    [Fact]
    public void Comando_Disfrazado_Detectado()
    {
        var r = EventValidator.Validar(
            new DefinicionEvento("EnviarEmailAlCliente", ["email", "version"]));
        Assert.False(r.Valido);
        Assert.Contains(r.Problemas, p => p.Contains("COMANDO"));
    }

    [Fact]
    public void Dato_Sensible_Detectado()
    {
        var r = EventValidator.Validar(
            new DefinicionEvento("UsuarioCreado", ["userId", "password", "version"]));
        Assert.Contains(r.Problemas, p => p.Contains("sensibles"));
    }

    [Fact]
    public void Sin_Version_Detectado()
    {
        var r = EventValidator.Validar(
            new DefinicionEvento("PedidoCreado", ["pedidoId"]));
        Assert.Contains(r.Problemas, p => p.Contains("versionado"));
    }

    [Fact]
    public void Acumula_Varios_Problemas()
    {
        var r = EventValidator.Validar(
            new DefinicionEvento("CobrarTarjeta", ["tarjeta", "cvv"]));
        Assert.False(r.Valido);
        Assert.True(r.Problemas.Count >= 3);   // comando + 2 sensibles + sin versión
    }

    [Theory]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(7, false)]
    public void Longitud_Cadena(int saltos, bool valido)
        => Assert.Equal(valido,
            EventValidator.ValidarLongitudCadena(saltos).Valido);

    [Fact]
    public void Cadena_Saltos_No_Positivos_Lanza()
        => Assert.Throws<ArgumentOutOfRangeException>(() =>
            EventValidator.ValidarLongitudCadena(0));

    [Fact]
    public void Tipo_Vacio_Lanza()
        => Assert.Throws<ArgumentException>(() =>
            EventValidator.Validar(new DefinicionEvento("  ", [])));
}
