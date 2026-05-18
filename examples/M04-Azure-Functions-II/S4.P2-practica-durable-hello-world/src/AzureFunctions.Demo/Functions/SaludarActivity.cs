using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Demo.Functions;

// Slide 5 — la Activity hace el "trabajo real". Aquí: saludar a un nombre.
//
// El guion usa Thread.Sleep(2000) para "ver" el paralelismo en clase; lo
// dejamos FUERA del código: una activity con sleep no es testeable de
// forma determinista y Thread.Sleep nunca va en producción. El paralelismo
// se demuestra igual con el log de timestamps.
public sealed class SaludarActivity
{
    [Function(nameof(Saludar))]
    public string Saludar(
        [ActivityTrigger] string nombre,
        FunctionContext ctx)
    {
        var logger = ctx.GetLogger(nameof(SaludarActivity));
        var saludo = Construir(nombre);
        logger.LogInformation("Saludo generado: {Saludo}", saludo);
        return saludo;
    }

    // Lógica pura extraída → testeable sin runtime de Functions.
    internal static string Construir(string? nombre)
    {
        var limpio = string.IsNullOrWhiteSpace(nombre) ? "desconocido" : nombre.Trim();
        return $"¡Hola, {limpio}!";
    }
}
