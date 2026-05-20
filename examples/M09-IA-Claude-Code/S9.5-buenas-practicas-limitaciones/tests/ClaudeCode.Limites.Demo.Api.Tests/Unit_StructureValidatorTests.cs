using ClaudeCode.Limites.Demo.Api.Limites;

namespace ClaudeCode.Limites.Demo.Api.Tests;

// CAPA 1 — validador del template de 7 secciones (slide 12).
[Trait("Category", "Unit")]
public class Unit_StructureValidatorTests
{
    private const string PromptCompleto = """
        CONTEXTO: API REST .NET 10 con MediatR y EF Core.
        OBJETIVO: crea endpoint POST /api/orders.
        CONSTRAINTS: no añadir dependencias, mantener Order aggregate.
        INPUT: src/Domain/Orders/Order.cs.
        OUTPUT: archivos en src/Application/Orders/Commands/ + tests.
        EJEMPLO: como en UpdateOrderHandler.cs.
        Criterio éxito: tests verdes y sin warnings.
        """;

    [Fact]
    public void Prompt_Con_Las_7_Secciones_Llega_A_100()
    {
        var v = PromptStructureValidator.Validar(PromptCompleto);
        Assert.Equal(100, v.Puntuacion);
        Assert.Equal(7, v.SeccionesDetectadas.Count);
        Assert.Empty(v.SeccionesFaltantes);
    }

    [Fact]
    public void Prompt_Vago_Tiene_Puntuacion_Baja_Y_Muchas_Sugerencias()
    {
        var v = PromptStructureValidator.Validar("hazme algo");
        Assert.True(v.Puntuacion < 30);
        Assert.True(v.SeccionesFaltantes.Count >= 5);
    }

    [Fact]
    public void Detecta_Contexto_Y_Objetivo_Aunque_Falten_Los_Demas()
    {
        var v = PromptStructureValidator.Validar(
            "Proyecto .NET 10. Crea un servicio de pedidos.");
        Assert.Contains(SeccionPrompt.Contexto, v.SeccionesDetectadas);
        Assert.Contains(SeccionPrompt.Objetivo, v.SeccionesDetectadas);
    }

    [Fact]
    public void Sugerencias_Cubren_Las_Secciones_Faltantes()
    {
        var v = PromptStructureValidator.Validar("hazme un endpoint");
        // Hay al menos 1 sugerencia por cada faltante.
        Assert.Equal(v.SeccionesFaltantes.Count, v.Sugerencias.Count);
    }

    [Fact]
    public void Detecta_Constraints_Por_No_Romper()
    {
        var v = PromptStructureValidator.Validar(
            "Mantén la API pública. No romper tests existentes.");
        Assert.Contains(SeccionPrompt.Constraints, v.SeccionesDetectadas);
    }

    [Fact]
    public void Detecta_Dod_Por_Tests_Verdes()
    {
        var v = PromptStructureValidator.Validar("Criterio éxito: tests verdes.");
        Assert.Contains(SeccionPrompt.DefinitionOfDone, v.SeccionesDetectadas);
    }

    [Fact]
    public void Prompt_Vacio_Lanza_ArgumentException()
    {
        Assert.Throws<ArgumentException>(() => PromptStructureValidator.Validar(" "));
    }
}
