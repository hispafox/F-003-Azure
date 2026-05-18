using AzureFunctions.Demo.Functions;

namespace AzureFunctions.Demo.Tests;

// La lógica de la activity está extraída a Construir() → testeable sin
// runtime de Functions (patrón S4.5).
public class SaludarActivityTests
{
    [Theory]
    [InlineData("Ana", "¡Hola, Ana!")]
    [InlineData("  Luis  ", "¡Hola, Luis!")]   // trim
    [InlineData("", "¡Hola, desconocido!")]    // vacío → fallback
    [InlineData("   ", "¡Hola, desconocido!")] // solo espacios → fallback
    [InlineData(null, "¡Hola, desconocido!")]  // null → fallback
    public void Construir_Saluda_Y_Normaliza(string? nombre, string esperado)
        => Assert.Equal(esperado, SaludarActivity.Construir(nombre));
}
