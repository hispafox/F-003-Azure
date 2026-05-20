# S4.5 — Testing local y depuración

> **Submódulo de referencia:** [M04-S4.5](../../../doc/M04-Azure-Functions-II/v4-actual/M04-S4.5-testing-depuracion-v4.md)
> **TFM:** `net10.0` · **Tipo:** Azure Functions isolated worker · **Tier:** Consumption
> **Coste:** ~0 € (sin Service Bus ni Cosmos)

> 📘 **¿Primera vez con este ejemplo?** Lee el [MANUAL.md](MANUAL.md) — manual del alumno: el chef y el ayudante de cocina, la pirámide de tests con un solo `SkippableFact` para integración, la lección dura del DI no ejercitado por los tests y el catálogo contextualizado de patrones de testing.

## Objetivo

Submódulo **meta**: cómo se testean y depuran Functions. El ejemplo
materializa la **pirámide de tests** (slide 2/14) sobre una función
pequeña, y el README sirve además de **catálogo consolidado de patrones
y gotchas de testing** descubiertos a lo largo de M03/M04.

> 🎯 **Patrón que el submódulo predica (slide 6/7/10/11)**: la lógica vive
> en **servicios**, la función es "pegamento". Así el 80% de los tests son
> unit-tests rápidos del servicio, y el test de la función solo verifica
> el wiring con el servicio mockeado.

## La pirámide, implementada

```
        ╱╲   Integration (1)  ← Testcontainers.Azurite, SkippableFact
       ╱──╲                     (se SALTA si no hay Docker)
      ╱ Fn ╲  Function (4)    ← PedidosApi con IDescuentoCalculator
     ╱──────╲                   mockeado vía NSubstitute (slide 6)
    ╱  Unit  ╲ Unit (19)      ← lógica pura: descuento escalonado,
   ╱──────────╲                 limpieza del timer, parseo del CSV
```

| Capa | Archivo | Qué prueba | Slide |
| --- | --- | --- | --- |
| Unit | `Unit_DescuentoCalculatorTests` | `[Theory]` escalonada del descuento | 7 |
| Unit | `Unit_ServiciosExtraidosTests` | lógica del Timer y del Blob como servicio | 10, 11 |
| Function | `Function_PedidosApiTests` | wiring HTTP con servicio NSubstitute | 6 |
| Integration | `Integration_AzuriteBlobTests` | round-trip real contra Azurite (Docker) | 8, 15 |

```bash
dotnet test
# 23 passed, 1 skipped (integration — sin Docker), 0 failed
```

> El test de integración usa **`Xunit.SkippableFact`**: si Docker no está,
> hace `Skip.If(...)` en vez de fallar. Así `dotnet test` queda **siempre
> verde** en máquinas/CI sin Docker, y se ejecuta de verdad donde lo haya.
> Es la forma de tener integration tests "opcionales" sin romper la suite.

## Catálogo de patrones y gotchas de testing (M03 + M04)

Lo más reutilizable de este submódulo. Cada uno descubierto resolviendo
un ejemplo real del curso:

### 1. Tests de Functions = instanciación directa (NO WebApplicationFactory)

Los `[Function]` se testean haciendo `new MiFunction(deps...)` y pasando
un `HttpRequest` fabricado con `DefaultHttpContext`. `WebApplicationFactory`
**no aplica** al worker aislado.

### 2. ⚠️ Eso NO ejercita el contenedor DI → bug latente

Como los tests instancian a mano, **un `Program.cs` con un servicio sin
registrar pasa los tests igualmente** pero el Function App real revienta
en runtime con *"Unable to resolve service"*. Pasó de verdad en S3.4 de
este curso (`IInformeService`/`IImportSummaryService`/`ICsvProductosImporter`
inyectados pero no registrados; 48/48 tests verdes, app rota).

**Regla**: tras escribir las funciones, **cruzar a mano cada parámetro de
constructor de cada `[Function]` (y de los servicios que estos inyectan)
con los `AddSingleton/Scoped/Transient` de `Program.cs`**. Los tests no te
cubren esto.

### 3. `FakeServiceBusMessageActions` (S4.1, S4.3)

`ServiceBusMessageActions` es abstracta; deriva un fake que registre
`Complete/Abandon/DeadLetter` en propiedades booleanas. La sobrecarga
virtual de `DeadLetterMessageAsync` lleva `Dictionary<string,object>?
propertiesToModify, string? deadLetterReason, ...` (no la firma "corta").

### 4. `ServiceBusModelFactory` (S4.3)

No tiene parámetros `deadLetterReason`/`deadLetterErrorDescription`. Se
fijan vía el diccionario `properties:` con claves bien conocidas
(`"DeadLetterReason"`, `"DeadLetterErrorDescription"`).

### 5. NSubstitute para `TaskOrchestrationContext` (S4.2)

Superficie de ~20 miembros virtuales → demasiado para un fake a mano.
`Substitute.For<TaskOrchestrationContext>()`. Gotcha:
`CreateReplaySafeLogger<T>()` devuelve `null` por defecto en el mock →
configúralo a `NullLogger<T>.Instance` o el orquestador peta.
`TaskFailedException` tiene ctor público `(taskName, taskId, inner)`
para simular el fallo de actividad tras reintentos.

### 6. `Activator.CreateInstance` ignora params opcionales (S4.3)

`Activator.CreateInstance(tipo, "msg")` falla si el ctor es
`(string, Exception? = null)` (no hay sobrecarga de 1 arg real). Usa
`TheoryData<Exception>` con instancias construidas explícitamente.

### 7. `[ExponentialBackoffRetry]` no vale en `ServiceBusTrigger` (S4.3)

El analyzer `AZFW0012` lo rechaza en el isolated worker. Service Bus usa
su propio `maxDeliveryCount`. El atributo solo aplica a triggers sin
retry propio (Timer, Event Hub).

### 8. Mock-vs-fake según superficie

- Superficie pequeña y estable → fake a mano (`FakeServiceBusMessageActions`).
- Superficie grande → NSubstitute (`TaskOrchestrationContext`, `IDescuentoCalculator`).
- Lógica pura → ni mock ni fake, instanciar y assertar (la mayoría).

## Estructura

```
S4.5-testing-depuracion/
├── src/AzureFunctions.Demo/
│   ├── Functions/
│   │   ├── PedidosApi.cs              (HTTP, glue sobre IDescuentoCalculator)
│   │   └── TareasFunctions.cs         (Timer + Blob, glue sobre servicios)
│   ├── Services/                      (DescuentoCalculator, Limpieza, CsvResumen)
│   └── Models/Pedido.cs
├── tests/AzureFunctions.Demo.Tests/   (Unit_* / Function_* / Integration_*)
└── scripts/                           (provision/deploy/smoke — opcional)
```

## Debugging (slide 4)

```jsonc
// .vscode/launch.json — F5 arranca func host con debugger
{
  "configurations": [{
    "name": "Attach to .NET Functions",
    "type": "coreclr", "request": "attach",
    "processId": "${command:azureFunctions.pickProcess}"
  }]
}
```

Flujo: `func start` en una terminal → Run → *Attach to .NET Functions* →
breakpoints → enviar petición con [`api.http`](src/AzureFunctions.Demo/api.http).

## Fuera de alcance (deliberado)

E2E real en staging (slide 14 — requiere despliegue y datos reales),
Bogus para datos falsos (slide 12 — no aporta sobre el dominio trivial),
Application Insights end-to-end (módulo 8).

## Próximo paso

`S4.P` (práctica de flujo completo) y `S4.P2` (Durable Hello World)
consolidan todo M04. Cierran el módulo.
