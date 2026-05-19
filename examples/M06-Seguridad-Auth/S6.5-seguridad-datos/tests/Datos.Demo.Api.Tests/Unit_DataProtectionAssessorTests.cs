using Datos.Demo.Api.Datos;

namespace Datos.Demo.Api.Tests;

// CAPA 1 — checklist de seguridad de datos (slide 14).
[Trait("Category", "Unit")]
public class Unit_DataProtectionAssessorTests
{
    private readonly IDataProtectionAssessor _svc = new DataProtectionAssessor();

    private static ChecklistDatos Bueno() => new(
        HttpsForzado: true,
        TlsMinimo: "1.2",
        SqlConnectionString: "Server=x;Encrypt=true;",
        StorageConnectionString: "https://stx.blob.core.windows.net",
        TdeHabilitado: true,
        SensibilidadMaxima: Sensibilidad.Confidencial,
        RegulacionExigeClaves: false,
        CorsOrigenes: ["https://app.azurewebsites.net"],
        CorsAllowCredentials: true);

    [Fact]
    public void Config_Correcta_Puntua_100()
    {
        var r = _svc.Evaluar(Bueno());
        Assert.Equal(100, r.Puntuacion);
        Assert.Empty(r.Hallazgos);
        Assert.Equal(EstrategiaCifrado.MmkAtRest, r.CifradoRecomendado);
    }

    [Fact]
    public void Detecta_Fallos_Y_Recomienda_Cifrado()
    {
        var c = Bueno() with
        {
            HttpsForzado = false,
            TlsMinimo = "1.0",
            CorsOrigenes = ["*"],
            SensibilidadMaxima = Sensibilidad.AltamenteConfidencial,
        };
        var r = _svc.Evaluar(c);

        Assert.True(r.Puntuacion < 100);
        Assert.Contains(r.Hallazgos, h => h.Contains("HTTPS"));
        Assert.Contains(r.Hallazgos, h => h.Contains("TLS"));
        Assert.Contains(r.Hallazgos, h => h.StartsWith("CORS:"));
        Assert.Equal(EstrategiaCifrado.AlwaysEncrypted, r.CifradoRecomendado);
    }

    [Fact]
    public void Null_Lanza()
        => Assert.Throws<ArgumentNullException>(() => _svc.Evaluar(null!));
}
