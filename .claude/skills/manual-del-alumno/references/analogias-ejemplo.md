# Catálogo de analogías vertebradoras

Catálogo de analogías que han funcionado en 56 manuales pedagógicos
reales. Cada entrada lleva: el ámbito al que se aplica, la analogía
con sus elementos principales, cuándo encaja y cuándo no.

Usa este catálogo para **inspirarte**, no para copiar literal. Una
buena analogía nace de **la decisión técnica concreta del ejemplo**,
no de un catálogo. Pero el catálogo te quita la página en blanco.

---

## Cómo elegir analogía

Tres criterios para que la analogía vertebradora funcione:

1. **Concreta y cotidiana**: el alumno conoce la imagen sin
   explicación adicional. Hotel, restaurante, fábrica, hospital,
   coche, teatro, oficina, biblioteca, mercado, ascensor.
2. **Mapea bien a la decisión técnica**: cada elemento de la imagen
   tiene un correlato en el código. Si tienes que forzar el mapeo,
   busca otra.
3. **Tiene "drama" operativo**: la analogía permite contar qué pasa
   si las cosas van mal. Las analogías estáticas (un cuadro, un
   monumento) no aguantan §11 (anti-patterns).

Cuándo dudar: si la analogía exige más de un párrafo para introducirla
porque el alumno no la entiende, no es lo bastante cotidiana. Cámbiala.

---

## Catálogo por ámbito

### Cloud y almacenamiento

**El edificio de cuatro inquilinos** (multi-tenancy en cloud)
- Elementos: edificio = cuenta cloud, plantas = entornos, pisos = tenants, paredes = aislamiento.
- Encaja: storage compartido, Cosmos multi-tenant, App Service con varios slots.
- Cuándo no: cuando todo es single-tenant trivial.

**El archivador** (storage de archivos, blob, file storage)
- Elementos: archivador = storage account, cajones = containers, carpetas = folders, etiquetas = metadata, cerradura = SAS / RBAC.
- Encaja: ejemplos de Azure Storage, S3, GCS — cualquier servicio de archivos.
- Cuándo no: cuando el tema es transaccional (BD) y no de archivo.

**La biblioteca con sucursales** (cosmos / multi-region)
- Elementos: biblioteca central = región primaria, sucursales = read replicas, catálogo = índice, ficha del libro = documento.
- Encaja: distribución multi-region, Cosmos DB, read replicas.

**El almacén y los camiones** (cifrado at-rest e in-transit)
- Elementos: almacén = at-rest, camiones = in-transit, candado = cifrado, llave = clave de cifrado, garante (banco) = KMS.
- Encaja: seguridad de datos, cifrado, claves CMK vs MMK.

**La caja fuerte del banco vs llave bajo el felpudo** (Key Vault vs secretos en config)
- Elementos: caja fuerte = Key Vault, llaves bajo el felpudo = secrets en config, gerente del banco = RBAC, vigilante = audit log.
- Encaja: gestión de secretos, Key Vault, AWS Secrets Manager.

### Backend, mensajería y arquitectura

**La centralita de cartas certificadas** (Service Bus, mensajería, queues)
- Elementos: centralita = broker, sobres = mensajes, bandejas = colas, oficinistas = consumidores, etiquetas = topics/filters.
- Encaja: Service Bus, RabbitMQ, SQS, Kafka cuando es mensajería pura.

**El coro y la sinfónica** (event-driven: choreography vs orchestration)
- Elementos: sinfónica = orchestration (Durable Functions, Step Functions), coro a capella = choreography (eventos sin coordinador).
- Encaja: Sagas, transacciones distribuidas, orquestación.

**El control de aduanas** (API Gateway, APIM, Kong)
- Elementos: aduanas = gateway, pasaporte = auth, cuotas = rate limiting, blacklists = IP filtering, sello = transformations.
- Encaja: APIM, AWS API Gateway, Kong, cualquier gateway.

**La línea de montaje del coche** (pipelines de procesamiento, batch jobs)
- Elementos: estaciones = stages, robots = workers, control de calidad = validation, bandejas = queues entre stages.
- Encaja: ETL, batch processing, pipelines de datos, CI/CD también.

