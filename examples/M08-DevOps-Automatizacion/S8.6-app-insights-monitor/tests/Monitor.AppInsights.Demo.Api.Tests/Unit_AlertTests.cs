using Monitor.AppInsights.Demo.Api.Monitor;

namespace Monitor.AppInsights.Demo.Api.Tests;

// CAPA 1 — recomendador de alertas (slide 8/9/18/21).
[Trait("Category", "Unit")]
public class Unit_AlertTests
{
    [Fact]
    public void Api_Publica_Sin_Sla_Incluye_5xx_Latencia_Excepciones_Y_Query()
    {
        var reglas = AlertRecommender.Recomendar(new EscenarioAlertas(ApiPublica: true));
        Assert.Contains(reglas, r => r.Nombre == "5xx-alta-tasa");
        Assert.Contains(reglas, r => r.Nombre == "latencia-alta");
        Assert.Contains(reglas, r => r.Nombre == "excepciones-no-controladas");
        Assert.Contains(reglas, r => r.Nombre == "pedidos-fallidos-query");
    }

    [Fact]
    public void Sla_Contratado_Eleva_Severidad_5xx_A_Critico_Y_Anade_Availability()
    {
        var reglas = AlertRecommender.Recomendar(new EscenarioAlertas(
            ApiPublica: true, ProductoConSlaContratado: true));
        var r5xx = reglas.Single(r => r.Nombre == "5xx-alta-tasa");
        Assert.Equal(Severidad.Sev0Critico, r5xx.Severidad);
        Assert.Contains(reglas, r => r.Nombre == "sla-availability");
    }

    [Fact]
    public void Tiempo_Real_Critico_Eleva_Severidad_Latencia()
    {
        var reglas = AlertRecommender.Recomendar(new EscenarioAlertas(
            TiempoRealCritico: true));
        var latencia = reglas.Single(r => r.Nombre == "latencia-alta");
        Assert.Equal(Severidad.Sev1Alto, latencia.Severidad);
    }

    [Fact]
    public void Sin_Apis_Publicas_No_Anade_Query_Based_Alert()
    {
        var reglas = AlertRecommender.Recomendar(new EscenarioAlertas(ApiPublica: false));
        Assert.DoesNotContain(reglas, r => r.Nombre == "pedidos-fallidos-query");
    }

    [Fact]
    public void Canales_Anaden_Teams_Y_Pagerduty_Solo_Si_Hay_Webhook()
    {
        var reglas = AlertRecommender.Recomendar(new EscenarioAlertas(
            EmailEquipo: "x@y.z",
            WebhookTeams: "https://teams/x",
            WebhookPagerDuty: "https://pd/x"));
        var canales = reglas[0].Acciones;
        Assert.Contains(canales, c => c.Tipo == "email" && c.Destino == "x@y.z");
        Assert.Contains(canales, c => c.Tipo == "teams");
        Assert.Contains(canales, c => c.Tipo == "pagerduty");
    }

    [Fact]
    public void Canales_Sin_Webhooks_Solo_Email()
    {
        var reglas = AlertRecommender.Recomendar(new EscenarioAlertas(
            EmailEquipo: "x@y.z"));
        var canales = reglas[0].Acciones;
        Assert.Single(canales);
        Assert.Equal("email", canales[0].Tipo);
    }

    [Fact]
    public void Smart_Detection_Lista_No_Vacia()
    {
        Assert.NotEmpty(AlertRecommender.SmartDetectionRecomendada);
    }

    [Fact]
    public void Runbook_Tiene_Cinco_Pasos()
    {
        // DETECTAR / DIAGNOSTICAR / MITIGAR / RESOLVER / POST-MORTEM.
        Assert.Equal(5, AlertRecommender.Runbook.Count);
    }
}
