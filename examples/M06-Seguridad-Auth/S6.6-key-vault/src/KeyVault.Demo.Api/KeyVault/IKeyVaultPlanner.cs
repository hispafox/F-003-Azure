namespace KeyVault.Demo.Api.KeyVault;

public sealed record PlanKeyVault(
    string Que,
    string Destino,
    bool VaAKeyVault,
    string RolMinimo,
    string? AppSettingReference,   // si va a KV como Secret
    string Nota);

// Compone KeyVaultItemAdvisor + KeyVaultReference en un plan de
// almacenamiento de un secreto/cert/clave. Servicio inyectable (seam
// para el test de contenedor).
public interface IKeyVaultPlanner
{
    PlanKeyVault Planificar(
        QueGuardar que, AccesoKv acceso, string vaultName, string itemName);
}

public sealed class KeyVaultPlanner : IKeyVaultPlanner
{
    public PlanKeyVault Planificar(
        QueGuardar que, AccesoKv acceso, string vaultName, string itemName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(vaultName);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemName);

        var destino = KeyVaultItemAdvisor.Donde(que);
        var rol = KeyVaultItemAdvisor.RolMinimo(destino, acceso);
        var vaKv = destino != Destino.ManagedIdentity;

        // Solo los Secrets se referencian vía @Microsoft.KeyVault(...)
        // en App Settings (slide 6).
        var reference = destino == Destino.KeyVaultSecret
            ? KeyVaultReference.Construir(vaultName, itemName)
            : null;

        var nota = destino switch
        {
            Destino.ManagedIdentity =>
                "Azure-a-Azure: usa Managed Identity, NO Key Vault (slide 2).",
            Destino.KeyVaultSecret =>
                "Secret en KV + App Setting como Key Vault Reference (slide 6).",
            Destino.KeyVaultCertificate =>
                "Certificado en KV con auto-renovación y alertas de expiración (slide 10).",
            _ => "Clave criptográfica en KV (CMK / firma de tokens, slide 11).",
        };

        return new PlanKeyVault(
            que.ToString(), destino.ToString(), vaKv, rol, reference, nota);
    }
}
