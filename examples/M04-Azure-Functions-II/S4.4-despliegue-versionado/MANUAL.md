# Manual del alumno — S4.4 · Despliegue y versionado

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: tabla de slides, despliegue por Portal, scripts. Este manual va antes: te cuenta por qué este submódulo entrega tan poco código pese a ser largo, qué hace exactamente cada uno de los tres patrones que sí se codifican (versionado por ruta, health post-deploy, feature flag) y por qué el feature flag suele ser un mejor rollback que el rollback "de verdad".

Tiempo de lectura: ~25 min. Submódulo de teoría: [M04-S4.4](../../../doc/M04-Azure-Functions-II/v4-actual/M04-S4.4-despliegue-versionado-v4.md). Cuatro endpoints HTTP en dos archivos, quince tests, cero servicios externos.

*Creado: 2026-05-20 14:50 +0200*

---

## 1. La idea en una frase

Desplegar a producción ya no es "subir un zip al servidor y rezar". Hay tres herramientas que convierten un deploy arriesgado en una operación rutinaria: **versionado de la API** (los clientes antiguos siguen funcionando mientras tú evolucionas), **verificación post-deploy automatizada** (el pipeline pregunta "¿está vivo?" antes de promocionar la nueva versión) y **feature flags** (encender y apagar trozos de lógica sin volver a desplegar). Las tres son código; las tres se prueban sin pisar Azure; las tres son el contenido de este ejemplo.

Slots, blue/green, Bicep, Flex Consumption y CI/CD también aparecen en el submódulo, pero como operaciones de plataforma — comandos `az`, configuración de pipeline, decisiones de plan. No son código que se desarrolle en una sesión de IDE. Por eso este ejemplo enseña los patrones programables y deja el resto a la teoría.

---

## 2. El problema real que hay detrás

Un equipo de catálogo iba a publicar un cambio "menor": añadir `moneda` y `stock` al endpoint `/api/productos`. La intención era buena, el código era de cinco líneas, y rompió a tres clientes en producción la primera tarde. Uno era una integración de un partner que parseaba el JSON con un schema estricto y rechazaba propiedades nuevas. Otro era una app móvil con un parser ad-hoc que se ahogaba con tipos inesperados. El tercero, irónicamente, era el cliente interno del equipo de logística, que tenía un test de regresión que falló al detectar diferencias en el payload.

La conversación post-mortem llegó a tres conclusiones que se ven en este ejemplo:

1. **El contrato de una API pública no se cambia in-place. Se versiona.** El cambio que rompió a los tres clientes era un breaking change disfrazado de "mejora". La solución no es discutir si añadir un campo es o no breaking — es exponer una v2 nueva y dejar v1 sirviendo a quien todavía no haya migrado.
2. **Después de cada deploy hay que verificar que la versión esperada está viva y que sus dependencias responden.** "Lo subí y parecía ir" no es verificación. Una llamada a `/api/health` que devuelve 200 o 503, comparada con la versión devuelta por `/api/version`, sí lo es.
3. **El rollback más rápido no es el rollback. Es un feature flag apagado.** Un swap de slots o un redeploy del paquete anterior tarda minutos; apagar un App Setting tarda segundos. Si la lógica nueva se puede aislar tras un flag, te ahorras la mecánica más cara del incidente.

Este ejemplo es la materialización de esas tres conclusiones.

---

## 3. Por qué esto importa en tu stack

Cualquier API que vaya más allá de "consumida solo por mi propio frontend en la misma release" se beneficia de versionado explícito desde el día uno. Si crees que tienes un cliente, mañana descubres que hay tres — uno interno, uno de un partner, uno legacy que nadie recordaba que existía. Cuando descubres a esos clientes, ya es tarde para empezar a versionar; el primer breaking change ya rompió algo.

Health checks y endpoint de versión son baratísimos de implementar (10 líneas cada uno) y se vuelven imprescindibles en cuanto tienes pipeline de CI/CD: el step de "post-deploy verification" es exactamente esa llamada. Sin ese step, tu pipeline avanza alegremente aunque la app esté caída.

Feature flags merecen más reflexión, porque su valor depende del tipo de cambio. Para una refactorización interna que no cambia el contrato, no tiene sentido — el cambio o pasa los tests o no, y rollback es redeploy del anterior. Para un cambio de **lógica de negocio** que el cliente va a notar (cálculo de descuento, regla de validación, integración con un proveedor nuevo), el flag te da un superpoder: probar en producción con tráfico real, apagar al segundo si algo huele mal. El ejemplo del descuento del 5% de fidelización es exactamente ese tipo de cambio.

