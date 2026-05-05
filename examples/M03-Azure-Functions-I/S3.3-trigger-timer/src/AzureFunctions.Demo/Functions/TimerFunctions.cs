using AzureFunctions.Demo.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Demo.Functions;

public sealed class TimerFunctions(
    IProductoService productos,
    IInformeService informes,
    ILogger<TimerFunctions> logger)
{
    // Slides 4, 5 — NCRONTAB con 6 campos. Cada minuto. CRON puesto bajo, pero
    // %CleanupCron% permite cambiarlo desde App Settings sin redeploy (slide 5).
    [Function(nameof(CleanupCadaMinuto))]
    public void CleanupCadaMinuto(
        [TimerTrigger("%CleanupCron%")] TimerInfo timer)
    {
        // Slide 7 — IsPastDue: el runtime detectó que se perdieron ejecuciones.
        if (timer.IsPastDue)
        {
            logger.LogWarning(
                "CleanupCadaMinuto past-due. Ultima: {Last}, proxima: {Next}",
                timer.ScheduleStatus?.Last,
                timer.ScheduleStatus?.Next);
        }

        var stats = productos.GetStats();
        logger.LogInformation(
            "CleanupCadaMinuto stats Total={Total} SinStock={SinStock} Valor={Valor:0.00}",
            stats.Total, stats.SinStock, stats.ValorTotalStock);
    }

    // Slide 5 — diariamente a las 06:00 (UTC por defecto, ver slide 6 para
    // WEBSITE_TIME_ZONE). Slide 12 — idempotente vía GetOrAdd: si la función
    // se reintenta o se dispara dos veces, no duplica el informe.
    [Function(nameof(InformeDiario))]
    public void InformeDiario(
        [TimerTrigger("0 0 6 * * *")] TimerInfo timer)
    {
        // Reportamos sobre el día anterior — ese es el patrón típico de un
        // informe nocturno: "ayer cerramos con X".
        var fecha = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1));

        var (yaExistia, informe) = informes.GenerarSiNoExiste(fecha);

        if (yaExistia)
        {
            logger.LogInformation(
                "InformeDiario {Id} ya existia (idempotencia OK)",
                informe.Id);
            return;
        }

        logger.LogInformation(
            "InformeDiario {Id} generado: {Total} productos, valor stock {Valor:0.00}",
            informe.Id, informe.TotalProductos, informe.ValorTotalStock);
    }
}
