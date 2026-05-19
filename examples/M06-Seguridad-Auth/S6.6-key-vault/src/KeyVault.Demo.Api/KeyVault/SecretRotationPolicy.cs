namespace KeyVault.Demo.Api.KeyVault;

public enum EstadoSecreto { Vigente, ProximoAExpirar, Expirado }

public sealed record EvaluacionRotacion(
    EstadoSecreto Estado, int DiasRestantes, bool DebeRotar);

// Slides 8-9 — rotación automática: 30 días antes de expirar, Event
// Grid emite SecretNearExpiry. Esta función pura decide el estado y si
// toca rotar (con reloj inyectable para testear).
public static class SecretRotationPolicy
{
    public const int VentanaDiasPorDefecto = 30;

    public static EvaluacionRotacion Evaluar(
        DateTimeOffset expira, DateTimeOffset ahora, int ventanaDias = VentanaDiasPorDefecto)
    {
        if (ventanaDias < 0)
            throw new ArgumentOutOfRangeException(nameof(ventanaDias));

        var dias = (int)Math.Floor((expira - ahora).TotalDays);

        if (dias < 0)
            return new EvaluacionRotacion(EstadoSecreto.Expirado, dias, true);

        if (dias <= ventanaDias)
            return new EvaluacionRotacion(EstadoSecreto.ProximoAExpirar, dias, true);

        return new EvaluacionRotacion(EstadoSecreto.Vigente, dias, false);
    }
}
