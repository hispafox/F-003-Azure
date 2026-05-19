namespace Security.Demo.Api.Security;

public enum Stride
{
    Spoofing, Tampering, Repudiation,
    InformationDisclosure, DenialOfService, ElevationOfPrivilege,
}

public sealed record AmenazaStride(
    char Inicial, string Nombre, string Amenaza, IReadOnlyList<string> Mitigaciones);

// Slide 20 — STRIDE aplicado a una API REST de pedidos. Las 6 categorías
// con su amenaza y mitigaciones en Azure, como datos puros testeables.
public static class StrideAnalyzer
{
    public static AmenazaStride Describir(Stride s) => s switch
    {
        Stride.Spoofing => new('S', "Spoofing",
            "Un atacante suplanta a un usuario legítimo",
            ["OAuth2 + OIDC (token de Entra ID)", "MFA en login", "Conditional Access"]),

        Stride.Tampering => new('T', "Tampering",
            "Modificar el body del request para cambiar el importe",
            ["HTTPS only (TLS 1.2+)", "Validar el schema en servidor",
             "Recalcular totales en servidor"]),

        Stride.Repudiation => new('R', "Repudiation",
            "El usuario niega haber hecho el pedido",
            ["Audit log con UserPrincipalName + timestamp",
             "Capturar IP y user agent", "Logs inmutables en Log Analytics"]),

        Stride.InformationDisclosure => new('I', "Information Disclosure",
            "Otro usuario ve tus pedidos",
            ["Row-Level Security en SQL", "[Authorize] + filtro por usuario actual"]),

        Stride.DenialOfService => new('D', "Denial of Service",
            "Bombardeo del endpoint /orders",
            ["Rate limiting (APIM o middleware)", "Quotas por usuario", "Auto-scaling"]),

        Stride.ElevationOfPrivilege => new('E', "Elevation of Privilege",
            "Un Customer accede a /admin",
            ["[Authorize(Roles=\"Admin\")]", "App Roles en App Registration",
             "Validar el claim roles del token"]),

        _ => throw new ArgumentOutOfRangeException(nameof(s)),
    };

    public static Stride PorInicial(char inicial) => char.ToUpperInvariant(inicial) switch
    {
        'S' => Stride.Spoofing,
        'T' => Stride.Tampering,
        'R' => Stride.Repudiation,
        'I' => Stride.InformationDisclosure,
        'D' => Stride.DenialOfService,
        'E' => Stride.ElevationOfPrivilege,
        _ => throw new ArgumentOutOfRangeException(nameof(inicial)),
    };
}
