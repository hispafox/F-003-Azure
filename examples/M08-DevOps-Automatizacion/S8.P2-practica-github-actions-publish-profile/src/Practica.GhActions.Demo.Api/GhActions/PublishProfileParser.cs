using System.Xml.Linq;

namespace Practica.GhActions.Demo.Api.GhActions;

public enum MetodoPublicacion { MSDeploy, Ftp, Zip, Otro }

public sealed record PerfilPublicacion(
    string Nombre,
    MetodoPublicacion Metodo,
    string PublishUrl,
    string UserName,
    string DestinationAppUrl,
    bool PasswordPresente);

public sealed record AnalisisPublishProfile(
    bool EsValido,
    IReadOnlyList<PerfilPublicacion> Perfiles,
    IReadOnlyList<string> Advertencias);

// Slide 7/17 — parser del XML que devuelve `az webapp deployment
// list-publishing-profiles ... --xml`. Lógica pura. Extrae perfiles
// MSDeploy + FTP + Zip y detecta los anti-patterns clásicos:
//   - password vacío o placeholder (`<password-larguísima>`)
//   - falta el endpoint MSDeploy (el que usa `azure/webapps-deploy@v3`)
public static class PublishProfileParser
{
    public static AnalisisPublishProfile Parsear(string xml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xml);

        XDocument doc;
        try
        {
            doc = XDocument.Parse(xml);
        }
        catch (System.Xml.XmlException ex)
        {
            return new AnalisisPublishProfile(
                EsValido: false,
                Perfiles: [],
                Advertencias: [$"XML inválido: {ex.Message}"]);
        }

        var raiz = doc.Root;
        if (raiz is null || !string.Equals(raiz.Name.LocalName, "publishData",
                StringComparison.Ordinal))
        {
            return new AnalisisPublishProfile(
                EsValido: false,
                Perfiles: [],
                Advertencias: [
                    "El XML no tiene el nodo raíz `<publishData>` esperado " +
                    "(slide 7). ¿Lo copiaste todo, incluyendo apertura y cierre?",
                ]);
        }

        var perfiles = new List<PerfilPublicacion>();
        var advertencias = new List<string>();

        foreach (var p in raiz.Elements("publishProfile"))
        {
            string nombre = (string?)p.Attribute("profileName") ?? "";
            string metodoRaw = (string?)p.Attribute("publishMethod") ?? "";
            string publishUrl = (string?)p.Attribute("publishUrl") ?? "";
            string userName = (string?)p.Attribute("userName") ?? "";
            string destApp = (string?)p.Attribute("destinationAppUrl") ?? "";
            string password = (string?)p.Attribute("userPWD") ?? "";

            var metodo = metodoRaw switch
            {
                "MSDeploy" => MetodoPublicacion.MSDeploy,
                "FTP" => MetodoPublicacion.Ftp,
                "ZipDeploy" => MetodoPublicacion.Zip,
                _ => MetodoPublicacion.Otro,
            };

            bool passwordPresente = !string.IsNullOrWhiteSpace(password)
                && !PareceUnPlaceholder(password);

            perfiles.Add(new PerfilPublicacion(
                Nombre: nombre,
                Metodo: metodo,
                PublishUrl: publishUrl,
                UserName: userName,
                DestinationAppUrl: destApp,
                PasswordPresente: passwordPresente));

            if (!passwordPresente)
                advertencias.Add(
                    $"Perfil `{nombre}` ({metodo}) sin password real: " +
                    "no servirá para deploy. Re-descarga el publish profile " +
                    "(slide 7) y refresca el secret de GitHub.");
        }

        if (perfiles.Count == 0)
            advertencias.Add("No hay perfiles `<publishProfile>` dentro de `<publishData>`.");

        if (!perfiles.Any(p => p.Metodo == MetodoPublicacion.MSDeploy))
            advertencias.Add(
                "No hay perfil MSDeploy. `azure/webapps-deploy@v3` lo necesita; " +
                "regenera el publish profile en Portal → Deployment Center (slide 17).");

        bool valido = perfiles.Count > 0
            && perfiles.Any(p => p.Metodo == MetodoPublicacion.MSDeploy)
            && perfiles.Where(p => p.Metodo == MetodoPublicacion.MSDeploy)
                       .All(p => p.PasswordPresente);

        return new AnalisisPublishProfile(valido, perfiles, advertencias);
    }

    // Slide 7/8 — passwords obviamente fake en docs/copy-paste.
    private static bool PareceUnPlaceholder(string p)
    {
        var lower = p.Trim().ToLowerInvariant();
        return lower.Contains("password-larguísima")
            || lower.Contains("password-larguisima")
            || lower.Contains("changeme")
            || lower.Contains("xxxxxxxx")
            || lower.Contains("...");
    }
}
