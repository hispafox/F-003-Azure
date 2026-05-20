using Practica.GhActions.Demo.Api.GhActions;

namespace Practica.GhActions.Demo.Api.Tests;

// CAPA 1 — parser del XML publish profile (slide 7/17).
[Trait("Category", "Unit")]
public class Unit_ProfileParserTests
{
    private const string ValidoConPasswords = """
        <publishData>
          <publishProfile profileName="app - Web Deploy"
                          publishMethod="MSDeploy"
                          publishUrl="app.scm.azurewebsites.net:443"
                          userName="$app"
                          userPWD="abc123XYZsecreto"
                          destinationAppUrl="https://app.azurewebsites.net" />
          <publishProfile profileName="app - FTP"
                          publishMethod="FTP"
                          publishUrl="ftps://waws-prod-..."
                          userName="$app"
                          userPWD="abc123XYZsecreto"
                          destinationAppUrl="https://app.azurewebsites.net" />
        </publishData>
        """;

    [Fact]
    public void Parsea_Dos_Perfiles_MSDeploy_Y_Ftp()
    {
        var r = PublishProfileParser.Parsear(ValidoConPasswords);
        Assert.True(r.EsValido);
        Assert.Equal(2, r.Perfiles.Count);
        Assert.Contains(r.Perfiles, p => p.Metodo == MetodoPublicacion.MSDeploy);
        Assert.Contains(r.Perfiles, p => p.Metodo == MetodoPublicacion.Ftp);
    }

    [Fact]
    public void Extrae_UserName_PublishUrl_Y_DestinationAppUrl()
    {
        var r = PublishProfileParser.Parsear(ValidoConPasswords);
        var msd = r.Perfiles.Single(p => p.Metodo == MetodoPublicacion.MSDeploy);
        Assert.Equal("$app", msd.UserName);
        Assert.Equal("app.scm.azurewebsites.net:443", msd.PublishUrl);
        Assert.Equal("https://app.azurewebsites.net", msd.DestinationAppUrl);
        Assert.True(msd.PasswordPresente);
    }

    [Fact]
    public void Placeholder_Password_Marca_PasswordPresente_False()
    {
        // Nota: el "<" literal en atributo XML rompe el parseo, así que
        // el caso real es cuando el alumno deja "changeme" o similar tras
        // copiar el ejemplo de la doc.
        const string conPlaceholder = """
            <publishData>
              <publishProfile profileName="x" publishMethod="MSDeploy"
                              publishUrl="x" userName="$x"
                              userPWD="changeme"
                              destinationAppUrl="https://x" />
            </publishData>
            """;
        var r = PublishProfileParser.Parsear(conPlaceholder);
        Assert.False(r.EsValido);
        Assert.False(r.Perfiles[0].PasswordPresente);
        Assert.Contains(r.Advertencias, a => a.Contains("sin password real", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Password_Vacia_Marca_PasswordPresente_False()
    {
        const string sinPassword = """
            <publishData>
              <publishProfile profileName="x" publishMethod="MSDeploy"
                              publishUrl="x" userName="$x" userPWD=""
                              destinationAppUrl="https://x" />
            </publishData>
            """;
        var r = PublishProfileParser.Parsear(sinPassword);
        Assert.False(r.EsValido);
        Assert.False(r.Perfiles[0].PasswordPresente);
    }

    [Fact]
    public void Sin_Perfil_MSDeploy_Avisa_Y_No_Es_Valido()
    {
        const string soloFtp = """
            <publishData>
              <publishProfile profileName="x" publishMethod="FTP"
                              publishUrl="ftps://x" userName="$x"
                              userPWD="real" destinationAppUrl="https://x" />
            </publishData>
            """;
        var r = PublishProfileParser.Parsear(soloFtp);
        Assert.False(r.EsValido);
        Assert.Contains(r.Advertencias, a => a.Contains("MSDeploy", StringComparison.Ordinal));
    }

    [Fact]
    public void Xml_Invalido_Devuelve_Reporte_Con_Error()
    {
        var r = PublishProfileParser.Parsear("<publishData><roto>");
        Assert.False(r.EsValido);
        Assert.Empty(r.Perfiles);
        Assert.Contains(r.Advertencias, a => a.Contains("XML inválido", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Sin_Nodo_publishData_Es_Invalido()
    {
        var r = PublishProfileParser.Parsear("<otraCosa />");
        Assert.False(r.EsValido);
        Assert.Contains(r.Advertencias, a => a.Contains("publishData", StringComparison.Ordinal));
    }

    [Fact]
    public void Xml_Vacio_Lanza_ArgumentException()
    {
        Assert.Throws<ArgumentException>(() => PublishProfileParser.Parsear(" "));
    }
}
