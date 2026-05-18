namespace Dr.Demo.Api.Dr;

public enum Regimen
{
    SecFinra, Finra4511, Hipaa, SarbanesOxley,
    Rgpd, FdaCfr11, PciDss, TaxEspana, LegalEspana,
}

// Anios = -1 → permanente · 0 → derecho al olvido (no retención mínima).
public sealed record RequisitoRetencion(
    int Anios, bool Worm, bool DerechoAlOlvido, string Nota);

// Slide 20 — requisitos de retención por regulación. Pura: alimenta la
// política de inmutabilidad (WORM) y la de backup vault.
public static class RetentionPolicyAdvisor
{
    public static RequisitoRetencion Requisito(Regimen regimen) => regimen switch
    {
        Regimen.SecFinra => new(6, Worm: true, DerechoAlOlvido: false,
            "SEC 17a-4: 6 años en WORM (immutable blob)"),
        Regimen.Finra4511 => new(6, false, false, "FINRA 4511: 6 años"),
        Regimen.Hipaa => new(6, false, false, "HIPAA: 6 años healthcare records"),
        Regimen.SarbanesOxley => new(7, false, false, "SOX: 7 años financial statements"),
        Regimen.Rgpd => new(0, false, DerechoAlOlvido: true,
            "RGPD: derecho de supresión; backups con retención corta (30-90 d)"),
        Regimen.FdaCfr11 => new(-1, true, false,
            "FDA 21 CFR Part 11: retención permanente"),
        Regimen.PciDss => new(1, false, false,
            "PCI DSS: el mínimo necesario (cardholder data)"),
        Regimen.TaxEspana => new(6, false, false, "Tax España: 6 años contables"),
        Regimen.LegalEspana => new(30, false, false,
            "Legal España: 30 años+ documentos de juicios"),
        _ => throw new ArgumentOutOfRangeException(nameof(regimen)),
    };

    // Slide 20 — período (en días) para la immutability-policy del
    // container (ej. SEC = 7 años ≈ 2555 días). -1 = permanente, 0 = N/A.
    public static int DiasInmutabilidad(Regimen regimen)
    {
        var r = Requisito(regimen);
        return r.Anios switch
        {
            -1 => -1,            // permanente
            0 => 0,              // derecho al olvido: sin retención mínima
            _ => r.Anios * 365,
        };
    }
}
