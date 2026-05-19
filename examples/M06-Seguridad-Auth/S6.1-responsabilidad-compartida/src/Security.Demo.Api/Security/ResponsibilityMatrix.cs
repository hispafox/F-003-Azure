namespace Security.Demo.Api.Security;

public enum Capa
{
    DatosYClasificacion, CuentasEIdentidades, DispositivosCliente,
    Aplicacion, ControlesDeRed, SistemaOperativo,
    HostsFisicos, RedFisica, Datacenter,
}

public enum ModeloServicio { OnPrem, IaaS, PaaS, SaaS }

public enum Responsable { Tu, Azure, Mixto }

// Slide 3 — el modelo de responsabilidad compartida como tabla de
// decisión pura. "La línea que NUNCA cambia": datos, identidades y
// dispositivos son SIEMPRE tuyos, en cualquier modelo.
public static class ResponsibilityMatrix
{
    public static bool SiempreTuya(Capa capa) => capa is
        Capa.DatosYClasificacion or
        Capa.CuentasEIdentidades or
        Capa.DispositivosCliente;

    public static Responsable Responsable(Capa capa, ModeloServicio modelo)
    {
        if (SiempreTuya(capa)) return Security.Responsable.Tu;

        return capa switch
        {
            Capa.Aplicacion => modelo == ModeloServicio.SaaS
                ? Security.Responsable.Azure : Security.Responsable.Tu,

            Capa.ControlesDeRed => modelo switch
            {
                ModeloServicio.OnPrem or ModeloServicio.IaaS => Security.Responsable.Tu,
                ModeloServicio.PaaS => Security.Responsable.Mixto,
                _ => Security.Responsable.Azure,
            },

            Capa.SistemaOperativo => modelo is ModeloServicio.OnPrem or ModeloServicio.IaaS
                ? Security.Responsable.Tu : Security.Responsable.Azure,

            // Hosts físicos, red física, datacenter: solo tuyos On-Prem.
            _ => modelo == ModeloServicio.OnPrem
                ? Security.Responsable.Tu : Security.Responsable.Azure,
        };
    }
}