---

## 4. La analogía vertebradora: las dos cocinas del restaurante

Imagina un restaurante con éxito que decide modernizar su carta. No puede cerrar tres semanas para hacer obras — perdería a sus clientes habituales. Lo que hace es montar una **segunda cocina** detrás de la pared, sirviendo la carta nueva en paralelo a la cocina vieja, durante el tiempo que haga falta hasta que todos los clientes habituales hayan probado y aprobado los platos nuevos.

Esto es exactamente el versionado por ruta. `/api/v1/productos` es la cocina vieja: sirve `{id, nombre, precio}` como llevaba haciendo años. `/api/v2/productos` es la cocina nueva: sirve `{id, nombre, precio, moneda, stock}`. Ambas cocinas usan **los mismos ingredientes** —el mismo dominio `IProductoCatalogo`—; lo que cambia es la presentación del plato, no la receta. Los `ProductoMappers.ToV1()` y `.ToV2()` son los emplatadores: cogen el mismo `Producto` interno y lo proyectan a la forma que cada cliente espera.

Mientras tanto, **un encargado del salón pasa cada hora** preguntando "¿todo bien? ¿las dos cocinas funcionan? ¿hay alguna queja?". Ese encargado es `GET /api/health`. Si responde "Healthy", el restaurante sigue abierto. Si responde "Unhealthy" tres veces seguidas, el dueño cierra preventivamente antes de que se quejen los clientes.

Y hay un **cartel en la cocina nueva** —"Modo experimental: pulse para apagar"— que el chef puede tocar en cualquier momento. Si esa noche el plato nuevo está saliendo mal, lo desactiva con un dedo y la cocina vieja vuelve a llevarlo todo. Ese cartel es el feature flag `FEATURE_NUEVO_PROCESAMIENTO`. Tocarlo cuesta dos segundos; no requiere parar el restaurante, no requiere reinaugurar, no requiere despedir al pinche.

Mantén la imagen: dos cocinas en paralelo, un encargado del salón vigilando, y un interruptor a mano para apagar lo nuevo. Eso es todo lo que el código de este ejemplo materializa.

---

## 5. Recorrido por el código

### Versionado por ruta (`ProductosVersionadasFunctions.cs`)

Cuatro funciones HTTP en un archivo: `GET /api/v1/productos`, `GET /api/v1/productos/{id}`, `GET /api/v2/productos`, `GET /api/v2/productos/{id}`. La estructura de cada una es trivial:

```csharp
public IActionResult ListarV1(...)
{
    var items = _catalogo.Listar().Select(p => p.ToV1()).ToList();
    return new OkObjectResult(new { version = "v1", total = items.Count, items });
}
```

Lo importante no está en las funciones. Está en `Producto.cs`:

```csharp
public sealed record Producto(string Id, string Nombre, decimal Precio, string Moneda, int Stock);
public sealed record ProductoV1(string Id, string Nombre, decimal Precio);
public sealed record ProductoV2(string Id, string Nombre, decimal Precio, string Moneda, int Stock);
```

Hay **un solo dominio** (`Producto`) y dos contratos de salida. Los mappers `ToV1()` y `ToV2()` proyectan el dominio al contrato apropiado. Si mañana el campo `Stock` deja de tener sentido y lo sustituyes por `InventarioDisponible`, ese cambio vive en el dominio; en la API publicas una v3 con el nuevo nombre, y v1/v2 siguen sirviendo `Stock` como hasta ahora.

Lo que NO debes hacer y lo que mata muchos sistemas: tener `Producto` como tu modelo y exponerlo directamente en `ListarV1`. Ese acoplamiento entre dominio y contrato es el que rompe el día que añades una propiedad al dominio sin pensar — porque tu cliente externo lo recibirá automáticamente. El paso por el mapper te obliga a decidir conscientemente "¿esta propiedad nueva va en v1, en v2, o solo en v3?".

### Health check post-deploy (`HealthAggregator`)

El pipeline de CI/CD llama a `GET /api/health` justo después del deploy. Lo que devuelve esa llamada decide si la promoción avanza (`200`) o se aborta (`503`):

