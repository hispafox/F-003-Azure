namespace Storage.Demo.Api.Storage;

// Slide 6 — "las carpetas en Blob NO existen": el nombre del blob lleva
// barras y las herramientas las pintan como carpetas. Esta utilidad
// centraliza esa convención (prefijo por fecha) — lógica pura, testeable.
public static class BlobPath
{
    // Construye el nombre jerárquico: facturas → 2026/05/factura-001.pdf
    public static string ParaFecha(DateOnly fecha, string archivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archivo);
        var limpio = archivo.Trim().TrimStart('/');
        return $"{fecha:yyyy}/{fecha:MM}/{limpio}";
    }

    // El "prefijo de carpeta" para listar un mes concreto.
    public static string PrefijoMes(int anio, int mes)
    {
        if (mes is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(mes), "Mes debe estar entre 1 y 12");
        return $"{anio:0000}/{mes:00}/";
    }

    // Extrae la "carpeta" (todo menos el último segmento).
    public static string Carpeta(string blobName)
    {
        var i = blobName.LastIndexOf('/');
        return i < 0 ? string.Empty : blobName[..i];
    }
}
