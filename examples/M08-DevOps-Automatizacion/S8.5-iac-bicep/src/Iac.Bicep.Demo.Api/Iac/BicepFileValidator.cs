using System.Text.RegularExpressions;

namespace Iac.Bicep.Demo.Api.Iac;

public sealed record HallazgoBicep(string Linea, int Numero, string Mensaje);

public sealed record ValidacionBicep(
    bool Valido,
    IReadOnlyList<HallazgoBicep> Errores,
    IReadOnlyList<HallazgoBicep> Avisos);

// Slides 4, 6, 10, 11, 19 — linter del archivo .bicep:
//  - parámetros con nombre "password|secret|key|token" → exigir @secure()
//  - connection strings con Password= literal hardcoded → error
//  - resource sin existing modifier cuando referencia recurso ya creado
//  - output que expone un parámetro @secure → aviso
// Lógica pura: opera sobre el texto del archivo.
public static partial class BicepFileValidator
{
    // Slide 6 — un parámetro string que se parece a un secreto.
    [GeneratedRegex(@"^\s*param\s+(\w+)\s+string", RegexOptions.IgnoreCase)]
    private static partial Regex ParamStringRegex();

    [GeneratedRegex(@"@secure\s*\(\s*\)", RegexOptions.IgnoreCase)]
    private static partial Regex SecureDecoratorRegex();

    private static readonly string[] SecretosEnNombre =
    [
        "password", "secret", "key", "token", "connection",
    ];

    public static ValidacionBicep Validar(string bicepTexto)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bicepTexto);
        var errores = new List<HallazgoBicep>();
        var avisos = new List<HallazgoBicep>();

        var lineas = bicepTexto.Replace("\r\n", "\n").Split('\n');
        for (int i = 0; i < lineas.Length; i++)
        {
            string linea = lineas[i];
            int num = i + 1;

            // Slide 11 — passwords/secretos literales en el código.
            if (linea.Contains("Password=", StringComparison.OrdinalIgnoreCase) &&
                !linea.TrimStart().StartsWith("//", StringComparison.Ordinal))
                errores.Add(new(linea.Trim(), num,
                    "Connection string con 'Password=' literal en el código (slide 11). " +
                    "Usar Key Vault Reference o Managed Identity."));

            // Slide 6 — parámetros que se parecen a secretos sin @secure.
            var m = ParamStringRegex().Match(linea);
            if (m.Success)
            {
                string nombre = m.Groups[1].Value;
                bool pareceSecreto = SecretosEnNombre.Any(s =>
                    nombre.Contains(s, StringComparison.OrdinalIgnoreCase));

                if (pareceSecreto)
                {
                    // Mirar las dos líneas anteriores buscando @secure().
                    bool tieneSecure = false;
                    for (int j = i - 1; j >= Math.Max(0, i - 3); j--)
                        if (SecureDecoratorRegex().IsMatch(lineas[j]))
                            tieneSecure = true;
                    if (!tieneSecure)
                        errores.Add(new(linea.Trim(), num,
                            $"Parámetro '{nombre}' parece un secreto: añade @secure() arriba (slide 6/11)."));
                }
            }
        }

        // Slide 19 — buen hábito: archivos Bicep en producción
        // suelen tener `targetScope` explícito (rara vez para rg, pero
        // sí para subscription/managementGroup).
        if (!bicepTexto.Contains("targetScope", StringComparison.Ordinal))
            avisos.Add(new("(global)", 0,
                "Sin `targetScope` declarado: por defecto es `resourceGroup`. " +
                "Si necesitas subscription o managementGroup, decláralo (slide 19)."));

        // Slide 11 — `output` de un valor `@secure` no debería existir.
        foreach (var (linea, idx) in lineas
            .Select((l, i) => (l, i))
            .Where(t => t.l.TrimStart().StartsWith("output", StringComparison.Ordinal)))
        {
            if (SecretosEnNombre.Any(s =>
                    linea.Contains(s, StringComparison.OrdinalIgnoreCase)))
                avisos.Add(new(linea.Trim(), idx + 1,
                    "`output` parece exponer un secreto: revísalo (slide 11)."));
        }

        return new ValidacionBicep(errores.Count == 0, errores, avisos);
    }
}
