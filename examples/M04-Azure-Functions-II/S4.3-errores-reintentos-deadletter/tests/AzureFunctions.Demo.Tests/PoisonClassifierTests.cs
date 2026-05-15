using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;

namespace AzureFunctions.Demo.Tests;

public class PoisonClassifierTests
{
    private readonly PoisonClassifier _sut = new();

    [Fact]
    public void Json_En_Descripcion_Se_Descarta()
    {
        var a = _sut.Clasificar("MalformedJson", "JsonException: unexpected token");
        Assert.Equal(PoisonAction.Discard, a);
    }

    [Fact]
    public void Timeout_En_Descripcion_Reintenta_Con_Aviso()
    {
        var a = _sut.Clasificar("ProcessingError", "Operation timeout after 30s");
        Assert.Equal(PoisonAction.NotifyAndRetry, a);
    }

    [Fact]
    public void MaxDeliveryCount_Va_A_Cuarentena()
    {
        var a = _sut.Clasificar("MaxDeliveryCountExceeded", "");
        Assert.Equal(PoisonAction.Quarantine, a);
    }

    [Fact]
    public void BusinessRule_Va_A_Cuarentena()
    {
        var a = _sut.Clasificar("BusinessRule", "Cliente bloqueado");
        Assert.Equal(PoisonAction.Quarantine, a);
    }

    [Fact]
    public void Desconocido_Va_A_Cuarentena_Por_Seguridad()
    {
        // Nunca descartamos a ciegas: lo desconocido se cuarentena.
        var a = _sut.Clasificar("AlgoRaro", "sin pistas");
        Assert.Equal(PoisonAction.Quarantine, a);
    }

    [Fact]
    public void Null_Reason_Y_Descripcion_No_Rompen()
    {
        var a = _sut.Clasificar(null, null);
        Assert.Equal(PoisonAction.Quarantine, a);
    }
}
