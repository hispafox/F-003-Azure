# Manual del alumno — S8.4 · Azure DevOps vs GitHub Actions

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: tabla de slides, scripts, preflight. Este manual va antes: te cuenta por qué Microsoft tiene dos productos parecidos haciendo cosas parecidas, qué hace mejor cada uno, cuándo el híbrido es la respuesta correcta y qué cuesta cada opción realmente.

Tiempo de lectura: ~20 min. Submódulo de teoría: [M08-S8.4](../../../doc/M08-DevOps-Automatizacion/v3-actual/M08-S8.4-ado-vs-github-actions-v3.md). Tres piezas de lógica pura (advisor con tres salidas posibles, mapeador de equivalencias YAML, estimador de coste mensual) más un planificador con checklist de migración.

*Creado: 2026-05-20 23:45 +0200*

---

## 1. La idea en una frase

Microsoft compró GitHub en 2018 y ahora es dueño de **dos plataformas de CI/CD** muy parecidas: Azure DevOps (ADO) y GitHub Actions. La conversación que aparece en cualquier equipo es "¿cuál usamos?". La respuesta del submódulo: **no hay un ganador absoluto** — hay tres opciones (ADO, GitHub, híbrido) y un par de criterios objetivos para elegir. ADO sigue siendo mejor para Boards de sprint con estimación y velocity; GitHub gana para open source y security features nativas (Dependabot, CodeQL); el híbrido (repos en GitHub + Pipelines+Boards en ADO) es legítimo y funciona. Y el coste mensual: para equipos pequeños, ADO es más barato (los primeros 5 usuarios son gratis); para 10+ usuarios o si necesitas GHAS, los números cambian.

---

## 2. El problema real que hay detrás

Tres situaciones que justifican el submódulo:

**Caso 1 — la migración a GitHub Actions "porque es lo moderno".** Un equipo con ADO funcionando bien decidió migrar a GitHub Actions "porque parece más moderno". Tres meses para reescribir 15 pipelines en YAML de GitHub Actions, formar al equipo, configurar service connections con OIDC nuevos, revisar todos los Variable Groups. El resultado final: el mismo CI/CD que tenían antes, pero con una sintaxis diferente. **Cero beneficio operativo, tres meses de trabajo perdido.** La lección 20 del submódulo: "Antes de migrar por modernizar, define qué beneficio concreto y medible obtienes".

**Caso 2 — el Boards de GitHub que se quedaba corto.** Otro equipo eligió GitHub Actions y GitHub Projects (Beta) para todo. Cuando llegó la primera planificación de sprint con velocity, estimación en story points, burndown chart y dependencias entre work items, **GitHub Projects no llegaba**. Tuvieron que improvisar con tags y campos custom. La conclusión: para equipos serios con sprints, ADO Boards sigue siendo significativamente mejor. **Cambiaron a híbrido**: repos en GitHub (mantienen la coherencia de marca, beneficios de comunidad), Pipelines + Boards en ADO. El advisor del ejemplo lo detecta automáticamente.

**Caso 3 — el coste oculto de Test Plans**. Una empresa entró en Azure DevOps Basic para 8 personas. Coste mensual: 18 € (los primeros 5 gratis, 3 extra × 6 €). Cuando QA pidió "Test Plans para gestión de tests manuales", **el coste subió a 18 € + 8×52 = 434 €/mes**. Test Plans no estaba previsto en el presupuesto y multiplica por 24 el coste. El estimador del ejemplo lo modela: addon de Test Plans solo aplica en ADO, y cuesta 52 €/usuario/mes. Conocer estos detalles ANTES de las decisiones evita sorpresas.

Los tres casos los resuelve el advisor del ejemplo (cuándo cada plataforma) más el estimador de coste (cuánto realmente cuesta).

---

## 3. Por qué esto importa en tu stack

Si te toca elegir o ya elegiste, tres preguntas que conviene tener zanjadas:

- **¿Qué plataforma encaja con mi equipo?** Equipo en Azure ya con sprints → ADO. Open source o equipo distribuido en GitHub → GitHub. Necesidades en ambos lados → híbrido.
- **¿Cuánto va a costar realmente?** ADO Basic: 5 primeros usuarios gratis, 6 €/extra/mes. GitHub Team: 4 €/usuario desde el primero. Addons: Test Plans solo ADO (52 €/usuario/mes), GHAS en ambas (49 €/usuario/mes).
- **¿Cómo mapeo lo que ya tengo si migro?** El YAML cambia. Las equivalencias del slide 6 cubren el 90%: `stages/jobs/steps` → `jobs/steps`, `task:` → `uses:`, `$(var)` → `${{ var }}`, `dependsOn:` → `needs:`, `condition: succeeded()` → `if: success()`.

