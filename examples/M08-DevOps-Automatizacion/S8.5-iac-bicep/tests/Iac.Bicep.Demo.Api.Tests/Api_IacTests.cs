using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Iac.Bicep.Demo.Api.Tests;

// CAPA E2E — la app completa vía WebApplicationFactory.
[Trait("Category", "Component")]
public class Api_IacTests
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
    public async Task Comparativa_Devuelve_Filas()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync("/iac/comparativa");
        Assert.True((await Json(r)).GetArrayLength() >= 5);
    }

    [Fact]
    public async Task Recomendar_Solo_Azure_Es_Bicep()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/iac/recomendar", new
        {
            soloAzure = true,
        });
        Assert.Equal("Bicep",
            (await Json(r)).GetProperty("herramienta").GetString());
    }

    [Fact]
    public async Task Validar_Bicep_Con_Password_Equals_Es_Error()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/iac/validar", new
        {
            bicep = "targetScope = 'resourceGroup'\nvar c = 'Password=secret'",
        });
        Assert.False((await Json(r)).GetProperty("valido").GetBoolean());
    }

    [Fact]
    public async Task WhatIf_Delete_Cosmos_Es_Riesgo_Alto()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/iac/whatif/parsear", new
        {
            output = "  - /sub/x/cosmos [Microsoft.DocumentDB/databaseAccounts]",
        });
        Assert.True((await Json(r)).GetProperty("riesgoAlto").GetBoolean());
    }

    [Fact]
    public async Task Plan_Compone_Herramienta_Validacion_WhatIf_Checklist()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/iac/plan", new
        {
            escenario = new { soloAzure = true },
            bicep = "targetScope = 'resourceGroup'\n@secure()\nparam pw string\nparam x string",
            whatIfOutput = "  + /sub/x/sites/a [Microsoft.Web/sites]",
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        Assert.Equal("Bicep",
            j.GetProperty("herramienta").GetProperty("herramienta").GetString());
        Assert.True(j.GetProperty("validacionDelArchivo").GetProperty("valido").GetBoolean());
        Assert.Equal(1, j.GetProperty("whatIf").GetProperty("cambios").GetArrayLength());
        Assert.True(j.GetProperty("checklist").GetArrayLength() >= 8);
    }
}
