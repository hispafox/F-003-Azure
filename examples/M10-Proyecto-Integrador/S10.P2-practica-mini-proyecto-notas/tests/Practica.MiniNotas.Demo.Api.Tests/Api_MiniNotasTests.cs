using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Practica.MiniNotas.Demo.Api.Tests;

// CAPA E2E — la app completa vía WebApplicationFactory.
[Trait("Category", "Component")]
public class Api_MiniNotasTests
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
    public async Task Preflight_Sin_Dotnet_Bloquea()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/mininotas/preflight", new
        {
            tieneAzCli = true,
            tieneCurl = true,
        });
        Assert.False((await Json(r)).GetProperty("listoParaArrancar").GetBoolean());
    }

    [Fact]
    public async Task Paso_Crear_Solucion_Ok_Devuelve_Pasa()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/mininotas/paso", new
        {
            paso = "CrearSolucion",
            comandoEjecutado = true,
            outputEsperadoVisible = true,
        });
        Assert.Equal("Pasa", (await Json(r)).GetProperty("resultado").GetString());
    }

    [Fact]
    public async Task Alcance_Auth_Devuelve_Completo()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/mininotas/alcance", new
        {
            necesitasAuthEntra = true,
        });
        Assert.Equal("Completo", (await Json(r)).GetProperty("cual").GetString());
    }

    [Fact]
    public async Task Camino_S101_Devuelve_Al_Menos_5_Pasos()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync("/mininotas/camino-s101");
        Assert.True((await Json(r)).GetArrayLength() >= 5);
    }

    [Fact]
    public async Task Plan_Compone_Preflight_Pasos_Alcance_Camino_Checklist()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/mininotas/plan", new
        {
            preflight = new
            {
                tieneDotNet8SDK = true,
                tieneAzCli = true,
                tieneCurl = true,
            },
            evidencias = new[]
            {
                new
                {
                    paso = "CrearSolucion",
                    comandoEjecutado = true,
                    outputEsperadoVisible = true,
                },
            },
            objetivo = new { quieresUnEndToEndMinimo = true },
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        Assert.True(j.GetProperty("preflight").GetProperty("listoParaArrancar").GetBoolean());
        Assert.Equal(1, j.GetProperty("pasos").GetArrayLength());
        Assert.Equal("Mini",
            j.GetProperty("alcance").GetProperty("cual").GetString());
        Assert.True(j.GetProperty("caminoHaciaS101").GetArrayLength() >= 5);
        Assert.True(j.GetProperty("checklist").GetArrayLength() >= 10);
    }
}