```csharp
public HealthResultado Evaluar()
{
    foreach (var c in checks)
    {
        bool ok;
        try { ok = c.Comprobar(); }
        catch { ok = false; }
        resultados[c.Nombre] = ok ? "ok" : "fail";
        if (!ok) sano = false;
    }
    return new HealthResultado(sano ? "Healthy" : "Unhealthy", resultados);
}
```

Tres detalles operativos importantes en esas pocas líneas:

- **Agregador con `IEnumerable<IHealthCheck>`**, no una clase monolítica. Cuando añadas Cosmos y Service Bus a tu sistema, simplemente registras `CosmosHealthCheck` y `ServiceBusHealthCheck` en DI y el agregador los recoge sin tocar el endpoint. Es de las pocas veces que la inversión en abstracción se paga sola.
- **El check que lanza no rompe el endpoint**. Si `Comprobar()` tira una excepción, se trata como `fail` y se sigue evaluando el resto. Sin ese `try/catch`, una conexión rota a Cosmos haría que `/api/health` devolviera un 500, que el pipeline interpretaría como "Function App caída entera" en vez de "una dependencia inaccesible". El test `HealthCheckQueLanza_CuentaComoUnhealthy_No500` cubre exactamente eso.
- **El código de respuesta importa más que el cuerpo**. El pipeline mira si es 2xx o 5xx, no parsea el JSON. Por eso el endpoint devuelve `200 + Healthy` o `503 + Unhealthy`, y nunca un 200 con `Unhealthy` dentro.

### Endpoint de versión (`Version()`)

Diez líneas que valen oro en operaciones:

```csharp
var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
    .InformationalVersion ?? "desconocida";
return new OkObjectResult(new
{
    version = info,
    featureFlags = new { nuevoProcesamiento = _flags.Activo(ProcesadorSelector.Flag) },
});
```

Lo que hace el script post-deploy es comparar `version` con la versión que esperaba haber desplegado (un commit SHA, un tag de release, lo que use tu pipeline). Si coinciden, **confirmas que el deploy "tomó"** — porque a veces no lo hace: el Function App se queda con el binario antiguo si hay un bloqueo de archivo, el slot swap se aborta, el zip se sube pero no se aplica. Sin un endpoint de versión, lo descubres por el comportamiento; con él, lo sabes de inmediato.

El segundo bloque expone los flags activos. Esto es para depurar incidentes en caliente: cuando el equipo de soporte te diga "el endpoint X devuelve cosas raras", lo primero que miras es `/api/version` para confirmar qué versión está corriendo y qué flags tiene activos. Te ahorra la conversación de "¿seguro que está la nueva?" tan típica de los días de incidente.

### Feature flag (`ProcesadorSelector`)

El selector encapsula la decisión:

```csharp
public IProcesadorPedido Seleccionar() => flags.Activo(Flag) ? nuevo : legacy;
```

Y la función simplemente lo usa:

```csharp
var procesador = _selector.Seleccionar();
var resultado = procesador.Procesar(pedido);
```

Lo que evita el selector es que `OperacionesFunctions` lea variables de entorno directamente. Si lo hiciera, sería complicado de testear (habría que `Environment.SetEnvironmentVariable` en cada test). Con el selector, el test inyecta un `IFeatureFlags` falso y comprueba que con el flag a true se invoca al procesador nuevo, con el flag a false al legacy.

El procesador nuevo aplica un 5% de descuento de fidelización. Si en producción descubres que el cálculo está mal o que el equipo de marketing no estaba de acuerdo con la métrica, **apagas el flag** en `Configuration → FEATURE_NUEVO_PROCESAMIENTO = false` y guardas. En diez segundos el procesador legacy vuelve a ser el activo. Sin redeploy, sin pipeline, sin esperar.

---

## 6. Lo que el ejemplo deja deliberadamente fuera

S4.4 es de los submódulos donde la teoría va por delante del código por una razón legítima: los slots, Bicep, Deployment Stacks y Flex Consumption no son código que escribas, son configuraciones de plataforma. Y la mayoría requieren plan **Premium o Dedicated**, que cuesta dinero todo el rato (Consumption es gratis hasta el millón de invocaciones).

Lo que el README documenta sin ejecutar:

