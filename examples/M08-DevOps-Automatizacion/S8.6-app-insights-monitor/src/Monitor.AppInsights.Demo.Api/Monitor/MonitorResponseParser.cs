using System.Text.Json;

namespace Monitor.AppInsights.Demo.Api.Monitor;

public sealed record TablaMonitor(
    string Nombre,
    IReadOnlyList<string> Columnas,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Filas);

public sealed record RespuestaMonitor(
    IReadOnlyList<TablaMonitor> Tablas,
    int FilasTotales);

// Slide 5/13 — parser de la respuesta JSON de
// `az monitor app-insights query` (y `az monitor log-analytics query`,
// que comparte el mismo shape: `tables[].{name, columns, rows}`).
// Lógica pura: no llama a Azure.
public static class MonitorResponseParser
{
    // Acepta tanto el JSON con `tables` (CLI) como respuestas con
    // `Tables` (proxy API). Insensible a mayúsculas en los nombres.
    public static RespuestaMonitor Parsear(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        using var doc = JsonDocument.Parse(json);
        var raiz = doc.RootElement;

        if (!IntentaPropiedad(raiz, "tables", out var tablesEl))
            throw new ArgumentException(
                "Respuesta sin propiedad 'tables' — ¿es el shape de `az monitor app-insights query`?",
                nameof(json));

        var tablas = new List<TablaMonitor>();
        int total = 0;

        foreach (var t in tablesEl.EnumerateArray())
        {
            string nombre = IntentaPropiedad(t, "name", out var n)
                ? n.GetString() ?? "" : "";

            var columnasMeta = new List<(string Nombre, string Tipo)>();
            if (IntentaPropiedad(t, "columns", out var colsEl))
            {
                foreach (var c in colsEl.EnumerateArray())
                {
                    string colNombre = IntentaPropiedad(c, "name", out var cn)
                        ? cn.GetString() ?? "" : "";
                    string colTipo = IntentaPropiedad(c, "type", out var ct)
                        ? ct.GetString() ?? "" : "";
                    columnasMeta.Add((colNombre, colTipo));
                }
            }

            var filas = new List<IReadOnlyDictionary<string, object?>>();
            if (IntentaPropiedad(t, "rows", out var rowsEl))
            {
                foreach (var fila in rowsEl.EnumerateArray())
                {
                    var dict = new Dictionary<string, object?>(columnasMeta.Count);
                    int idx = 0;
                    foreach (var valor in fila.EnumerateArray())
                    {
                        if (idx >= columnasMeta.Count) break;
                        var (colNombre, colTipo) = columnasMeta[idx];
                        dict[colNombre] = Coercer(valor, colTipo);
                        idx++;
                    }
                    filas.Add(dict);
                    total++;
                }
            }

            tablas.Add(new TablaMonitor(
                nombre,
                columnasMeta.ConvertAll(c => c.Nombre),
                filas));
        }

        return new RespuestaMonitor(tablas, total);
    }

    // Slide 5 — utilidades sobre la respuesta ya parseada.
    public static IReadOnlyList<string> EndpointsLentos(RespuestaMonitor r, int topN = 5)
    {
        ArgumentNullException.ThrowIfNull(r);
        var tabla = r.Tablas.FirstOrDefault(t =>
            t.Columnas.Any(c => c.Equals("name", StringComparison.OrdinalIgnoreCase))
            && t.Columnas.Any(c => c.Contains("p95", StringComparison.OrdinalIgnoreCase)));

        if (tabla is null) return [];

        return tabla.Filas
            .Select(f =>
            {
                var nombre = ValorComoString(f, "name");
                var p95 = ValorComoString(f, "p95");
                return $"{nombre}: P95={p95}ms";
            })
            .Take(topN)
            .ToList();
    }

    private static bool IntentaPropiedad(JsonElement el, string nombre, out JsonElement valor)
    {
        if (el.ValueKind != JsonValueKind.Object) { valor = default; return false; }

        // case-insensitive sobre las propiedades.
        foreach (var prop in el.EnumerateObject())
            if (string.Equals(prop.Name, nombre, StringComparison.OrdinalIgnoreCase))
            {
                valor = prop.Value;
                return true;
            }
        valor = default;
        return false;
    }

    private static object? Coercer(JsonElement v, string tipo)
    {
        switch (v.ValueKind)
        {
            case JsonValueKind.Null: return null;
            case JsonValueKind.True: return true;
            case JsonValueKind.False: return false;
            case JsonValueKind.Number:
                bool esEntero = tipo.Contains("int", StringComparison.OrdinalIgnoreCase)
                    || tipo.Contains("long", StringComparison.OrdinalIgnoreCase);
                if (esEntero && v.TryGetInt64(out var i)) return i;
                return v.GetDouble();
            case JsonValueKind.String:
                return v.GetString();
            default:
                return v.GetRawText();
        }
    }

    private static string ValorComoString(IReadOnlyDictionary<string, object?> fila, string col)
    {
        foreach (var kv in fila)
            if (string.Equals(kv.Key, col, StringComparison.OrdinalIgnoreCase))
                return kv.Value?.ToString() ?? "";
        return "";
    }
}