Tener las tres respuestas claras te ahorra el caso 1 (migración sin valor), el caso 2 (cambiar a mitad de proyecto) y el caso 3 (factura sorpresa).

---

## 4. La analogía vertebradora: dos restaurantes del mismo dueño

Imagina que un mismo restaurador (Microsoft) tiene dos restaurantes en la misma calle:

- **Restaurante Clásico** (Azure DevOps): abierto desde hace 15 años. Carta extensa, servicio formal, manteles, mesas para reuniones de trabajo (Boards completos con sprint planning), cuenta separada disponible (Test Plans). Clientela de toda la vida que conoce al jefe de sala.
- **Restaurante Moderno** (GitHub Actions): abierto desde hace 7 años, comprado por el mismo dueño. Más informal, con buena comunidad de comensales que se hablan entre ellos (marketplace de actions), platos con etiquetas claras (security scanning, Dependabot, CodeQL). Clientela del barrio que pasa cada día.

Cuando vas a comer, no hay un restaurante "mejor": hay uno que encaja con lo que necesitas hoy:

- **Comida de empresa con presentación formal y orden del día**: el clásico (ADO con Boards).
- **Comida informal con amigos donde queréis hablar de varios temas**: el moderno (GitHub con la comunidad).
- **Tienes la reunión formal en el clásico pero los cafés después en el moderno**: **híbrido**. Coordinas con los dos restaurantes.

Y luego está la cuenta:

- **Clásico**: las primeras 5 personas gratis (cortesía de la casa); a partir de la 6ª, 6 € por cabeza. El postre formal (Test Plans) lo cobran a 52 € por persona.
- **Moderno**: 4 € por cabeza desde la 1ª. El servicio de seguridad alimentaria (GHAS) lo cobran a 49 € por persona.

Para 5 personas con menú estándar: clásico = 0 €, moderno = 20 €. **Clásico gana**.
Para 10 personas con menú estándar: clásico = 30 €, moderno = 40 €. **Clásico sigue ganando**.
Para 10 personas con servicio de seguridad: clásico = 30 + 490 = 520 €, moderno = 40 + 490 = 530 €. **Empate técnico**.
Para 10 personas con postre formal: clásico = 30 + 520 = 550 €, moderno = 40 € (no tienen postre formal). **Moderno gana solo por no ofrecer ese servicio**.

Mantén la imagen: dos restaurantes del mismo dueño, distintos públicos, distintas cuentas. Tu trabajo es elegir el que encaja sin dejarte sorprender por la cuenta.

---

## 5. Recorrido por el código

### `PlatformAdvisor.Recomendar` — tres salidas posibles

La función central:

```csharp
public static RecomendacionPlataforma Recomendar(EscenarioPlataforma e)
{
    var aAdo = new List<string>();
    var aGh = new List<string>();

    if (e.YaUsasAdo) aAdo.Add("Ya usas Azure DevOps...");
    if (e.NecesitaBoardsCompletos) aAdo.Add("Boards completos → ADO es superior...");
    if (e.NecesitaTestPlans) aAdo.Add("Test Plans → exclusivo de ADO...");
    if (e.OnPremises) aAdo.Add("On-premises → Azure DevOps Server...");

    if (e.OpenSource) aGh.Add("Open source → GitHub...");
    if (e.QuiereDependabotCodeQL) aGh.Add("Dependabot + CodeQL → GitHub...");
    if (e.EquipoDistribuidoYaEnGitHub) aGh.Add("Equipo ya en GitHub → coherencia...");

    // Hybrid: señales fuertes en AMBOS lados.
    if (aAdo.Count > 0 && aGh.Count > 0)
        return new RecomendacionPlataforma(Hybrid, [...]);

    // El que más señales tiene gana; sin señales → ADO por defecto.
    if (aAdo.Count > aGh.Count) return AzureDevOps;
    if (aGh.Count > aAdo.Count) return GitHubActions;
    return AzureDevOps;   // empate sin señales: ADO para equipos pequeños
}
```

Cuatro señales que empujan a ADO:

