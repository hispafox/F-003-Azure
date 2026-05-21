namespace ProyectoIntegrador.Diseno.Demo.Api.Diseno;

public enum Componente
{
    AppServiceApi,         // slide 3/4
    AzureFunctions,
    CosmosDb,
    ServiceBus,
    EntraIdAuth,
    KeyVault,
    ManagedIdentity,
    ApplicationInsights,
    BicepIaC,
    PipelineCiCd,
}

public enum EstadoComponente { Pendiente, EnProgreso, Desplegado }

public sealed record EstadoArquitectura(
    Componente Componente,
    EstadoComponente Estado,
    string Descripcion);

public sealed record EstadoSistema(
    EstadoComponente AppService = EstadoComponente.Pendiente,
    EstadoComponente Functions = EstadoComponente.Pendiente,
    EstadoComponente Cosmos = EstadoComponente.Pendiente,
    EstadoComponente ServiceBus = EstadoComponente.Pendiente,
    EstadoComponente Entra = EstadoComponente.Pendiente,
    EstadoComponente KeyVault = EstadoComponente.Pendiente,
    EstadoComponente ManagedIdentity = EstadoComponente.Pendiente,
    EstadoComponente AppInsights = EstadoComponente.Pendiente,
    EstadoComponente Bicep = EstadoComponente.Pendiente,
    EstadoComponente Pipeline = EstadoComponente.Pendiente);

// Slide 3/4 — checklist de los 10 componentes del proyecto integrador.
// Lógica pura. Mapea EstadoSistema a una lista de `EstadoArquitectura`
// con descripción del rol de cada componente (slide 4) para que el
// alumno entienda qué falta y qué ya está hecho.
public static class ArquitecturaChecklist
{
    public static IReadOnlyList<EstadoArquitectura> Inventariar(EstadoSistema sistema)
    {
        ArgumentNullException.ThrowIfNull(sistema);

        return
        [
            new(Componente.AppServiceApi, sistema.AppService,
                "API REST con CRUD productos + crear pedidos + auth JWT (slide 4)"),
            new(Componente.AzureFunctions, sistema.Functions,
                "Procesamiento async: Change Feed Cosmos → SB → notificaciones (slide 4/8)"),
            new(Componente.CosmosDb, sistema.Cosmos,
                "Cosmos DB serverless con containers: pedidos, productos, analytics (slide 4)"),
            new(Componente.ServiceBus, sistema.ServiceBus,
                "Topic `pedido-eventos` con 2 suscripciones (notificaciones, analytics) (slide 4)"),
            new(Componente.EntraIdAuth, sistema.Entra,
                "Microsoft Entra ID con MSAL + JWT validation (slide 4/7)"),
            new(Componente.KeyVault, sistema.KeyVault,
                "Key Vault con API keys externas accesibles vía MI (slide 4)"),
            new(Componente.ManagedIdentity, sistema.ManagedIdentity,
                "Managed Identity en TODAS las conexiones (zero passwords) (slide 4)"),
            new(Componente.ApplicationInsights, sistema.AppInsights,
                "Application Insights + dashboard + 2 alertas mínimas (slide 4/10)"),
            new(Componente.BicepIaC, sistema.Bicep,
                "Bicep modular con `main.bicep` + `modules/` por dominio (slide 4/6)"),
            new(Componente.PipelineCiCd, sistema.Pipeline,
                "Pipeline ADO con Build + Test + Deploy a staging + Swap (slide 4/9)"),
        ];
    }

    public static int PorcentajeDesplegado(EstadoSistema sistema)
    {
        var inv = Inventariar(sistema);
        int total = inv.Count;
        int desplegados = inv.Count(c => c.Estado == EstadoComponente.Desplegado);
        return total == 0 ? 0 : desplegados * 100 / total;
    }
}
