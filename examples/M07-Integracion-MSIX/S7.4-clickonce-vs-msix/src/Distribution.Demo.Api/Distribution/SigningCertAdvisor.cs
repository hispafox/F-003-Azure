namespace Distribution.Demo.Api.Distribution;

// Slide 8 — tipos de certificado para firmar MSIX.
public enum TipoCertificado { SelfSigned, EnterpriseCa, PublicCa, MicrosoftStore }

public enum EscenarioFirma
{
    Desarrollo,
    DistribucionInterna,
    DistribucionExterna,
    PublicacionStore,
}

public sealed record RecomendacionCert(
    TipoCertificado Tipo, string Coste, string SmartScreen, string Justificacion);

// Slide 8 — decisión del certificado de firma por escenario.
public static class SigningCertAdvisor
{
    public static RecomendacionCert Recomendar(EscenarioFirma escenario) => escenario switch
    {
        EscenarioFirma.Desarrollo => new(
            TipoCertificado.SelfSigned, "Gratis", "Warning",
            "Self-signed con New-SelfSignedCertificate; solo dev/test (slide 8)."),
        EscenarioFirma.DistribucionInterna => new(
            TipoCertificado.EnterpriseCa, "Incluido en AD", "Sin warning (si CA es trusted)",
            "Enterprise CA (AD CS): de confianza en todos los PCs del dominio (slide 8)."),
        EscenarioFirma.DistribucionExterna => new(
            TipoCertificado.PublicCa, "~200-500 €/año", "Sin warning",
            "Public CA (DigiCert, Sectigo, Trusted Signing): firma reconocida globalmente (slide 8)."),
        EscenarioFirma.PublicacionStore => new(
            TipoCertificado.MicrosoftStore, "Gratis (con dev account)", "Sin warning",
            "Microsoft Store firma el paquete al publicarlo (slide 8)."),
        _ => throw new ArgumentOutOfRangeException(nameof(escenario)),
    };
}
