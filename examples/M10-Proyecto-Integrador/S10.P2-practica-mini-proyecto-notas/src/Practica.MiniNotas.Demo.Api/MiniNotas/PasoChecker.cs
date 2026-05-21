namespace Practica.MiniNotas.Demo.Api.MiniNotas;

public enum Paso
{
    DisenarModelo,           // slide 4
    CrearSolucion,           // slide 5
    ImplementarModelo,       // slide 6
    ImplementarRepositorio,  // slide 7
    EndpointsCrud,           // slide 8
    TestsUnitarios,          // slide 9
    SmokeTests,              // slide 10
    CrearInfra,              // slide 11
    DesplegarApp,            // slide 12
    ValidarEndToEnd,         // slide 13
    Limpiar,                 // slide 14
}

public enum ResultadoPaso { Pasa, Falla, Pendiente }

public sealed record EvidenciaPaso(
    Paso Paso,
    bool ComandoEjecutado,
    bool OutputEsperadoVisible,
    string? Comentario = null);

public sealed record InformePaso(
    Paso Paso,
    string Slide,
    ResultadoPaso Resultado,
    IReadOnlyList<string> AccionesSugeridas);

// Slides 4-14 — evaluador de los 11 pasos del mini-proyecto. Cada
// paso lleva slide + sugerencia específica de qué probar si el output
// no es el esperado. Lógica pura.
public static class PasoChecker
{
    public static InformePaso Evaluar(EvidenciaPaso e)
    {
        ArgumentNullException.ThrowIfNull(e);

        string slide = SlideDe(e.Paso);
        var acciones = new List<string>();

        if (!e.ComandoEjecutado)
            acciones.Add(SugerenciaComando(e.Paso));
        if (!e.OutputEsperadoVisible)
            acciones.Add(SugerenciaOutput(e.Paso));

        ResultadoPaso resultado;
        if (acciones.Count == 0)
        {
            resultado = ResultadoPaso.Pasa;
            acciones.Add($"Paso {e.Paso} completado (slide {slide}).");
        }
        else if (!e.ComandoEjecutado && !e.OutputEsperadoVisible)
        {
            resultado = ResultadoPaso.Falla;
        }
        else
        {
            resultado = ResultadoPaso.Pendiente;
        }

        return new InformePaso(e.Paso, slide, resultado, acciones);
    }

    public static string SlideDe(Paso p) => p switch
    {
        Paso.DisenarModelo => "4",
        Paso.CrearSolucion => "5",
        Paso.ImplementarModelo => "6",
        Paso.ImplementarRepositorio => "7",
        Paso.EndpointsCrud => "8",
        Paso.TestsUnitarios => "9",
        Paso.SmokeTests => "10",
        Paso.CrearInfra => "11",
        Paso.DesplegarApp => "12",
        Paso.ValidarEndToEnd => "13",
        Paso.Limpiar => "14",
        _ => "0",
    };

    private static string SugerenciaComando(Paso p) => p switch
    {
        Paso.DisenarModelo =>
            "Anota campos de Note: Id (RowKey), PartitionKey='notes', Title, Content, Tags, " +
            "CreatedAt, UpdatedAt (slide 4).",
        Paso.CrearSolucion =>
            "`dotnet new sln` + `dotnet new webapi -o src/MiniNotas` + " +
            "`dotnet new xunit -o tests/MiniNotas.Tests` + `dotnet sln add` (slide 5).",
        Paso.ImplementarModelo =>
            "Crea `src/MiniNotas/Models/Note.cs` que implemente `ITableEntity` con los " +
            "campos del slide 4 (slide 6).",
        Paso.ImplementarRepositorio =>
            "Crea `INotesRepository` + `NotesRepository` con `TableClient` + " +
            "`CreateIfNotExists` + `GetAllAsync` filtrando por PartitionKey (slide 7).",
        Paso.EndpointsCrud =>
            "Mapea 5 endpoints en `Program.cs`: `GET /notes`, `GET /notes/{id}`, " +
            "`POST /notes`, `PUT /notes/{id}`, `DELETE /notes/{id}` (slide 8).",
        Paso.TestsUnitarios =>
            "Escribe `NotesRepositoryTests` con Azurite o un fake del `TableClient`. " +
            "`dotnet test` debe pasar (slide 9).",
        Paso.SmokeTests =>
            "Crea `smoke-tests.sh`: crear nota, obtenerla, listarla, actualizarla y borrarla " +
            "con `curl` (slide 10).",
        Paso.CrearInfra =>
            "`az group create` + `az storage account create` + `az appservice plan create --sku F1` + " +
            "`az webapp create --runtime DOTNETCORE:8.0` (slide 11).",
        Paso.DesplegarApp =>
            "`dotnet publish -c Release -o publish` + `cd publish && zip -r ../app.zip .` + " +
            "`az webapp deploy --src-path app.zip --type zip` (slide 12).",
        Paso.ValidarEndToEnd =>
            "Pasa `smoke-tests.sh` con la URL pública: `SMOKE_URL=https://<app>.azurewebsites.net " +
            "./smoke-tests.sh` (slide 13).",
        Paso.Limpiar =>
            "`az group delete --name <rg> --yes --no-wait` (slide 14). Verifica con " +
            "`az group exists` que devuelve `false`.",
        _ => "Ejecuta el comando indicado en el slide correspondiente.",
    };

    private static string SugerenciaOutput(Paso p) => p switch
    {
        Paso.DisenarModelo =>
            "Decide ANTES de codear: ¿PartitionKey='notes' fijo o por userId? Para esta " +
            "práctica fija (slide 4).",
        Paso.CrearSolucion =>
            "`dotnet build` debe terminar con `0 Warning(s) 0 Error(s)`. Si falla, comprueba " +
            "que `dotnet sln add` añadió ambos csproj (slide 5).",
        Paso.ImplementarModelo =>
            "El proyecto debe compilar. Si falta `Azure.Data.Tables`, " +
            "`dotnet add package Azure.Data.Tables` (slide 6).",
        Paso.ImplementarRepositorio =>
            "Compila + las cinco operaciones del interface están implementadas con " +
            "`TableClient` real (no `NotImplementedException`).",
        Paso.EndpointsCrud =>
            "`dotnet run` arranca y `curl http://localhost:5xxx/notes` devuelve `[]` la primera vez.",
        Paso.TestsUnitarios =>
            "`dotnet test` debe imprimir `Passed: N` con N ≥ 3 (al menos GetAll vacío, " +
            "Create + GetById, Delete + GetById null).",
        Paso.SmokeTests =>
            "Los 5 pasos del smoke test devuelven 2xx y el último `GET` devuelve `null` o " +
            "`404` tras el `DELETE`.",
        Paso.CrearInfra =>
            "Tras los `az ... create`, `az webapp browse` o `curl https://<app>.azurewebsites.net` " +
            "devuelve la página por defecto de App Service.",
        Paso.DesplegarApp =>
            "Tras `az webapp deploy`, `az webapp log tail` muestra el banner de Minimal API arrancando. " +
            "Si tarda, esperar 30-60 s al cold start del F1.",
        Paso.ValidarEndToEnd =>
            "El smoke test contra la URL pública devuelve `✅ Smoke tests passed` y la latencia " +
            "media es < 2s (slide 13).",
        Paso.Limpiar =>
            "`az group list -o table` no debe mostrar tu RG. Si no, `az group delete` no terminó.",
        _ => "Revisa el output esperado del slide correspondiente.",
    };
}
