using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Practica.GhActions.Demo.Api.Tests;

// CAPA E2E — la app completa vía WebApplicationFactory.
[Trait("Category", "Component")]
public class Api_GhActionsTests
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
    public async Task Profile_Parsear_MSDeploy_Es_Valido()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/ghactions/profile/parsear", new
        {
            xml = "<publishData><publishProfile profileName=\"x\" "
                  + "publishMethod=\"MSDeploy\" publishUrl=\"x\" userName=\"$x\" "
                  + "userPWD=\"realpwd\" destinationAppUrl=\"https://x\" /></publishData>",
        });
        Assert.True((await Json(r)).GetProperty("esValido").GetBoolean());
    }

    [Fact]
    public async Task Profile_Con_Placeholder_No_Es_Valido()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/ghactions/profile/parsear", new
        {
            xml = "<publishData><publishProfile profileName=\"x\" "
                  + "publishMethod=\"MSDeploy\" publishUrl=\"x\" userName=\"$x\" "
                  + "userPWD=\"<password-larguisima>\" destinationAppUrl=\"https://x\" /></publishData>",
        });
        Assert.False((await Json(r)).GetProperty("esValido").GetBoolean());
    }

    [Fact]
    public async Task Workflow_Con_Tests_Devuelve_Dos_Jobs()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/ghactions/workflow", new
        {
            appName = "webapp-pedro",
            dotnetVersion = "10.0.x",
            incluirTests = true,
        });
        Assert.Equal(2, (await Json(r)).GetProperty("jobs").GetArrayLength());
    }

    [Fact]
    public async Task Auth_Side_Project_Devuelve_PublishProfile()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/ghactions/auth/recomendar", new
        {
            sideProjectPersonal = true,
        });
        Assert.Equal("PublishProfile",
            (await Json(r)).GetProperty("metodo").GetString());
    }

    [Fact]
    public async Task Auth_Produccion_Con_Entra_Devuelve_Oidc()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/ghactions/auth/recomendar", new
        {
            sideProjectPersonal = false,
            controlaEntraId = true,
            proyectoEnProduccion = true,
        });
        Assert.Equal("Oidc",
            (await Json(r)).GetProperty("metodo").GetString());
    }

    [Fact]
    public async Task Plan_Compone_Profile_Workflow_Recomendacion_Checklist()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/ghactions/plan", new
        {
            publishProfileXml = "<publishData><publishProfile profileName=\"x\" "
                + "publishMethod=\"MSDeploy\" publishUrl=\"x\" userName=\"$x\" "
                + "userPWD=\"realpwd\" destinationAppUrl=\"https://x\" /></publishData>",
            opciones = new { appName = "webapp-pedro", incluirTests = true },
            escenario = new { sideProjectPersonal = true },
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        Assert.True(j.GetProperty("profile").GetProperty("esValido").GetBoolean());
        Assert.True(j.GetProperty("workflow").GetProperty("jobs").GetArrayLength() >= 2);
        Assert.Equal("PublishProfile",
            j.GetProperty("recomendacion").GetProperty("metodo").GetString());
        Assert.True(j.GetProperty("checklist").GetArrayLength() >= 10);
    }
}
