namespace KeyVault.Demo.Api.KeyVault;

public enum QueGuardar
{
    ApiKeyExterna,          // Stripe, SendGrid, Twilio…
    ClientSecretAppReg,     // secret de una App Registration
    CertificadoSsl,         // *.empresa.com
    ClaveCifrado,           // RSA/EC para CMK o firmar JWT
    ConexionAzureAAzure,    // App Service → Cosmos/SQL/Storage
}

public enum Destino { ManagedIdentity, KeyVaultSecret, KeyVaultKey, KeyVaultCertificate }

public enum AccesoKv { Lectura, Gestion, UsoCripto }

// Slides 2-5 — la regla: si es Azure-a-Azure → Managed Identity (NO va
// a Key Vault); si es un secreto que no puede ser MI → Key Vault, en el
// tipo correcto (Secret/Key/Certificate). Tabla de decisión pura.
public static class KeyVaultItemAdvisor
{
    public static Destino Donde(QueGuardar que) => que switch
    {
        // Slide 2 — lo que SÍ puede ser MI no va a Key Vault.
        QueGuardar.ConexionAzureAAzure => Destino.ManagedIdentity,
        QueGuardar.ApiKeyExterna or QueGuardar.ClientSecretAppReg
            => Destino.KeyVaultSecret,
        QueGuardar.CertificadoSsl => Destino.KeyVaultCertificate,
        QueGuardar.ClaveCifrado => Destino.KeyVaultKey,
        _ => throw new ArgumentOutOfRangeException(nameof(que)),
    };

    // Slide 5 — rol RBAC mínimo de Key Vault según tipo de item +
    // acceso. NUNCA "Key Vault Administrator" salvo gestión total.
    public static string RolMinimo(Destino destino, AccesoKv acceso) => destino switch
    {
        Destino.KeyVaultSecret => acceso == AccesoKv.Lectura
            ? "Key Vault Secrets User"
            : "Key Vault Secrets Officer",
        Destino.KeyVaultKey => acceso == AccesoKv.UsoCripto
            ? "Key Vault Crypto User"
            : "Key Vault Crypto Officer",
        Destino.KeyVaultCertificate => "Key Vault Certificates Officer",
        Destino.ManagedIdentity => "(no aplica: el acceso es por RBAC del recurso destino)",
        _ => throw new ArgumentOutOfRangeException(nameof(destino)),
    };

    // Slide 5 — RBAC es lo recomendado; las Access Policies son legacy.
    public const bool RbacRecomendadoSobreAccessPolicies = true;
}
