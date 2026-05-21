namespace Practica.MiniNotas.Demo.Api.MiniNotas;

public enum NivelPreflight { Ok, Aviso, Bloqueante }

public sealed record HallazgoPreflight(NivelPreflight Nivel, string Comprobacion, string Mensaje);

public sealed record ReportePreflight(
    bool ListoParaArrancar,
    IReadOnlyList<HallazgoPreflight> Hallazgos);

public sealed record EscenarioPreflight(
    bool TieneDotNet8SDK = false,
    bool TieneAzCli = false,
    bool TieneCurl = false,
    bool TieneJq = false,
    bool TieneGit = false,
    bool HizoM01 = false,        // Cloud Shell + RG
    bool HizoM02 = false,        // Web App + deploy zip
    bool HizoM05 = false);       // Persistencia (Cosmos o Table)

// Slide 3 — preflight de la mini-práctica. Las herramientas (.NET,
// az, curl, jq, git) son bloqueantes; el conocimiento previo M01/M02/M05
// se marca como aviso porque el alumno puede ir consultando los slides
// si se atasca, no impide arrancar. Lógica pura.
public static class MiniNotasPreflight
{
    public static ReportePreflight Comprobar(EscenarioPreflight e)
    {
        ArgumentNullException.ThrowIfNull(e);

        var hallazgos = new List<HallazgoPreflight>
        {
            Check(e.TieneDotNet8SDK,
                ".NET 8 SDK instalado",
                "Necesario para `dotnet new webapi` y `dotnet test` (slide 3). " +
                "Instala desde https://dotnet.microsoft.com.",
                NivelPreflight.Bloqueante),

            Check(e.TieneAzCli,
                "Azure CLI (`az`) instalada y autenticada",
                "Imprescindible para `az group create` + `az webapp deploy` (slide 3).",
                NivelPreflight.Bloqueante),

            Check(e.TieneCurl,
                "`curl` disponible",
                "Necesario para los smoke tests post-deploy (slide 3).",
                NivelPreflight.Bloqueante),

            Check(e.TieneJq,
                "`jq` disponible",
                "Útil para parsear las respuestas JSON de la API. " +
                "En Windows: `winget install stedolan.jq`.",
                NivelPreflight.Aviso),

            Check(e.TieneGit,
                "`git` instalado",
                "Recomendado para versionar el mini-proyecto (slide 5).",
                NivelPreflight.Aviso),

            Check(e.HizoM01,
                "Conocimiento previo M01 (Cloud Shell + Resource Groups)",
                "La práctica asume que sabes crear un Resource Group y " +
                "manejar Cloud Shell. Si te atascas, repasa M01-S1.P / S1.P2.",
                NivelPreflight.Aviso),

            Check(e.HizoM02,
                "Conocimiento previo M02 (Web App + deploy zip)",
                "Asume saber crear una Web App y desplegar con zip. " +
                "Si te atascas, repasa M02-S2.P / S2.P2.",
                NivelPreflight.Aviso),

            Check(e.HizoM05,
                "Conocimiento previo M05 (persistencia: Cosmos o Table)",
                "Esta práctica usa Table Storage. Si no recuerdas el modelo " +
                "PartitionKey/RowKey, repasa M05-S5.P2.",
                NivelPreflight.Aviso),
        };

        bool listo = !hallazgos.Any(h => h.Nivel == NivelPreflight.Bloqueante);
        return new ReportePreflight(listo, hallazgos);
    }

    private static HallazgoPreflight Check(bool ok, string nombre, string mensaje, NivelPreflight nivelFallo)
        => ok
            ? new HallazgoPreflight(NivelPreflight.Ok, nombre, "OK.")
            : new HallazgoPreflight(nivelFallo, nombre, mensaje);
}
