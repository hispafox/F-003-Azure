using System.Net;
using System.Text.Json;
using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;

namespace AzureFunctions.Demo.Tests;

public class ErrorClassifierTests
{
    private readonly ErrorClassifier _sut = new();

    [Fact]
    public void Json_Malformado_Es_Permanente()
    {
        Exception ex;
        try { JsonSerializer.Deserialize<Pedido>("{ broken"); ex = null!; }
        catch (JsonException e) { ex = e; }

        Assert.Equal(TipoError.Permanente, _sut.Clasificar(ex));
    }

    public static TheoryData<Exception> Permanentes => new()
    {
        new ErrorPermanenteException("regla negocio"),
        new ArgumentException("arg inválido"),
        new InvalidOperationException("estado inválido"),
    };

    [Theory]
    [MemberData(nameof(Permanentes))]
    public void Errores_De_Negocio_Y_Validacion_Son_Permanentes(Exception ex)
        => Assert.Equal(TipoError.Permanente, _sut.Clasificar(ex));

    public static TheoryData<Exception> Transitorios => new()
    {
        new ErrorTransitorioException("timeout BD"),
        new TimeoutException("timeout"),
        new TaskCanceledException("cancelado por timeout"),
        new CircuitoAbiertoException("circuito abierto"),
    };

    [Theory]
    [MemberData(nameof(Transitorios))]
    public void Errores_Reintenable_Son_Transitorios(Exception ex)
        => Assert.Equal(TipoError.Transitorio, _sut.Clasificar(ex));

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]   // 429
    [InlineData(HttpStatusCode.ServiceUnavailable)] // 503
    [InlineData(HttpStatusCode.GatewayTimeout)]     // 504
    [InlineData(HttpStatusCode.RequestTimeout)]     // 408
    public void Http_5xx_Y_429_Son_Transitorios(HttpStatusCode code)
    {
        var ex = new HttpRequestException("fallo", null, code);
        Assert.Equal(TipoError.Transitorio, _sut.Clasificar(ex));
    }

    [Fact]
    public void Http_Sin_StatusCode_Es_Transitorio()
    {
        // Fallo de conexión (no llegó a haber respuesta) → transitorio.
        var ex = new HttpRequestException("connection refused");
        Assert.Equal(TipoError.Transitorio, _sut.Clasificar(ex));
    }

    [Fact]
    public void Http_404_No_Es_Transitorio_Es_Desconocido()
    {
        // 404 no está en la lista de transitorios; cae a Desconocido
        // (preferimos log critical + reintento a descartarlo a ciegas).
        var ex = new HttpRequestException("not found", null, HttpStatusCode.NotFound);
        Assert.Equal(TipoError.Desconocido, _sut.Clasificar(ex));
    }

    [Fact]
    public void Excepcion_No_Catalogada_Es_Desconocida()
    {
        Assert.Equal(TipoError.Desconocido, _sut.Clasificar(new Exception("???")));
    }
}