1. **Ya usas ADO y funciona**. La lección 20 hecha código.
2. **Necesitas Boards completos** (sprints con velocity, burndown, dependencias).
3. **Necesitas Test Plans**.
4. **On-premises** (Azure DevOps Server, sin equivalente GitHub).

Tres señales que empujan a GitHub:

1. **Open source o mixto**.
2. **Quieres Dependabot + CodeQL nativos**.
3. **Equipo distribuido ya en GitHub**.

Y la innovación: **híbrido**. Si tienes al menos una señal en ambos lados, no fuerzas la decisión binaria. Repos en GitHub (para la coherencia de marca) + Pipelines y Boards en ADO (para sprint management). Es lo que muchas empresas hacen sin reconocerlo.

Sin señales claras: ADO por defecto. La razón pragmática del slide 19: ADO es más barato para equipos pequeños (5 primeros gratis) y trae Boards completos.

### `SyntaxEquivalenceMapper.Todas` — 16 equivalencias clave

La tabla que sirve para migrar:

| Concepto | ADO YAML | GitHub Actions YAML |
| --- | --- | --- |
| Jerarquía | `stages: → jobs: → steps:` | `jobs: → steps:` (sin stages) |
| Trigger main | `trigger: branches: include: [main]` | `on: push: branches: [main]` |
| Pull request | `pr: branches: include: [main]` | `on: pull_request: branches: [main]` |
| Cron | `schedules: - cron: '0 2 * * *'` | `on: schedule: - cron: '0 2 * * *'` |
| Pool/runner | `pool: vmImage: ubuntu-latest` | `runs-on: ubuntu-latest` |
| Setup .NET | `task: UseDotNet@2` | `uses: actions/setup-dotnet@v4` |
| Checkout | (automático) | `uses: actions/checkout@v4` |
| Deploy App Service | `task: AzureWebApp@1` | `uses: azure/webapps-deploy@v3` |
| Login Azure | `azureSubscription: 'Service-Connection'` | `uses: azure/login@v2` |
| Subir artifact | `publish: $(...) artifact: app` | `uses: actions/upload-artifact@v4` |
| Variable inline | `$(buildConfiguration)` | `${{ env.BUILD_CONFIGURATION }}` |
| Secreto | `$(StripeApiKey)` | `${{ secrets.STRIPE_API_KEY }}` |
| Job depends | `dependsOn: Build` | `needs: build` |
| Condición | `condition: succeeded()` | `if: success()` |
| Environment | `environment: production` | `environment: production` |

Tres reglas operativas que emergen de la tabla:

1. **GitHub Actions no tiene stages**. Donde en ADO agrupas Build / Test / Deploy_Staging / Deploy_Production como stages, en GitHub son jobs con `needs:` entre ellos.
2. **Checkout es explícito en GitHub**. ADO checkouta el código automáticamente; en GitHub lo pides explícitamente con `actions/checkout@v4`.
3. **Sintaxis de variables**: `$(var)` en ADO, `${{ var }}` en GitHub. Mecánicamente intercambiable.

La función `Buscar(concepto)` te permite consultar una equivalencia por nombre. Si no existe, devuelve `null` y el endpoint REST responde 404.

### `MigrationCostEstimator.Comparar` — la factura real

El cálculo, traducido a euros:

```csharp
public const decimal AdoUsuarioMes      = 6m;      // por usuario adicional
public const decimal AdoBasicGratisHasta = 5m;     // los primeros 5 gratis
public const decimal AdoTestPlansAddon  = 52m;     // por usuario/mes
public const decimal GhUsuarioMes       = 4m;      // por usuario desde el 1.º
public const decimal GhasUsuarioMes     = 49m;     // GitHub Advanced Security, ambas

// ADO:
//   usuariosBase = max(0, usuarios - 5) × 6
//   addons = (testPlans ? usuarios × 52 : 0) + (ghas ? usuarios × 49 : 0)
//   total = usuariosBase + addons

// GitHub:
//   usuariosBase = usuarios × 4
//   addons = ghas ? usuarios × 49 : 0
//   total = usuariosBase + addons
```

Tres escenarios típicos calculados:

| Escenario | ADO | GitHub | Más barato |
| --- | --- | --- | --- |
| 5 usuarios, básico | 0 € | 20 € | ADO (5 primeros gratis) |
| 10 usuarios, básico | 30 € | 40 € | ADO |
| 10 usuarios, básico + GHAS | 520 € | 530 € | ADO por 10 € (empate técnico) |
| 10 usuarios, básico + Test Plans | 550 € | 40 € | GitHub (no tiene Test Plans) |
| 20 usuarios, básico | 90 € | 80 € | GitHub |

