using System.Globalization;

namespace Tables.Demo.Api.Tables;

// Slide 10 — los filtros OData de Table Storage. Construirlos a mano es
// propenso a inyección si no se escapan las comillas simples. Esta clase
// pura genera filtros seguros (escapa ' → '').
public static class ODataFilter
{
    // En OData, una comilla simple dentro de un string literal se duplica.
    public static string Escapar(string valor)
    {
        ArgumentNullException.ThrowIfNull(valor);
        return valor.Replace("'", "''");
    }

    public static string PorParticion(string categoria)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(categoria);
        return $"PartitionKey eq '{Escapar(categoria)}'";
    }

    // Números: cultura invariante, sin comillas (slide 10).
    public static string RangoPrecio(double min, double max)
    {
        if (min > max) throw new ArgumentException("min > max");
        var lo = min.ToString(CultureInfo.InvariantCulture);
        var hi = max.ToString(CultureInfo.InvariantCulture);
        return $"precio ge {lo} and precio le {hi}";
    }

    public static string Y(params string[] condiciones)
    {
        ArgumentNullException.ThrowIfNull(condiciones);
        var partes = condiciones.Where(c => !string.IsNullOrWhiteSpace(c));
        return string.Join(" and ", partes);
    }
}
