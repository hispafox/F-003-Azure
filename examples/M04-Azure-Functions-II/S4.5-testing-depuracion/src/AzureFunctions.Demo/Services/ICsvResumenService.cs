using AzureFunctions.Demo.Models;

namespace AzureFunctions.Demo.Services;

// Slide 11 — la lógica del Blob trigger extraída a un servicio puro.
// El test pasa el contenido como string; no necesita Azurite ni Storage.
public interface ICsvResumenService
{
    ResumenCsv Procesar(string contenido, string nombreArchivo);
}

public sealed class CsvResumenService : ICsvResumenService
{
    public ResumenCsv Procesar(string contenido, string nombreArchivo)
    {
        if (string.IsNullOrWhiteSpace(contenido))
            throw new ArgumentException("El CSV está vacío", nameof(contenido));

        var lineas = contenido
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.TrimEnd('\r'))
            .ToList();

        var columnas = lineas[0].Split(',').Select(c => c.Trim()).ToArray();
        var filasDatos = lineas.Count - 1;

        return new ResumenCsv(nombreArchivo, filasDatos, columnas);
    }
}
