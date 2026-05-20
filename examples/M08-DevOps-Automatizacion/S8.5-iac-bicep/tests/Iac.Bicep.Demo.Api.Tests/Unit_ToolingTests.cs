using Iac.Bicep.Demo.Api.Iac;

namespace Iac.Bicep.Demo.Api.Tests;

// CAPA 1 — comparativa + recomendación de herramienta (slide 3).
[Trait("Category", "Unit")]
public class Unit_ToolingTests
{
    [Fact]
    public void Solo_Azure_Es_Bicep()
        => Assert.Equal(HerramientaIac.Bicep,
            ToolingComparison.Recomendar(new EscenarioIac(SoloAzure: true))
                .Herramienta);

    [Fact]
    public void Multi_Cloud_Es_Terraform()
        => Assert.Equal(HerramientaIac.Terraform,
            ToolingComparison.Recomendar(new EscenarioIac(MultiCloud: true))
                .Herramienta);

    [Fact]
    public void Equipo_Ya_En_Terraform_Mantiene_Terraform()
        => Assert.Equal(HerramientaIac.Terraform,
            ToolingComparison.Recomendar(new EscenarioIac(EquipoYaUsaTerraform: true))
                .Herramienta);

    [Fact]
    public void Tabla_Comparativa_Tiene_Filas_De_Formato_Y_State()
    {
        Assert.Contains(ToolingComparison.Comparativa, f => f.Feature == "Formato");
        Assert.Contains(ToolingComparison.Comparativa, f => f.Feature.Contains("State"));
    }
}
