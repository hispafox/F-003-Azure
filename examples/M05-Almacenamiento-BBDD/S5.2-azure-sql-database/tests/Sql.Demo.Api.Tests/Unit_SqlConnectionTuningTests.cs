using Microsoft.Data.SqlClient;
using Sql.Demo.Api.Sql;

namespace Sql.Demo.Api.Tests;

// CAPA 1 — afinado puro de la connection string (slides 6, 10, 20).
// Verificamos parseando el resultado (robusto frente a la forma canónica
// que elija SqlConnectionStringBuilder).
[Trait("Category", "Unit")]
public class Unit_SqlConnectionTuningTests
{
    private const string Base =
        "Server=tcp:srv.database.windows.net,1433;Database=db;User Id=app;Password=secreto";

    [Fact]
    public void Afinar_Aplica_Pooling_Y_Encrypt()
    {
        var b = new SqlConnectionStringBuilder(SqlConnectionTuning.Afinar(Base));

        Assert.Equal(SqlConnectionTuning.MaxPoolSize, b.MaxPoolSize);
        Assert.Equal(SqlConnectionTuning.MinPoolSize, b.MinPoolSize);
        Assert.True(b.Pooling);
        Assert.Equal(SqlConnectionEncryptOption.Mandatory, b.Encrypt); // forzado (slide 6)
    }

    [Fact]
    public void Afinar_Respeta_Encrypt_Explicito_De_Testcontainers()
    {
        // El cs de Testcontainers trae Encrypt/TrustServerCertificate:
        // no lo pisamos (cert self-signed del contenedor).
        var contenedor = Base + ";Encrypt=False;TrustServerCertificate=True";
        var b = new SqlConnectionStringBuilder(SqlConnectionTuning.Afinar(contenedor));

        Assert.Equal(SqlConnectionEncryptOption.Optional, b.Encrypt);
        Assert.True(b.TrustServerCertificate);
    }

    [Fact]
    public void Afinar_Es_Idempotente()
        => Assert.Equal(
            SqlConnectionTuning.Afinar(Base),
            SqlConnectionTuning.Afinar(SqlConnectionTuning.Afinar(Base)));

    [Fact]
    public void Afinar_Vacio_Lanza()
        => Assert.Throws<ArgumentException>(() => SqlConnectionTuning.Afinar("  "));

    [Fact]
    public void UsaManagedIdentity_True_Con_Entra_Sin_Password()
    {
        var cs = "Server=tcp:srv.database.windows.net,1433;Database=db;" +
                 "Authentication=Active Directory Default;Encrypt=True";
        Assert.True(SqlConnectionTuning.UsaManagedIdentity(cs));
    }

    [Fact]
    public void UsaManagedIdentity_False_Con_Sql_Auth()
        => Assert.False(SqlConnectionTuning.UsaManagedIdentity(Base));
}
