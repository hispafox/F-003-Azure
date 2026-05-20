namespace Practica.Pipeline.Demo.Api.Pipeline;

public enum HallazgoNivel { Ok, Aviso, Bloqueante }

public sealed record Hallazgo(HallazgoNivel Nivel, string Comprobacion, string Mensaje);

public sealed record ReportePreflight(
    bool ListoParaArrancar,
    IReadOnlyList<Hallazgo> Hallazgos);

public sealed record EscenarioPreflight(
    bool TieneOrgADO = false,
    bool TieneRepoConPushAccess = false,
    bool TieneSuscripcionAzure = false,
    bool EsAdminProyectoADO = false,
    bool EsOwnerOUserAccessAdmin = false,
    bool PlanS1OSuperior = false,
    bool SlotStagingExiste = false,
    bool TieneServiceConnectionOidc = false,
    bool TieneAppRegistration = false,
    bool TieneAzCliInstalado = false);

// Slide 3 — validación pre-flight antes de empezar la práctica. Sin
// esto, la práctica falla a mitad y el alumno pierde 30 minutos.
// Lógica pura: convierte un escenario booleano en hallazgos
// clasificados (OK / Aviso / Bloqueante).
public static class PreflightChecker
{
    public static ReportePreflight Comprobar(EscenarioPreflight e)
    {
        ArgumentNullException.ThrowIfNull(e);

        var hallazgos = new List<Hallazgo>
        {
            Check(e.TieneOrgADO,
                "Azure DevOps Organization disponible",
                "Sin org no se puede crear el pipeline (slide 3).",
                HallazgoNivel.Bloqueante),

            Check(e.TieneRepoConPushAccess,
                "Repo con push access",
                "Sin push no dispara el pipeline (`git push origin main`).",
                HallazgoNivel.Bloqueante),

            Check(e.TieneSuscripcionAzure,
                "Suscripción Azure activa",
                "Sin suscripción no hay slot al que desplegar.",
                HallazgoNivel.Bloqueante),

            Check(e.EsAdminProyectoADO,
                "Project Administrator en ADO",
                "Requerido para crear Service Connections (slide 3).",
                HallazgoNivel.Bloqueante),

            Check(e.EsOwnerOUserAccessAdmin,
                "Owner o User Access Administrator en Azure",
                "Requerido para crear App Registration + role assignment (slide 3).",
                HallazgoNivel.Bloqueante),

            Check(e.PlanS1OSuperior,
                "App Service Plan S1 o superior",
                "Los deployment slots requieren Standard tier (S1+). " +
                "Free/Shared/Basic no tienen slot staging (slide 3).",
                HallazgoNivel.Bloqueante),

            Check(e.SlotStagingExiste,
                "Slot 'staging' existe en la App",
                "Sin slot staging el pipeline no puede desplegar (slide 5). " +
                "Crea el slot primero — práctica M02-S2.P.",
                HallazgoNivel.Bloqueante),

            Check(e.TieneAzCliInstalado,
                "Azure CLI (`az`) disponible local",
                "Necesario para los pasos de pre-setup (App Registration, RBAC).",
                HallazgoNivel.Aviso),

            Check(e.TieneAppRegistration,
                "App Registration en Entra ID creada",
                "Crear con `az ad app create --display-name ado-pipeline-curso` (slide 3).",
                HallazgoNivel.Aviso),

            Check(e.TieneServiceConnectionOidc,
                "Service Connection con Workload Identity federada",
                "Recomendado sin secrets. Crear en Project Settings → Service connections (slide 3/17).",
                HallazgoNivel.Aviso),
        };

        bool listo = !hallazgos.Any(h => h.Nivel == HallazgoNivel.Bloqueante);
        return new ReportePreflight(listo, hallazgos);
    }

    private static Hallazgo Check(bool ok, string nombre, string mensaje, HallazgoNivel nivelFallo)
        => ok
            ? new Hallazgo(HallazgoNivel.Ok, nombre, "OK.")
            : new Hallazgo(nivelFallo, nombre, mensaje);
}
