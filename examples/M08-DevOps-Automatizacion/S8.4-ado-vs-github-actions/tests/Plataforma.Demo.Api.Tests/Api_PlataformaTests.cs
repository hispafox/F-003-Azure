using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Plataforma.Demo.Api.Tests;

// CAPA E2E — la app completa vía WebApplicationFactory.
[Trait("Category", "Component")]
public class Api_PlataformaTests
{
    private static async Task<JsonElement> Json(HttpResponseMessage r) =>
        JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;

    [Fact]
    public async Task Health_Ok()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Fact]
    public async Task Elegir_Ado_Mas_Boards_Es_AzureDevOps()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/plataforma/elegir", new
        {
            yaUsasAdo = true,
            necesitaBoardsCompletos = true,
            personas = 8,
        });
        Assert.Equal("AzureDevOps",
            (await Json(r)).GetProperty("plataforma").GetString());
    }

    [Fact]
    public async Task Elegir_Open_Source_Es_GitHubActions()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/plataforma/elegir", new
        {
            openSource = true,
            quiereDependabotCodeQL = true,
            personas = 6,
        });
        Assert.Equal("GitHubActions",
            (await Json(r)).GetProperty("plataforma").GetString());
    }

    [Fact]
    public async Task Elegir_Ambas_Senales_Es_Hybrid()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/plataforma/elegir", new
        {
            yaUsasAdo = true,
            necesitaBoardsCompletos = true,
            quiereDependabotCodeQL = true,
            personas = 8,
        });
        Assert.Equal("Hybrid",
            (await Json(r)).GetProperty("plataforma").GetString());
    }

    [Fact]
    public async Task Equivalencias_Devuelve_Al_Menos_15()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync("/plataforma/equivalencias");
        Assert.True((await Json(r)).GetArrayLength() >= 15);
    }

    [Fact]
    public async Task Equivalencia_Concepto_No_Encontrado_Es_404()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync(
            "/plataforma/equivalencia?concepto=ConceptoInexistente");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task Coste_5_Usuarios_Ado_Es_Cero()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/plataforma/coste", new
        {
            usuarios = 5,
        });
        var j = await Json(r);
        Assert.Equal(0, j.GetProperty("ado").GetProperty("totalMes").GetDecimal());
        Assert.Equal("AzureDevOps", j.GetProperty("masBarata").GetString());
    }

    [Fact]
    public async Task Plan_Compone_Recomendacion_Coste_Equivalencias_Checklist()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/plataforma/plan", new
        {
            escenario = new
            {
                yaUsasAdo = true,
                necesitaBoardsCompletos = true,
                personas = 8,
            },
            coste = new { usuarios = 8 },
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        Assert.Equal("AzureDevOps",
            j.GetProperty("recomendacion").GetProperty("plataforma").GetString());
        Assert.True(j.GetProperty("equivalenciasClave").GetArrayLength() >= 5);
        Assert.True(j.GetProperty("checklist").GetArrayLength() >= 8);
    }
}
