namespace AzureFunctions.Demo.Services;

// Slide 3 — excepciones de dominio que el ErrorClassifier mapea a TipoError.
// Tener tipos propios (en vez de strings o códigos) hace la clasificación
// trivial y testeable.

// Error transitorio: reintentando puede arreglarse (timeout, 429, 503).
public sealed class ErrorTransitorioException(string mensaje, Exception? inner = null)
    : Exception(mensaje, inner);

// Error permanente: reintentar NO ayuda (datos inválidos, 404, regla negocio).
public sealed class ErrorPermanenteException(string mensaje, Exception? inner = null)
    : Exception(mensaje, inner);

// El circuito está abierto: el servicio externo lleva demasiados fallos
// seguidos, no merece la pena intentar (slide 9).
public sealed class CircuitoAbiertoException(string mensaje)
    : Exception(mensaje);
