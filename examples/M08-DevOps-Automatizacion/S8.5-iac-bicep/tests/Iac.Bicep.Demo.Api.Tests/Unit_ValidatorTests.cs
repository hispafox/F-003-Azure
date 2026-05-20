using Iac.Bicep.Demo.Api.Iac;

namespace Iac.Bicep.Demo.Api.Tests;

// CAPA 1 — linter del .bicep (slides 6, 11, 19).
[Trait("Category", "Unit")]
public class Unit_ValidatorTests
{
    [Fact]
    public void Bicep_Correcto_Es_Valido()
    {
        const string bicep = """
            targetScope = 'resourceGroup'

            @secure()
            param cosmosConnectionString string

            param appName string = 'app-x'

            resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
              name: 'plan-x'
              location: resourceGroup().location
              sku: { name: 'S1' }
            }
            """;
        var r = BicepFileValidator.Validar(bicep);
        Assert.True(r.Valido, string.Join('\n', r.Errores.Select(x => x.Mensaje)));
    }

    [Fact]
    public void Secreto_Hardcoded_Con_Password_Equals_Es_Error()
    {
        const string bicep = """
            targetScope = 'resourceGroup'
            var conexion = 'Server=tcp:sql.db;Password=secret123;User=admin'
            """;
        var r = BicepFileValidator.Validar(bicep);
        Assert.False(r.Valido);
        Assert.Contains(r.Errores, e => e.Mensaje.Contains("Password="));
    }

    [Fact]
    public void Parametro_Que_Parece_Secreto_Sin_Secure_Es_Error()
    {
        const string bicep = """
            targetScope = 'resourceGroup'
            param dbPassword string
            param appName string
            """;
        var r = BicepFileValidator.Validar(bicep);
        Assert.False(r.Valido);
        Assert.Contains(r.Errores,
            e => e.Mensaje.Contains("@secure") && e.Mensaje.Contains("dbPassword"));
    }

    [Fact]
    public void Parametro_Con_Secure_Justo_Encima_Es_Valido()
    {
        const string bicep = """
            targetScope = 'resourceGroup'

            @secure()
            param apiKey string

            param appName string
            """;
        var r = BicepFileValidator.Validar(bicep);
        Assert.True(r.Valido);
    }

    [Fact]
    public void Sin_TargetScope_Es_Aviso_Slide_19()
    {
        const string bicep = """
            param appName string
            """;
        var r = BicepFileValidator.Validar(bicep);
        Assert.Contains(r.Avisos, a => a.Mensaje.Contains("targetScope"));
    }

    [Fact]
    public void Output_Que_Parece_Secreto_Es_Aviso()
    {
        const string bicep = """
            targetScope = 'resourceGroup'
            output connectionString string = 'foo'
            """;
        var r = BicepFileValidator.Validar(bicep);
        Assert.Contains(r.Avisos, a => a.Mensaje.Contains("secreto"));
    }

    [Fact]
    public void Bicep_Vacio_Lanza()
        => Assert.Throws<ArgumentException>(() =>
            BicepFileValidator.Validar("   "));
}