- **Slots** (slide 5). En Premium, `az functionapp deployment slot create … --slot staging`. Despliegas a staging, ejecutas tu script post-deploy contra el slot, y si pasa, haces `slot swap`. Las connection strings con marca *sticky* se quedan en el slot que las define, así que los triggers de Service Bus de staging no consumen mensajes de producción. En Consumption no hay slots — la única forma de tener "staging" es otro Function App entero (`-staging` o `-blue/-green`).
- **Blue/green sin slots** (slide 11). Dos Function Apps, despliegas a la "verde", verificas con tu script, y cambias el routing en el frontend (Application Gateway, APIM o un alias DNS). La parte de "cambiar el routing" es la que añade complejidad si quieres hacerla automática.
- **Rollback**. Con slots = swap inverso, instantáneo. Sin slots = redeploy del artefacto anterior (por eso guardas siempre los zips de release) o `git revert` + pipeline. Y la opción del feature flag, que ya tienes implementada aquí.

Si te encuentras con un caso real donde necesites todo esto, la cadencia natural es: empezar con Consumption y feature flags → cuando tengas tráfico que justifique Premium, añadir slots → cuando tengas múltiples regiones, añadir blue/green con APIM/Front Door delante.

---

## 7. Cómo probarlo en local

Es el ejemplo más cómodo del módulo: no necesita Service Bus, ni Cosmos, ni emuladores. `dotnet test` ejecuta los 15 tests en segundos y la mayor parte del trabajo se hace ahí sin tocar Azure.

Para reproducir end-to-end (Portal):

- Resource Group `rg-curso-m04-s44`.
- Storage Account `stcursom04s44{iniciales}` (LRS).
- Function App .NET 10 Isolated, Linux, Consumption, usando ese Storage.
- **Configuration → New application setting**:
  - `WEBSITE_RUN_FROM_PACKAGE = 1` (slide 4 — el deploy es atómico, no un copy de archivos uno a uno).
  - `FEATURE_NUEVO_PROCESAMIENTO = false` (despliegas con la feature apagada — luego la enciendes a mano).
- Deploy desde VS Code.

Cuando ya está arriba, te recomiendo ejecutar este pequeño ritual:

```bash
APP="https://func-curso-m04-s44-{ini}.azurewebsites.net"

curl $APP/api/health    # 200 con Healthy si todo OK
curl $APP/api/version   # ves el build vivo y los flags activos

curl $APP/api/v1/productos   # contrato viejo
curl $APP/api/v2/productos   # contrato nuevo con moneda y stock
```

Y luego juega con el flag: Configuration → cambia `FEATURE_NUEVO_PROCESAMIENTO` a `true` → Save → vuelves a llamar a `POST /api/pedidos/procesar` con un body válido y la respuesta dice `procesadoPor: "nuevo"`. Apagas el flag → `procesadoPor: "legacy"` al instante.

> Yo no lanzo apps. Tú haces `func start --csharp` y `dotnet test`.

---

## 8. Los tests son la documentación viva

15 pruebas que cubren los tres patrones sin Azure:

**`ProductosVersionadasTests`** verifica que v1 y v2 proyectan el mismo dominio: el `precio` es idéntico en ambas respuestas, lo único que cambia es la presencia de `moneda` y `stock`. Y prueba que un id inexistente devuelve 404 en las dos. Si alguien refactoriza los mappers y rompe la equivalencia, el test salta.

**`OperacionesFunctionsTests`** cubre los siete escenarios operativos críticos: feature flag off → `procesadoPor: legacy` y total intacto; feature flag on → `procesadoPor: nuevo` y total con el descuento; body inválido → 400; health con todos los checks OK → 200; un check falla → 503; un check que lanza excepción → cuenta como unhealthy, no como 500; `/version` expone los flags. Esos siete tests son la especificación operativa del sistema.

**`HelloFunctionTests` + `PingFunctionTests`** son heredados del esqueleto del curso. Cuatro tests adicionales que validan que la app sigue arrancando y respondiendo, igual que en los otros submódulos.

Un detalle pedagógico de los tests del feature flag: el `IFeatureFlags` se inyecta como dependencia con la implementación `EnvFeatureFlags` en producción (lee de variables de entorno) y un fake `DiccionarioFeatureFlags` en tests (lee de un `Dictionary<string,bool>`). Esto es lo que te permite cubrir "flag on" y "flag off" en milisegundos sin tocar el entorno.

---

## 9. La trampa del feature flag mal usado

Un flag de feature es una herramienta, no una solución universal. Tres formas de hacerlo mal que merece la pena nombrar:

