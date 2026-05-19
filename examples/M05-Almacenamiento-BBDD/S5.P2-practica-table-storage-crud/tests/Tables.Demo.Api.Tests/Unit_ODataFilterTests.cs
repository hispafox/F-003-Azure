using Tables.Demo.Api.Tables;

namespace Tables.Demo.Api.Tests;

// CAPA 1 — construcción segura de filtros OData (slide 10).
[Trait("Category", "Unit")]
public class Unit_ODataFilterTests
{
    [Fact]
    public void PorParticion()
        => Assert.Equal("PartitionKey eq 'electronica'",
            ODataFilter.PorParticion("electronica"));

    [Fact]
    public void PorParticion_Escapa_Comilla_Simple()   // anti-inyección
        => Assert.Equal("PartitionKey eq 'O''Brien'",
            ODataFilter.PorParticion("O'Brien"));

    [Fact]
    public void RangoPrecio_Cultura_Invariante_Sin_Comillas()
        => Assert.Equal("precio ge 49.99 and precio le 100.5",
            ODataFilter.RangoPrecio(49.99, 100.5));

    [Fact]
    public void RangoPrecio_Min_Mayor_Que_Max_Lanza()
        => Assert.Throws<ArgumentException>(() => ODataFilter.RangoPrecio(100, 1));

    [Fact]
    public void Y_Combina_Con_And_Ignorando_Vacios()
        => Assert.Equal("PartitionKey eq 'x' and stock gt 0",
            ODataFilter.Y("PartitionKey eq 'x'", "", "stock gt 0"));

    [Fact]
    public void PorParticion_Vacia_Lanza()
        => Assert.Throws<ArgumentException>(() => ODataFilter.PorParticion(" "));
}
