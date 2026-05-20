using Iac.Bicep.Demo.Api.Iac;

namespace Iac.Bicep.Demo.Api.Tests;

// CAPA 1 — parser del output de what-if (slides 5, 14).
[Trait("Category", "Unit")]
public class Unit_WhatIfTests
{
    [Fact]
    public void Parsea_Create_Modify_Delete_NoChange()
    {
        const string output = """
              + /subscriptions/x/.../Microsoft.Web/sites/app1 [Microsoft.Web/sites]
              ~ /subscriptions/x/.../Microsoft.Web/serverfarms/p1 [Microsoft.Web/serverfarms]
              - /subscriptions/x/.../Microsoft.Web/sites/old [Microsoft.Web/sites]
              = /subscriptions/x/.../Microsoft.Storage/storageAccounts/st [Microsoft.Storage/storageAccounts]
            """;
        var r = WhatIfDiffParser.Parsear(output);
        Assert.Equal(4, r.Cambios.Count);
        Assert.Contains(r.Cambios, c => c.Tipo == CambioWhatIf.Create);
        Assert.Contains(r.Cambios, c => c.Tipo == CambioWhatIf.Modify);
        Assert.Contains(r.Cambios, c => c.Tipo == CambioWhatIf.Delete);
        Assert.Contains(r.Cambios, c => c.Tipo == CambioWhatIf.NoChange);
    }

    [Fact]
    public void Delete_De_Cosmos_Es_Riesgo_Alto_Slide_14()
    {
        const string output = """
              - /subscriptions/x/.../Microsoft.DocumentDB/databaseAccounts/cosmos-ventas [Microsoft.DocumentDB/databaseAccounts]
              + /subscriptions/x/.../Microsoft.Web/sites/app [Microsoft.Web/sites]
            """;
        var r = WhatIfDiffParser.Parsear(output);
        Assert.True(r.RiesgoAlto);
        Assert.Contains(r.Avisos, a => a.Contains("STATEFUL"));
    }

    [Fact]
    public void Delete_De_Storage_Tambien_Es_Riesgo_Alto()
    {
        const string output = "  - /subscriptions/x/storageAccounts/st [Microsoft.Storage/storageAccounts]";
        var r = WhatIfDiffParser.Parsear(output);
        Assert.True(r.RiesgoAlto);
    }

    [Fact]
    public void Delete_De_App_Service_Solo_No_Es_Riesgo_Alto()
    {
        const string output = "  - /subscriptions/x/sites/app [Microsoft.Web/sites]";
        var r = WhatIfDiffParser.Parsear(output);
        Assert.False(r.RiesgoAlto);
    }

    [Fact]
    public void Ignora_Lineas_Sin_Marcador()
    {
        const string output = """
            Note: The result may contain false positives.

              + /sites/x [Microsoft.Web/sites]
            """;
        var r = WhatIfDiffParser.Parsear(output);
        Assert.Single(r.Cambios);
    }

    [Fact]
    public void Vacio_Lanza()
        => Assert.Throws<ArgumentException>(() =>
            WhatIfDiffParser.Parsear("  "));
}