**La oficina con cuatro puertas** (Functions con varios triggers)
- Elementos: oficina = Function App, puertas = triggers (HTTP, Timer, Queue, Blob), oficinistas = handlers.
- Encaja: serverless multi-trigger, Cloud Functions, Lambda con varios eventos.

**El director de orquesta** (Durable Functions, Step Functions, workflows)
- Elementos: director = orquestador, músicos = activities, partitura = workflow, ensayo = replay.
- Encaja: Durable Functions, Temporal, Cadence.

### Seguridad e identidad

**El contrato del hotel** (responsabilidad compartida cloud)
- Elementos: hotel = proveedor cloud, habitación = inquilino, llave maestra = control de plataforma, llave de habitación = control del cliente, contrato = responsabilidad compartida.
- Encaja: explicar responsabilidades compartidas, modelo SaaS/PaaS/IaaS.

**El carnet de identidad y los pases del edificio** (identidad y permisos)
- Elementos: carnet = identidad, pases = permisos por área, recepción = identity provider, vigilante = autorización.
- Encaja: Entra ID, Cognito, IAM, cualquier sistema de identidad.

**El portero y la entrega del paquete** (OAuth2, autenticación delegada)
- Elementos: portero = authorization server, paquete = token, código de acceso = code, llave del paquete = refresh token.
- Encaja: OAuth2, OIDC, tokens y refresh.

**Las llaves del piso compartido** (auth multi-modal en desktop, MSIX)
- Elementos: llaves = métodos de auth, piso = recurso, compañeros de piso = otras apps, candado nuevo = re-auth.
- Encaja: auth desktop, MSIX, multi-factor.

**El triaje de urgencias** (clasificación de errores, retry policies)
- Elementos: triaje = clasificador de error, código verde = transitorio, código rojo = permanente, sala de espera = retry queue, UCI = dead-letter.
- Encaja: error handling, Polly, retry/circuit breaker.

### Desktop y distribución

**La mudanza de oficina** (migración ClickOnce → MSIX, cambio de plataforma)
- Elementos: oficina vieja = formato antiguo, oficina nueva = formato nuevo, mudanza = migración, muebles = código existente.
- Encaja: migraciones grandes, cambios de plataforma, evoluciones legacy.

**El pasaporte y el embarque** (empaquetado, identidad de paquete, firma)
- Elementos: pasaporte = manifest, sellos = certificados, control de seguridad = validación, terminales = canales de distribución.
- Encaja: MSIX manifest, code signing, app stores.

**Las actualizaciones del coche conectado** (auto-update, canary, rollback)
- Elementos: coche = app desplegada, fabricante = sistema de updates, etapa de pruebas = canary, marcha atrás = rollback.
- Encaja: auto-update, OTA, deployment strategies.

**La mudanza por fases** (migración por fases con criterios de salida)
- Elementos: cajas etiquetadas = stage 1, casa piloto = stage 2, mudanza general = stage 3, casa vieja = legacy a apagar.
- Encaja: migraciones planificadas, blue/green, ring deployments.

**La receta de cocina con tiempos** (workflow paso a paso con criterios)
- Elementos: receta = workflow, ingredientes = inputs, control de cocción = criterios, plato final = entregable.
- Encaja: prácticas guiadas con criterios de salida, runbooks.

**El cambio automático vs manual** (Wizard vs CLI, abstracción vs control)
- Elementos: cambio automático = wizard/UI, cambio manual = CLI, motor = lo que hace por debajo, conductor experto = ingeniero senior.
- Encaja: comparativa de herramientas wizard vs CLI, dev tools.

### DevOps y CI/CD

**La oficina compartida y sus normas** (Azure DevOps Repos/Boards, convenciones)
- Elementos: oficina = repo, normas = branch policies, etiquetas en archivadores = Conventional Commits, agenda = work items.
- Encaja: convenciones de equipo, Git workflows, gestión de proyectos.

