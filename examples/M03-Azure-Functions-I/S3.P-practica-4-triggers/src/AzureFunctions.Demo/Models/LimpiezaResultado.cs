namespace AzureFunctions.Demo.Models;

// Resultado de una pasada del timer de limpieza. Sirve para tests: la
// función pública del Timer no devuelve nada (Functions runtime no usa el
// return value), pero el handler interno sí lo devuelve para validar.
public sealed record LimpiezaResultado(
    DateTimeOffset Ejecutado,
    int RegistrosEliminados,
    bool LlegoTarde);
