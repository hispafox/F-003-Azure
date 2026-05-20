namespace Practica.Pipeline.Demo.Api.Pipeline;

public enum DecisionSmoke { Continuar, RollbackNecesario }

public sealed record ResultadoSmoke(
    DecisionSmoke Decision,
    IReadOnlyList<string> Razones,
    IReadOnlyList<string> Detalles);

public sealed record MedidasSmoke(
    int HttpCode,
    double LatenciaMediaSegundos,
    double ErrorRatePorcentaje);

public sealed record UmbralesSmoke(
    int HttpCodeEsperado = 200,
    double LatenciaMaxSegundos = 2.0,
    double ErrorRateMaxPorcentaje = 1.0);

// Slide 5/6/10 — evalúa el resultado del smoke test post-deploy y
// decide si seguir o disparar rollback automático. Lógica pura.
public static class SmokeTestEvaluator
{
    public static ResultadoSmoke Evaluar(
        MedidasSmoke medidas, UmbralesSmoke? umbrales = null)
    {
        ArgumentNullException.ThrowIfNull(medidas);
        umbrales ??= new UmbralesSmoke();

        var razones = new List<string>();
        var detalles = new List<string>
        {
            $"HTTP {medidas.HttpCode} (esperado {umbrales.HttpCodeEsperado})",
            $"Latencia {medidas.LatenciaMediaSegundos:0.###}s (max {umbrales.LatenciaMaxSegundos}s)",
            $"Error rate {medidas.ErrorRatePorcentaje:0.##}% (max {umbrales.ErrorRateMaxPorcentaje}%)",
        };

        if (medidas.HttpCode != umbrales.HttpCodeEsperado)
            razones.Add(
                $"Health check devolvió {medidas.HttpCode}, esperado {umbrales.HttpCodeEsperado} (slide 5).");

        if (medidas.LatenciaMediaSegundos > umbrales.LatenciaMaxSegundos)
            razones.Add(
                $"Latencia media {medidas.LatenciaMediaSegundos:0.###}s supera el umbral " +
                $"{umbrales.LatenciaMaxSegundos}s (slide 10).");

        if (medidas.ErrorRatePorcentaje > umbrales.ErrorRateMaxPorcentaje)
            razones.Add(
                $"Error rate {medidas.ErrorRatePorcentaje:0.##}% supera el umbral " +
                $"{umbrales.ErrorRateMaxPorcentaje}% (slide 10).");

        var decision = razones.Count == 0
            ? DecisionSmoke.Continuar
            : DecisionSmoke.RollbackNecesario;

        if (decision == DecisionSmoke.Continuar)
            razones.Add("Smoke test pasa: continuar con swap a producción.");

        return new ResultadoSmoke(decision, razones, detalles);
    }
}