Lecciones:

- **Hasta ~15 usuarios sin addons, ADO suele ser más barato** (los 5 primeros gratis pesan).
- **Con muchos usuarios, GitHub gana** (no hay base gratis, pero el precio por unidad es más bajo).
- **Test Plans dispara el coste de ADO** si lo activas para todos.
- **GHAS empata las dos plataformas** porque cuesta lo mismo.

Y la verdad menos obvia: **GHAS en GitHub se llama "Advanced Security" y cuesta lo mismo (49 €/u/mes)**. Si lo activas, el "GitHub es más barato" se va al traste.

### `PlatformPlanner` — el plan + checklist

El servicio inyectable. Combina los tres y devuelve plan completo: plataforma recomendada con razones, comparativa de coste, equivalencias clave si la decisión es migrar, y checklist específica según el camino (puro ADO, puro GitHub, híbrido, migración entre los dos).

---

## 6. El híbrido: qué es y cuándo merece la pena

La opción que más se ignora y que más casos reales cubre. **Híbrido = repos en GitHub + Pipelines y Boards en Azure DevOps**.

Cómo funciona:

- Los repos están en `github.com/miempresa/...`. La gente clona, hace PRs, revisa con la UI de GitHub. Beneficios: coherencia de marca pública, integración con GitHub Copilot, security scanning con CodeQL/Dependabot nativos.
- Los pipelines y los Boards están en `dev.azure.com/miempresa/...`. Los pipelines de Azure DevOps **pueden conectarse a un repo de GitHub** (configurando un service connection). Los work items y sprints siguen en ADO Boards.
- Los commits en GitHub triggerean pipelines en ADO. Los work items en ADO se vinculan a los PRs en GitHub. La integración es mejor de lo que parece.

Cuándo merece la pena:

- Tu equipo ya estaba en GitHub para repos y le da igual mover.
- Necesitas Boards con sprints serios (que GitHub Projects sigue sin tener bien).
- Quieres aprovechar GHAS sin pagar Advanced Security separado.

Cuándo no:

- Tu equipo es pequeño (≤5 personas) y no necesitas Boards complejos.
- Tu equipo ya hace todo en uno de los dos sitios sin problema.

El advisor detecta automáticamente el híbrido: si recibe señales fuertes en ambos lados (al menos una de ADO + una de GitHub), recomienda híbrido.

---

## 7. Cómo probarlo en local

```bash
dotnet run --project src/Plataforma.Demo.Api
# http://localhost:5108
```

Endpoints:

```http
### Elegir plataforma
POST http://localhost:5108/plataforma/elegir
Content-Type: application/json

{
  "yaUsasAdo": true,
  "necesitaBoardsCompletos": true,
  "quiereDependabotCodeQL": true,
  "personas": 8
}
# → Hybrid con razones (señales en ambos lados)

### Listar equivalencias YAML
GET http://localhost:5108/plataforma/equivalencias

### Buscar una equivalencia
GET http://localhost:5108/plataforma/equivalencia?concepto=trigger
# → { concepto: "Trigger en main", adoYaml: "...", gitHubYaml: "..." }

GET http://localhost:5108/plataforma/equivalencia?concepto=foo
# → 404

### Calcular coste mensual
POST http://localhost:5108/plataforma/coste
Content-Type: application/json

{ "usuarios": 10, "testPlans": false, "ghasOAdvancedSecurity": false }
# → { ado: { total: 30 }, github: { total: 40 }, masBarata: "ADO", ahorroMes: 10 }

### Plan completo
POST http://localhost:5108/plataforma/plan
```

Los 28 tests cubren los seis escenarios típicos del advisor (incluyendo el híbrido), las equivalencias clave (búsqueda exacta y por contención), los cálculos de coste con los tres addons posibles.

Para preflight de plataformas en tu PC:

```bash
./scripts/demo.sh
# 1) 01-preflight-platforms.sh → verifica az+azure-devops y gh CLI
```

Si tienes ambas instaladas y autenticadas, el híbrido es viable sin nada más que configurar.

> Yo no lanzo apps. Tú haces `dotnet run` y `dotnet test`.

---

## 8. La lección 20: "antes de migrar, define qué ganas"

La lección más importante del submódulo es operativa y no programable. Antes de cualquier migración entre plataformas, **escribe en una línea qué beneficio concreto y medible esperas obtener**. Si no puedes escribirla, no migres.

