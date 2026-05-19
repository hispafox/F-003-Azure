using Tables.Demo.Api.Tables;

namespace Tables.Demo.Api.Tests;

// CAPA 1 — restricciones de PartitionKey/RowKey + patrón timestamp
// invertido (slides 5, 14).
[Trait("Category", "Unit")]
public class Unit_TableKeysTests
{
    [Theory]
    [InlineData("electronica", true)]
    [InlineData("laptop001", true)]
    [InlineData("cat/egoria", false)]     // '/'
    [InlineData("a\\b", false)]            // '\'
    [InlineData("tag#1", false)]           // '#'
    [InlineData("q?x", false)]             // '?'
    [InlineData("", false)]
    [InlineData(null, false)]
    public void EsValida(string? key, bool esperado)
        => Assert.Equal(esperado, TableKeys.EsValida(key));

    [Fact]
    public void EsValida_False_Con_Control_Char()
        => Assert.False(TableKeys.EsValida("a\tb"));

    [Fact]
    public void Sanitizar_Sustituye_Prohibidos_Por_Guion()
        => Assert.Equal("cat-egoria-1", TableKeys.Sanitizar("cat/egoria#1"));

    [Fact]
    public void RowKeyTimestampInvertido_Mas_Reciente_Ordena_Antes()
    {
        var antiguo = TableKeys.RowKeyTimestampInvertido(
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var reciente = TableKeys.RowKeyTimestampInvertido(
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
        // Orden alfabético de RowKey: el más reciente debe ser "menor".
        Assert.True(string.CompareOrdinal(reciente, antiguo) < 0);
        Assert.Equal(19, reciente.Length);
    }

    [Fact]
    public void Sanitizar_Null_Lanza()
        => Assert.Throws<ArgumentNullException>(() => TableKeys.Sanitizar(null!));
}
