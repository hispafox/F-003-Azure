namespace Entra.Demo.Api.Entra;

public sealed record DecisionAutorizacion(bool Autorizado, string Motivo);

// Slide 19 — App Roles: autorización basada en el claim `roles` del
// token ([Authorize(Roles="Admin")]). Servicio inyectable (seam para el
// test de contenedor); la lógica es determinista y testeable.
public interface IAppRolesAuthorizer
{
    DecisionAutorizacion Autorizar(
        IEnumerable<string> rolesDelToken, string rolRequerido);

    bool AutorizaAlguno(
        IEnumerable<string> rolesDelToken, params string[] rolesRequeridos);
}

public sealed class AppRolesAuthorizer : IAppRolesAuthorizer
{
    public DecisionAutorizacion Autorizar(
        IEnumerable<string> rolesDelToken, string rolRequerido)
    {
        ArgumentNullException.ThrowIfNull(rolesDelToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(rolRequerido);

        // Comparación case-insensitive contra el claim `roles` (slide 18/19).
        var tiene = rolesDelToken.Any(r =>
            string.Equals(r?.Trim(), rolRequerido.Trim(),
                StringComparison.OrdinalIgnoreCase));

        return tiene
            ? new DecisionAutorizacion(true, $"El token incluye el rol '{rolRequerido}'")
            : new DecisionAutorizacion(false,
                $"403 Forbidden: falta el rol '{rolRequerido}' en el token");
    }

    public bool AutorizaAlguno(
        IEnumerable<string> rolesDelToken, params string[] rolesRequeridos)
    {
        ArgumentNullException.ThrowIfNull(rolesDelToken);
        var set = rolesDelToken
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return rolesRequeridos.Any(set.Contains);
    }
}
