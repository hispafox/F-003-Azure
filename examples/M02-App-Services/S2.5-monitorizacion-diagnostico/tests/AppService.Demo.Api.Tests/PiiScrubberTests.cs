using AppService.Demo.Api.Telemetry;

namespace AppService.Demo.Api.Tests;

public class PiiScrubberTests
{
    [Theory]
    [InlineData("Email del usuario: pedro@example.com", PiiScrubber.EmailPlaceholder)]
    [InlineData("Contacto: a.b-c@empresa.es;", PiiScrubber.EmailPlaceholder)]
    public void Scrub_Replaces_Emails(string input, string expectedPlaceholder)
    {
        var result = PiiScrubber.Scrub(input);
        Assert.Contains(expectedPlaceholder, result);
        Assert.DoesNotContain("@example.com", result);
        Assert.DoesNotContain("@empresa.es", result);
    }

    [Theory]
    [InlineData("4111-1111-1111-1111")]
    [InlineData("4111 1111 1111 1111")]
    [InlineData("4111111111111111")]
    public void Scrub_Replaces_Credit_Card_Numbers(string number)
    {
        var input = $"Pago con tarjeta {number} aprobado";
        var result = PiiScrubber.Scrub(input);

        Assert.Contains(PiiScrubber.CreditCardPlaceholder, result);
        Assert.DoesNotContain(number, result);
    }

    [Fact]
    public void Scrub_Replaces_Bearer_Tokens()
    {
        var jwt = "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJ1c2VyMTIzIn0.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        var input = $"Authorization: Bearer {jwt}";

        var result = PiiScrubber.Scrub(input);

        Assert.Contains(PiiScrubber.TokenPlaceholder, result);
        Assert.DoesNotContain(jwt, result);
    }

    [Fact]
    public void Scrub_Leaves_Safe_Text_Untouched()
    {
        const string safe = "Pedido ORD-12345 procesado correctamente en 120 ms";
        Assert.Equal(safe, PiiScrubber.Scrub(safe));
    }

    [Fact]
    public void Scrub_Handles_Null_And_Empty()
    {
        Assert.Equal(string.Empty, PiiScrubber.Scrub(null));
        Assert.Equal(string.Empty, PiiScrubber.Scrub(string.Empty));
    }

    [Fact]
    public void Scrub_Replaces_Multiple_Pii_Types_In_Same_Input()
    {
        var input = "Cliente pedro@example.com pagó con 4111-1111-1111-1111";

        var result = PiiScrubber.Scrub(input);

        Assert.Contains(PiiScrubber.EmailPlaceholder, result);
        Assert.Contains(PiiScrubber.CreditCardPlaceholder, result);
        Assert.DoesNotContain("pedro@example.com", result);
        Assert.DoesNotContain("4111", result);
    }
}
