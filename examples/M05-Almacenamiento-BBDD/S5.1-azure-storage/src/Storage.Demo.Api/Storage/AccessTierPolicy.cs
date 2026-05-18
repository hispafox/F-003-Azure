namespace Storage.Demo.Api.Storage;

public enum AccessTier { Hot, Cool, Cold, Archive }

// Slide 5 — la lógica de lifecycle management ("mover a Cool a los 30
// días, a Archive a los 180, borrar al año") expresada como función pura.
// En Azure esto lo hace una management policy JSON; aquí lo modelamos
// para poder testear la decisión y para el endpoint de "sugerencia".
public static class AccessTierPolicy
{
    public const int DiasACool = 30;
    public const int DiasAArchive = 180;
    public const int DiasABorrado = 365;

    public static AccessTier Sugerir(int diasDesdeModificacion)
    {
        if (diasDesdeModificacion < 0)
            throw new ArgumentOutOfRangeException(nameof(diasDesdeModificacion));

        return diasDesdeModificacion switch
        {
            < DiasACool => AccessTier.Hot,
            < DiasAArchive => AccessTier.Cool,
            < DiasABorrado => AccessTier.Archive,
            _ => AccessTier.Archive, // ≥365: candidato a borrado por la policy
        };
    }

    public static bool DebeBorrarse(int diasDesdeModificacion)
        => diasDesdeModificacion >= DiasABorrado;
}
