using AzureFunctions.Demo.Models;

namespace AzureFunctions.Demo.Services;

public interface IInformeService
{
    // Slide 12 — Patrón de idempotencia: si ya hay informe para esa fecha,
    // devuelve el existente; si no, lo genera y lo persiste. La operación es
    // thread-safe y atómica.
    (bool yaExistia, Informe informe) GenerarSiNoExiste(DateOnly fecha);

    Informe? GetByFecha(DateOnly fecha);
    IReadOnlyList<Informe> Listar();
}