**La cadena de montaje** (pipelines CI/CD, jobs, stages)
- Elementos: cadena = pipeline, estaciones = stages, robots = agents, control = approvals.
- Encaja: cualquier pipeline lineal con etapas.

**El cambio de neumáticos coche normal vs F1** (deployment strategies)
- Elementos: coche normal = direct deploy, coche F1 = slot swap, garaje = staging slot, pista = producción.
- Encaja: blue/green, slot swap, hot deployment.

**Dos restaurantes del mismo dueño** (comparativa de plataformas similares)
- Elementos: restaurante clásico = plataforma A, restaurante moderno = plataforma B, mismo dueño = mismo vendor, públicos distintos = casos de uso.
- Encaja: ADO vs GitHub Actions, AWS vs Azure, productos hermanos.

**El plano del arquitecto** (Infrastructure as Code, Bicep, Terraform)
- Elementos: plano = archivo IaC, estudio de cambios = what-if, demolición = delete recursos, arquitecto = ingeniero IaC.
- Encaja: IaC, Bicep, Terraform, CloudFormation.

**El monitor de constantes vitales** (observabilidad, alertas, runbook)
- Elementos: monitor = App Insights, pulso = latencia, saturación O2 = tasa de error, alarmas = alertas, protocolo médico = runbook.
- Encaja: observabilidad, monitoring, SRE.

**La entrega de un producto en una tienda con tres controles** (CI/CD con gates)
- Elementos: almacén = build, vitrina interna = staging, estantería pública = producción, gerente que aprueba = approvers.
- Encaja: pipelines con gates múltiples, deployment con aprobación.

**Las dos llaves de la casa** (Publish profile vs OIDC, métodos de auth)
- Elementos: llave física = secret, sistema biométrico = OIDC, portero = environment con reviewers.
- Encaja: cualquier comparativa "longeva vs federada" de credenciales.

### Patrones generales

**El restaurante con dos cocinas** (versionado de API, compatibilidad)
- Elementos: cocina vieja = v1, cocina nueva = v2, mismos ingredientes = mismo backend, presentación distinta = contrato distinto.
- Encaja: API versioning, breaking changes, retrocompatibilidad.

**El ascensor con varias plantas** (escalado, layered architecture)
- Elementos: ascensor = capa de coordinación, plantas = layers, botones = APIs, contrapeso = caché.
- Encaja: arquitectura por capas, escalado vertical/horizontal.

**El servicio postal con apartado de correos** (Event Grid, fan-out, push vs pull)
- Elementos: apartado = subscription, cartas = events, oficina central = Event Grid, repartidor = webhook.
- Encaja: Event Grid, pub/sub, webhooks.

**El control de aduanas** (validación, rate limiting, ya mencionada arriba)

**El conserje del edificio** (Easy Auth, App Service Authentication)
- Elementos: conserje = Easy Auth, residentes = users, pase = token, oficina del residente = tu API.
- Encaja: Easy Auth, Cognito, auth automatizada de plataforma.

### IA y agentes

**El contratista de obras vs el manitas con destornillador** (Claude Code vs Copilot, agente vs autocompletado)
- Elementos: manitas con destornillador = Copilot (te ayuda mientras tú haces la tarea pequeña), contratista de obras = Claude Code (hace la reforma entera mientras tú revisas), contrato firmado = `settings.json`, partes vedadas de la casa = `excludePatterns`, supervisor en obra = hooks `PreToolUse`/`PostToolUse`, formas de contratar = los 4 modos (interactive/one-shot/pipe/headless).
- Encaja: agentes de IA en general (Claude Code, Cursor, Aider), comparativas agente vs autocompletado, gobierno del agente con permisos y hooks.
- Por qué funciona: mapea limpiamente CC vs Copilot, los 4 modos de ejecución, el rol del `settings.json` y los hooks como "controles intermedios". Permite además explicar anti-patterns ("entrar sin contrato firmado = `rm -rf`") y la conversación con seguridad ("qué deja entrar a casa, qué no").