- **Flags eternos**. Un flag debe vivir el tiempo del rollout — días o semanas. Si llevas seis meses con `FEATURE_NUEVO_PROCESAMIENTO = true` en producción, ya no es una feature flag, es un `if` permanente. Quita el flag y limpia el código del procesador legacy. Si no, vas a acumular ramas muertas que nadie se atreve a borrar porque "¿y si hay que volver?".
- **Flags que cambian de significado**. Un flag que empezó siendo "activa el cálculo nuevo de descuento" y acaba siendo "activa el cálculo nuevo y también el nuevo endpoint y también el nuevo cliente HTTP" es una cebolla operativa. Cuando hay que apagarlo, no sabes qué se rompe. Un flag = una decisión binaria atómica.
- **Flags que dependen unos de otros**. Si `FLAG_A` solo funciona si `FLAG_B` también está activo y a su vez `FLAG_B` interactúa con `FLAG_C`, has reinventado la lógica de tu sistema fuera del código fuente. Es una pesadilla de cara al soporte.

El flag del ejemplo es deliberadamente sencillo: una decisión binaria, dos implementaciones intercambiables del mismo interfaz, cero acoplamiento con otros flags. Si todos los flags de tu sistema se parecen a este, vas bien.

---

## 10. Glosario breve

- **Slot**: instancia paralela del Function App donde puedes desplegar y validar antes de mover el tráfico. Solo en Premium/Dedicated. El cambio se hace con `slot swap`, que es atómico e instantáneo.
- **Blue/green**: tienes dos entornos (blue y green); el tráfico apunta a uno; despliegas en el otro; cuando está validado, cambias el routing. En Functions sin slots se hace con dos Function Apps + un Application Gateway / APIM / DNS por delante.
- **Run from Package** (`WEBSITE_RUN_FROM_PACKAGE=1`): la Function App monta el zip de release directamente como filesystem read-only, en vez de descomprimirlo. El deploy es atómico, sin estado intermedio "medio copiado".
- **Health probe**: endpoint que devuelve 2xx si la app está sana, 5xx si no. El pipeline lo llama post-deploy; APIM/Application Gateway lo llaman cada pocos segundos para enrutar tráfico solo a instancias sanas.
- **Feature flag**: variable de entorno o servicio externo (Azure App Configuration, LaunchDarkly) que activa o desactiva un trozo de lógica sin redeploy. La forma más rápida de hacer "rollback" si la lógica nueva está aislada tras un flag.
- **Sticky setting**: connection string o App Setting que se queda en su slot durante un swap. Garantiza que `production` no use accidentalmente la cadena de Service Bus de `staging`.
- **Breaking change**: cualquier cambio en el contrato de la API que pueda romper a un cliente existente. Añadir un campo opcional NO es breaking; cambiar el tipo de un campo SÍ; quitar un endpoint también; añadir un campo obligatorio en el request también.

---

## 11. Para ir más allá del ejemplo

Tres frentes naturales que el ejemplo deja abiertos a propósito:

- **Azure App Configuration en vez de variables de entorno** para los feature flags. Sigue siendo un toggle, pero centralizado, auditable y con UI. Es trivial conectarlo a `IFeatureFlags`.
- **Health checks reales** (`CosmosHealthCheck`, `ServiceBusHealthCheck`). Cuando tu Function App tenga dependencias externas, cada una merece su check. El `HealthAggregator` ya los recoge solo con que estén en DI.
- **Pipeline de CI/CD** que ejecute el script `05-postdeploy-check.sh` después de cada deploy y aborte si `/api/health` o `/api/version` no devuelven lo esperado. Está fuera de alcance aquí pero es el cierre operativo del submódulo.

---

## 12. Cierre

El submódulo te deja con tres herramientas que no son glamurosas pero te salvarán incidentes: versionado de la API, health check, feature flag. Ninguna es complicada de implementar. Las tres marcan la diferencia entre "esperamos que el deploy haya salido bien" y "el pipeline confirmó que salió bien y, si algo falla, apago el flag y volvemos al estado anterior en diez segundos".

Lo siguiente es [`S4.5 — Testing y depuración`](../S4.5-testing-depuracion/MANUAL.md), que cierra el bloque conceptual de M04 con estrategias de test (unit/integration), depuración local y remota, y observabilidad — la disciplina diaria de mantener un sistema de Functions vivo en producción.