Ejemplos de líneas válidas:

- "Migramos a GitHub Actions para activar CodeQL en todos nuestros repos sin pagar el addon de Advanced Security separado".
- "Movemos los repos a GitHub para que el equipo de open source pueda contribuir vía PRs en la misma plataforma donde está su trabajo público".
- "Mantenemos ADO por los Boards de sprint con velocity tracking que necesitamos para el reporting trimestral a dirección".

Ejemplos de líneas que indican que NO debes migrar:

- "Migramos a GitHub Actions porque es más moderno".
- "Migramos a GitHub Actions porque todo el mundo lo usa ahora".
- "Migramos para tener todo en un sitio" (sin justificar qué pierdes en el otro).

El estimador de coste te ayuda a evaluar la primera línea. El advisor te dice si tu necesidad real es ADO, GitHub o híbrido. Las equivalencias te dicen cuánto curro es traducir tus pipelines.

---

## 9. Los anti-patterns operativos

Cinco prácticas que evitar:

**Anti-pattern 1 — Migrar sin objetivo medible**. La lección 20 hecha negación.

**Anti-pattern 2 — Boards en GitHub Projects para sprints serios**. GitHub Projects ha mejorado pero sigue por debajo de ADO Boards en sprint planning, velocity, burndown. Si lo necesitas, ADO o híbrido.

**Anti-pattern 3 — Comprar Test Plans sin haber justificado el coste**. 52 €/u/mes es muchísimo. Si tu QA puede vivir con Test Plans básico de Boards o con herramientas alternativas, no actives el addon.

**Anti-pattern 4 — Activar GHAS / Advanced Security por defecto**. 49 €/u/mes. Útil para apps críticas; injustificable para apps internas pequeñas. Decide por proyecto, no por organización completa.

**Anti-pattern 5 — Esconder el coste real al equipo**. Si tu equipo no conoce la factura mensual, no puede tomar decisiones informadas (ej.: "vamos a activar Test Plans para 8 personas" → 416 € extra al mes que aparecen en presupuesto). Transparencia.

---

## 10. Glosario breve

- **Azure DevOps (ADO)**: suite SaaS de Microsoft para DevOps (Repos, Boards, Pipelines, Test Plans, Artifacts).
- **GitHub Actions**: CI/CD nativo de GitHub. Más moderno en algunos aspectos, integrado con Dependabot, CodeQL.
- **GitHub Projects**: equivalente a ADO Boards pero más simple. Mejorando rápido pero todavía por debajo en sprint management.
- **GitHub Advanced Security (GHAS)**: addon de GitHub con CodeQL, secret scanning, dependency scanning. 49 €/u/mes.
- **GHAS for AzDO**: GHAS aplicable también a repos en Azure DevOps. Mismo precio.
- **Test Plans**: addon de ADO para gestión de tests manuales. 52 €/u/mes. Exclusivo de ADO.
- **Híbrido**: repos en GitHub + Pipelines+Boards en ADO. Patrón legítimo.
- **Service Connection**: credencial para que el pipeline acceda a Azure. Mismo concepto en ambas plataformas, distinta UI.
- **Marketplace / Actions**: catálogo de tasks reutilizables. ADO tiene "Marketplace" de tasks; GitHub tiene "Marketplace" de actions.
- **OIDC / Federated Identity**: forma moderna de autenticación pipeline → Azure sin secret almacenado. Disponible en ambas.
- **Azure DevOps Server**: versión on-premises. Sin equivalente en GitHub Actions (GitHub Enterprise Server existe pero es para GitHub general, no exclusivo de Actions).

---

## 11. Cierre

S8.4 es el submódulo de **decisión informada**. La conversación correcta no es "cuál es mejor" sino "cuál encaja con mi equipo, mi presupuesto y mis necesidades". El advisor te da tres salidas legítimas (ADO, GitHub, híbrido). El estimador te da la factura real. Las equivalencias YAML te dicen cuánto curro es migrar si decides hacerlo. Y la lección 20 te recuerda que migrar "por modernizar" sin objetivo medible es perder tiempo.

Lo siguiente es [`S8.5 — IaC con Bicep`](../S8.5-iac-bicep/MANUAL.md), donde la infraestructura como código se cubre con un linter local, parsing de salida de `what-if` y la integración real con `bicep build` que rompe la regla "sin packages" del módulo.