**El becario espabilado al que le delegas tareas** (uso responsable de un agente)
- Elementos: becario = agente, tu tiempo = contexto del LLM, lista de tareas = prompts, review final = lectura del diff, formación = system prompt + skills.
- Encaja: cuando el ángulo es la productividad humana y el cambio cultural ("delegar bien" como habilidad). Útil para hablar de "qué se delega y qué no".
- Cuándo NO: si el ejemplo es más técnico (instalación, settings.json), el contratista funciona mejor por las metáforas de "obra" y "contrato".

---

## Recetas para crear analogías nuevas

Cuando el catálogo no tenga la analogía perfecta para tu ejemplo, sigue
estas recetas:

### Receta 1: del problema operativo a la imagen

1. Pregúntate: ¿cuál es el peor escenario operativo si el ejemplo se
   hace mal?
2. ¿En qué situación cotidiana ocurre algo similar?
3. ¿Qué actores tiene esa situación cotidiana? Mapéalos a los
   componentes técnicos.

Ejemplo: el peor escenario de un sistema sin sampling de logs es la
factura sorpresa. Eso pasa en cotidiano cuando vas a un restaurante y
pides sin mirar la carta de precios. Mapea: comensales = requests,
camarero = telemetría, cuenta final = factura cloud, presupuesto =
daily cap.

### Receta 2: del rol técnico al rol humano

1. ¿Cuál es el componente protagonista del ejemplo? (Un orchestrator,
   un gateway, un workflow engine).
2. ¿Qué rol humano hace lo mismo en una organización? (Director de
   orquesta, portero, capataz, despachador).
3. Construye la analogía desde ese rol.

Ejemplo: un orchestrator de Durable Functions coordina activities como
un director de orquesta coordina músicos.

### Receta 3: del problema de comunicación al sistema

1. ¿Tu ejemplo trata sobre cómo se comunican dos o más cosas?
2. ¿Cómo se comunican dos personas o instituciones en cotidiano para
   hacer lo mismo?
3. Mapea actores, mensajes y reglas de comunicación.

Ejemplo: para "OAuth2", el protocolo de comunicación entre cliente,
authorization server, resource server y usuario es como la entrega de
un paquete que requiere identificación: cartero, portero, código de
recogida y firma.

---

## Las analogías que NO funcionaron

Aprendido en 56 manuales. Patrones de analogía que prueba ahorrar:

- **Analogías biológicas demasiado abstractas** ("el sistema nervioso
  central de la app"). Suena bonito pero no mapea bien.
- **Analogías de ciencia-ficción** ("como un agente Smith de Matrix").
  Date un margen: no todos los alumnos conocen la referencia.
- **Analogías militares** ("la batalla por el throughput", "atrincherar
  el endpoint"). El tono se vuelve agresivo y muchos alumnos lo
  rechazan.
- **Analogías deportivas muy locales** ("como el pase del 10 al 9").
  Sólo funcionan si el lector conoce el deporte.
- **Analogías esotéricas o de magia** ("el hechizo que invoca la
  función"). Suena infantil en contexto profesional.
- **Analogías políticas o religiosas**. Evítalas siempre.

Las que **sí** funcionan son las domésticas y profesionales neutras:
hotel, oficina, restaurante, cocina, fábrica, hospital, coche, teatro,
biblioteca, mercado, taller, almacén, mudanza.

---

## Cómo se nota una analogía bien hecha

Tres síntomas operativos:

1. **El §4 se lee solo** sin tener que volver atrás. La imagen se
   forma en la cabeza del lector sin esfuerzo.
2. **Se referencia naturalmente en §5-§9** cuando se explica una pieza
   concreta. "Esta función es el conserje preguntando..." vs "esta
   función comprueba la lista de permitidos".
3. **El §11 (anti-patterns) usa la analogía** para ilustrar qué pasa
   si las cosas van mal. "Es como si el conserje dejara entrar a
   cualquiera sin pedir carnet".

Si los tres síntomas se dan, la analogía está bien elegida. Si te
cuesta integrarla en §5-§9 o no funciona en §11, **cámbiala antes de
seguir** — es más barato cambiarla en el primer borrador que reescribir
el manual entero al final.
