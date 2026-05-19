namespace Messaging.Demo.Api.Messaging;

// Slide 10 — Message Deduplication. Service Bus descarta un mensaje si
// llega otro con el MISMO MessageId dentro de la ventana de detección
// de duplicados. La ventana va de 20 s a 7 días (default 30 s).
//
// Lógica pura, determinista: dado el flujo de (MessageId, instante de
// encolado) decide cuáles entrega el broker y cuáles descarta.

public sealed record MensajeEntrante(string MessageId, TimeSpan Encolado);

public sealed record ResultadoDedup(
    IReadOnlyList<string> Entregados,
    IReadOnlyList<string> Descartados);

public static class MessageDeduplicator
{
    public static readonly TimeSpan VentanaMinima = TimeSpan.FromSeconds(20);
    public static readonly TimeSpan VentanaMaxima = TimeSpan.FromDays(7);
    public static readonly TimeSpan VentanaPorDefecto = TimeSpan.FromSeconds(30);

    public static bool VentanaValida(TimeSpan ventana) =>
        ventana >= VentanaMinima && ventana <= VentanaMaxima;

    // Recorre los mensajes EN ORDEN de encolado. Un MessageId se
    // descarta si el último con ese id se entregó hace <= `ventana`.
    public static ResultadoDedup Procesar(
        TimeSpan ventana, IEnumerable<MensajeEntrante> mensajes)
    {
        ArgumentNullException.ThrowIfNull(mensajes);
        if (!VentanaValida(ventana))
            throw new ArgumentOutOfRangeException(nameof(ventana),
                $"La ventana debe estar entre {VentanaMinima} y {VentanaMaxima}.");

        var entregados = new List<string>();
        var descartados = new List<string>();
        var ultimoEntregado = new Dictionary<string, TimeSpan>(StringComparer.Ordinal);

        foreach (var m in mensajes.OrderBy(x => x.Encolado))
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(m.MessageId);

            if (ultimoEntregado.TryGetValue(m.MessageId, out var previo) &&
                m.Encolado - previo <= ventana)
            {
                descartados.Add(m.MessageId);          // duplicado en ventana
                continue;
            }

            entregados.Add(m.MessageId);
            ultimoEntregado[m.MessageId] = m.Encolado;  // referencia nueva
        }

        return new ResultadoDedup(entregados, descartados);
    }
}
