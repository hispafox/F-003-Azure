namespace Plataforma.Demo.Api.Plataforma;

public sealed record EscenarioCoste(
    int Usuarios,
    bool TestPlans = false,            // solo ADO ($52/user/mes)
    bool GhasOAdvancedSecurity = false); // $49/user/mes en ambas

public sealed record CosteDesglose(
    TipoPlataforma Plataforma,
    decimal UsuariosBase,
    decimal AddonsMes,
    decimal TotalMes);

public sealed record ComparativaCoste(
    CosteDesglose Ado, CosteDesglose Github,
    TipoPlataforma MasBarata, decimal AhorroMes);

// Slides 12, 17 — coste por usuario al mes (precios 2026, EUR/USD ≈ 1).
public static class MigrationCostEstimator
{
    // Slide 12.
    public const decimal AdoUsuarioMes = 6m;          // Basic
    public const decimal AdoBasicGratisHasta = 5m;
    public const decimal AdoTestPlansAddon = 52m;     // por usuario/mes
    public const decimal GhUsuarioMes = 4m;            // Team
    public const decimal GhasUsuarioMes = 49m;        // ambas plataformas

    public static ComparativaCoste Comparar(EscenarioCoste e)
    {
        ArgumentNullException.ThrowIfNull(e);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(e.Usuarios);

        // Slide 12 — ADO Basic: primeros 5 gratis, $6 cada usuario extra.
        decimal adoUsuariosBase = Math.Max(0, e.Usuarios - AdoBasicGratisHasta) * AdoUsuarioMes;
        decimal adoAddons = (e.TestPlans ? e.Usuarios * AdoTestPlansAddon : 0m)
                          + (e.GhasOAdvancedSecurity ? e.Usuarios * GhasUsuarioMes : 0m);

        // Slide 12/17 — GitHub Team: $4/usuario desde el primero.
        decimal ghUsuariosBase = e.Usuarios * GhUsuarioMes;
        decimal ghAddons = e.GhasOAdvancedSecurity ? e.Usuarios * GhasUsuarioMes : 0m;
        // Test Plans no existe en GitHub.

        var ado = new CosteDesglose(TipoPlataforma.AzureDevOps,
            adoUsuariosBase, adoAddons, adoUsuariosBase + adoAddons);
        var gh = new CosteDesglose(TipoPlataforma.GitHubActions,
            ghUsuariosBase, ghAddons, ghUsuariosBase + ghAddons);

        var (barata, ahorro) = ado.TotalMes <= gh.TotalMes
            ? (TipoPlataforma.AzureDevOps, gh.TotalMes - ado.TotalMes)
            : (TipoPlataforma.GitHubActions, ado.TotalMes - gh.TotalMes);

        return new ComparativaCoste(ado, gh, barata, ahorro);
    }
}
