namespace AzureFunctions.Demo.Tests;

public class LimpiezaProgramadaFunctionTests
{
    [Fact]
    public void Procesar_Registra_Una_Ejecucion()
    {
        var (fn, tracker) = TestHost.NewLimpieza();

        var resultado = fn.Procesar(llegoTarde: false);

        Assert.False(resultado.LlegoTarde);
        Assert.InRange(resultado.RegistrosEliminados, 10, 99);
        Assert.Equal(1, tracker.TotalEjecuciones);
    }

    [Fact]
    public void Procesar_Multiples_Veces_Acumula_En_Tracker()
    {
        var (fn, tracker) = TestHost.NewLimpieza();

        fn.Procesar(false);
        fn.Procesar(false);
        fn.Procesar(false);

        Assert.Equal(3, tracker.TotalEjecuciones);
    }

    [Fact]
    public void Procesar_Con_LlegoTarde_Se_Refleja_En_Resultado()
    {
        // El timer marca IsPastDue=true cuando la ejecución previa se
        // saltó (Functions estaba parada). El warning lo logueamos pero
        // la limpieza se ejecuta igual.
        var (fn, _) = TestHost.NewLimpieza();

        var resultado = fn.Procesar(llegoTarde: true);

        Assert.True(resultado.LlegoTarde);
    }
}
