using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Monitor.AppInsights.Demo.Api.Tests;

// CAPA E2E — la app completa vía WebApplicationFactory.
[Trait("Category", "Component")]
public class Api_MonitorTests
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
    public async Task Kql_P95_Devuelve_Texto_Con_Percentiles()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/monitor/kql/p95", new
        {
            ventana = "24h",
            minimo = 100,
        });
        var texto = (await Json(r)).GetProperty("texto").GetString();
        Assert.Contains("percentile(duration, 95)", texto, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Kql_Correlacion_Incluye_OperationId()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/monitor/kql/correlacion", new
        {
            operationId = "trace-42",
        });
        var texto = (await Json(r)).GetProperty("texto").GetString();
        Assert.Contains("trace-42", texto, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Alertas_Sla_Eleva_Severidad_Y_Anade_Availability()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/monitor/alertas/recomendar", new
        {
            apiPublica = true,
            productoConSlaContratado = true,
            emailEquipo = "oncall@empresa.com",
        });

        var j = await Json(r);
        Assert.Equal(JsonValueKind.Array, j.ValueKind);
        bool tieneSla = false;
        foreach (var regla in j.EnumerateArray())
            if (regla.GetProperty("nombre").GetString() == "sla-availability")
                tieneSla = true;
        Assert.True(tieneSla);
    }

    [Fact]
    public async Task Respuesta_Parsear_Devuelve_Tablas_Y_Filas()
    {
        await using var f = new WebApplicationFactory<Program>();
        const string payload = "{\"tables\":[{\"name\":\"T\"," +
            "\"columns\":[{\"name\":\"x\",\"type\":\"string\"}]," +
            "\"rows\":[[\"a\"],[\"b\"]]}]}";
        var r = await f.CreateClient().PostAsJsonAsync("/monitor/respuesta/parsear", new
        {
            json = payload,
        });
        var j = await Json(r);
        Assert.Equal(2, j.GetProperty("filasTotales").GetInt32());
        Assert.Equal(1, j.GetProperty("tablas").GetArrayLength());
    }

    [Fact]
    public async Task Plan_Compone_Queries_Alertas_Smart_Runbook_Checklist()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/monitor/plan", new
        {
            escenario = new
            {
                apiPublica = true,
                productoConSlaContratado = true,
                emailEquipo = "x@y.z",
            },
            ventana = "24h",
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        Assert.True(j.GetProperty("queriesCanonicas").GetArrayLength() >= 4);
        Assert.True(j.GetProperty("alertas").GetArrayLength() >= 4);
        Assert.True(j.GetProperty("smartDetection").GetArrayLength() >= 3);
        Assert.Equal(5, j.GetProperty("runbook").GetArrayLength());
        Assert.True(j.GetProperty("checklist").GetArrayLength() >= 8);
    }

    [Fact]
    public async Task Smart_Detection_Devuelve_Lista()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync("/monitor/alertas/smart-detection");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.True((await Json(r)).GetArrayLength() >= 3);
    }

    [Fact]
    public async Task Runbook_Devuelve_5_Pasos()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync("/monitor/alertas/runbook");
        Assert.Equal(5, (await Json(r)).GetArrayLength());
    }
}
