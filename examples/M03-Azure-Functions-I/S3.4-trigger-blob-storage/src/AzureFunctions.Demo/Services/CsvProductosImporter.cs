using System.Globalization;
using AzureFunctions.Demo.Models;

namespace AzureFunctions.Demo.Services;

public sealed class CsvProductosImporter(IProductoService productos) : ICsvProductosImporter
{
    private static readonly string[] HeaderEsperado =
        ["nombre", "categoria", "precio", "stock"];

    public ImportResultado Import(string nombreArchivo, string csvContent)
    {
        var lineas = csvContent
            .Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

        var errores = new List<FilaImport>();
        var creados = new List<string>();
        var ok = 0;

        if (lineas.Length == 0)
        {
            return new ImportResultado(
                nombreArchivo, 0, 0, 0,
                Errores: [],
                ProductosCreados: [],
                ProcesadoEn: DateTimeOffset.UtcNow);
        }

        // Parsear cabecera
        var header = SplitCsv(lineas[0]);
        if (!HeaderMatches(header))
        {
            errores.Add(new FilaImport(1,
                $"Cabecera inesperada. Recibida: [{string.Join(",", header)}]; " +
                $"esperada: [{string.Join(",", HeaderEsperado)}]"));
            return new ImportResultado(
                nombreArchivo,
                LineasTotales: lineas.Length - 1,
                LineasOk: 0,
                LineasError: lineas.Length - 1,
                Errores: errores,
                ProductosCreados: [],
                ProcesadoEn: DateTimeOffset.UtcNow);
        }

        // Procesar líneas de datos (salteando la cabecera)
        for (var i = 1; i < lineas.Length; i++)
        {
            var lineNumber = i + 1; // 1-based, contando la cabecera
            var campos = SplitCsv(lineas[i]);
            if (campos.Length != 4)
            {
                errores.Add(new FilaImport(lineNumber,
                    $"4 campos esperados, recibidos {campos.Length}"));
                continue;
            }

            var nombre = campos[0];
            var categoria = campos[1];

            if (string.IsNullOrWhiteSpace(nombre) || nombre.Length < 3)
            {
                errores.Add(new FilaImport(lineNumber, "Nombre vacio o demasiado corto"));
                continue;
            }
            if (string.IsNullOrWhiteSpace(categoria))
            {
                errores.Add(new FilaImport(lineNumber, "Categoria vacia"));
                continue;
            }
            if (!decimal.TryParse(campos[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var precio) || precio <= 0)
            {
                errores.Add(new FilaImport(lineNumber, $"Precio invalido: '{campos[2]}'"));
                continue;
            }
            if (!int.TryParse(campos[3], out var stock) || stock < 0)
            {
                errores.Add(new FilaImport(lineNumber, $"Stock invalido: '{campos[3]}'"));
                continue;
            }

            var creado = productos.Crear(new CrearProductoDto
            {
                Nombre = nombre,
                Categoria = categoria,
                Precio = precio,
                Stock = stock
            });

            creados.Add(creado.Id);
            ok++;
        }

        return new ImportResultado(
            Archivo: nombreArchivo,
            LineasTotales: lineas.Length - 1,
            LineasOk: ok,
            LineasError: errores.Count,
            Errores: errores,
            ProductosCreados: creados,
            ProcesadoEn: DateTimeOffset.UtcNow);
    }

    private static bool HeaderMatches(string[] header) =>
        header.Length == HeaderEsperado.Length &&
        header.Select(h => h.Trim().ToLowerInvariant())
              .SequenceEqual(HeaderEsperado);

    private static string[] SplitCsv(string linea) =>
        linea.Split(',').Select(c => c.Trim()).ToArray();
}
