namespace Iac.Bicep.Demo.Api.Iac;

public enum CambioWhatIf { Create, Modify, Delete, NoChange, Deploy, Ignore }

public sealed record CambioRecurso(CambioWhatIf Tipo, string Recurso, string Tipo_Azure);

public sealed record ReporteWhatIf(
    IReadOnlyList<CambioRecurso> Cambios,
    bool RiesgoAlto,
    IReadOnlyList<string> Avisos);

// Slide 5/14 — parser del output de `az deployment group what-if`.
// La salida marca cada línea con un símbolo: `+`, `~`, `-`, `=`, etc.
// Lógica pura.
public static class WhatIfDiffParser
{
    // Slide 14 — borrar un recurso STATEFUL es alto riesgo (pierdes datos).
    private static readonly string[] TiposStateful =
    [
        "Microsoft.Storage/storageAccounts",
        "Microsoft.DocumentDB/databaseAccounts",
        "Microsoft.Sql/servers",
        "Microsoft.Sql/servers/databases",
        "Microsoft.KeyVault/vaults",
        "Microsoft.ServiceBus/namespaces",
    ];

    public static ReporteWhatIf Parsear(string output)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(output);

        var cambios = new List<CambioRecurso>();
        var avisos = new List<string>();

        foreach (var raw in output.Replace("\r\n", "\n").Split('\n'))
        {
            var linea = raw.TrimEnd();
            if (linea.Length == 0) continue;

            var trimmed = linea.TrimStart();
            char marcador = trimmed.Length > 0 ? trimmed[0] : ' ';
            var tipo = marcador switch
            {
                '+' => CambioWhatIf.Create,
                '~' => CambioWhatIf.Modify,
                '-' => CambioWhatIf.Delete,
                '=' => CambioWhatIf.NoChange,
                _ => CambioWhatIf.Ignore,
            };
            if (tipo == CambioWhatIf.Ignore) continue;

            // El formato típico es: `+ <resourceId>` o
            // `~ <resourceId> [<resourceType>]`.
            // resourceId termina típicamente en /<nombre>; resourceType
            // sigue el formato Microsoft.Web/sites.
            string sinMarcador = trimmed[1..].Trim();
            string recurso = sinMarcador;
            string tipoAzure = "";

            int corcheteAbre = sinMarcador.LastIndexOf('[');
            int corcheteCierra = sinMarcador.LastIndexOf(']');
            if (corcheteAbre >= 0 && corcheteCierra > corcheteAbre)
            {
                tipoAzure = sinMarcador.Substring(corcheteAbre + 1,
                    corcheteCierra - corcheteAbre - 1).Trim();
                recurso = sinMarcador[..corcheteAbre].Trim();
            }
            else if (recurso.Contains('/'))
            {
                // Inferir el tipo desde el resourceId (microsoft. ...).
                int idx = recurso.IndexOf("Microsoft.", StringComparison.Ordinal);
                if (idx >= 0)
                {
                    string cola = recurso[idx..];
                    int barra = cola.IndexOf('/', "Microsoft.".Length);
                    if (barra > 0)
                    {
                        int sig = cola.IndexOf('/', barra + 1);
                        tipoAzure = sig > 0 ? cola[..sig] : cola;
                    }
                }
            }

            cambios.Add(new CambioRecurso(tipo, recurso, tipoAzure));

            if (tipo == CambioWhatIf.Delete && TiposStateful.Any(s =>
                    tipoAzure.StartsWith(s, StringComparison.OrdinalIgnoreCase)))
                avisos.Add($"⚠ DELETE de recurso STATEFUL: {recurso} ({tipoAzure}). " +
                    "Revisa que tienes backup antes de aplicar (slide 14).");
        }

        bool riesgoAlto = avisos.Count > 0;
        return new ReporteWhatIf(cambios, riesgoAlto, avisos);
    }
}
