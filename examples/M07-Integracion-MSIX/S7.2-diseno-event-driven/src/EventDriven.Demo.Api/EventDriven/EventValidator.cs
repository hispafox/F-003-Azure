namespace EventDriven.Demo.Api.EventDriven;

// Un evento del catálogo: nombre del tipo + campos del payload.
public sealed record DefinicionEvento(string Tipo, IReadOnlyList<string> Campos);

public sealed record ResultadoValidacion(
    bool Valido, IReadOnlyList<string> Problemas);

// Slide 20 — anti-patterns de eventos. Reglas puras que detectan los
// errores que rompen un sistema event-driven en producción.
public static class EventValidator
{
    public const int MaxSaltosCadena = 4;   // slide 20.1

    // Verbos imperativos: si el "evento" empieza por uno, es un COMANDO
    // disfrazado (slide 20.2 — el evento describe algo que YA pasó).
    private static readonly string[] VerbosComando =
    [
        "enviar", "crear", "procesar", "cobrar", "reservar", "borrar",
        "eliminar", "actualizar", "cancelar", "validar", "generar",
        "send", "create", "process", "charge", "reserve", "delete",
        "update", "cancel", "validate", "generate", "notify", "notificar",
    ];

    // Fragmentos de campos con datos sensibles (slide 20.3).
    private static readonly string[] CamposSensibles =
    [
        "password", "contrasena", "contraseña", "secret", "token",
        "apikey", "api_key", "cvv", "tarjeta", "creditcard", "iban",
        "clientsecret",
    ];

    public static ResultadoValidacion Validar(DefinicionEvento evento)
    {
        ArgumentNullException.ThrowIfNull(evento);
        ArgumentException.ThrowIfNullOrWhiteSpace(evento.Tipo);

        var problemas = new List<string>();
        string tipo = evento.Tipo.Trim();

        if (VerbosComando.Any(v =>
                tipo.StartsWith(v, StringComparison.OrdinalIgnoreCase)))
            problemas.Add($"'{tipo}' parece un COMANDO, no un evento: nombra en pasado lo que ocurrió (slide 20.2).");

        foreach (var campo in evento.Campos ?? [])
        {
            if (CamposSensibles.Any(s =>
                    campo.Contains(s, StringComparison.OrdinalIgnoreCase)))
                problemas.Add($"Campo '{campo}' expone datos sensibles: usa solo una referencia/ID (slide 20.3).");
        }

        bool tieneVersion = (evento.Campos ?? []).Any(c =>
            c.Equals("version", StringComparison.OrdinalIgnoreCase) ||
            c.Equals("schemaVersion", StringComparison.OrdinalIgnoreCase));
        if (!tieneVersion)
            problemas.Add("El evento no está versionado: añade 'version' para no romper consumidores (slide 20.4).");

        return new ResultadoValidacion(problemas.Count == 0, problemas);
    }

    // Slide 20.1 — una cadena de eventos no debe pasar de 3-4 saltos;
    // más allá, usar un Orchestrator (Durable Functions).
    public static ResultadoValidacion ValidarLongitudCadena(int saltos)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(saltos);
        return saltos > MaxSaltosCadena
            ? new ResultadoValidacion(false,
                [$"Cadena de {saltos} saltos > {MaxSaltosCadena}: se pierde la trazabilidad; usa un Orchestrator (slide 20.1)."])
            : new ResultadoValidacion(true, []);
    }
}
