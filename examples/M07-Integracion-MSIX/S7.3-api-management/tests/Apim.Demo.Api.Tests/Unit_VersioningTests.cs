using Apim.Demo.Api.Apim;

namespace Apim.Demo.Api.Tests;

// CAPA 1 — versionado de APIs (slide 7).
[Trait("Category", "Unit")]
public class Unit_VersioningTests
{
    private static readonly IReadOnlySet<string> Validas =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "v1", "v2" };

    [Fact]
    public void Segment_Toma_El_Primer_Segmento()
    {
        var r = ApimVersioningResolver.Resolver(
            EsquemaVersionado.Segment, "productos", "/v2/productos/123", Validas);
        Assert.Equal("v2", r.Version);
        Assert.Equal("/v2/productos", r.RutaGateway);
    }

    [Fact]
    public void Query_Usa_El_Valor()
    {
        var r = ApimVersioningResolver.Resolver(
            EsquemaVersionado.Query, "productos", "v1", Validas);
        Assert.Equal("v1", r.Version);
        Assert.Equal("/productos?api-version=v1", r.RutaGateway);
    }

    [Fact]
    public void Header_Usa_El_Valor()
    {
        var r = ApimVersioningResolver.Resolver(
            EsquemaVersionado.Header, "productos", "v2", Validas);
        Assert.Equal("v2", r.Version);
        Assert.Contains("Api-Version: v2", r.RutaGateway);
    }

    [Fact]
    public void Version_Inexistente_Lanza()
        => Assert.Throws<ArgumentException>(() =>
            ApimVersioningResolver.Resolver(
                EsquemaVersionado.Query, "productos", "v9", Validas));

    [Fact]
    public void Segment_Sin_Version_Lanza()
        => Assert.Throws<FormatException>(() =>
            ApimVersioningResolver.Resolver(
                EsquemaVersionado.Segment, "productos", "/", Validas));

    [Fact]
    public void Recomendado_Es_Segment()
        => Assert.Equal(EsquemaVersionado.Segment,
            ApimVersioningResolver.Recomendado);
}
