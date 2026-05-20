# Plantilla del manual del alumno

Esta plantilla es **adaptable**: el esqueleto de 13 secciones es fijo,
pero el contenido de cada una se adapta al tema del ejemplo. Las
secciones técnicas (§5-§9) son las que más varían según el dominio
(cloud, backend, frontend, ML, etc.).

Las indicaciones entre `[corchetes]` son guía para el redactor; no van
en el manual final.

---

```markdown
# Manual del alumno — [SX.Y · Título descriptivo del ejemplo]

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica:
[lo que tenga el README del ejemplo — estructura, comandos, despliegue].
Este manual va antes: te cuenta [el ángulo pedagógico — qué decisión
silenciosa entrena este ejemplo, qué problema operativo justifica que
exista, por qué la analogía vertebradora ayuda a entenderlo].

Tiempo de lectura: ~[N] min. [Si hay documento de teoría:
Submódulo/lección de referencia: [enlace].] [Resumen de las piezas
principales — N piezas de lógica X, N tests, integraciones reales o
ausencia justificada].

*Creado: [YYYY-MM-DD HH:MM ±ZZZZ — captúralo con `date "+%Y-%m-%d %H:%M %z"`]*

---

## 1. La idea en una frase

[Una afirmación con voz de formador que resume el "para qué" del
ejemplo. Cero callout-tesis-cápsula. Cero "Imagina que…". Cero
"En este capítulo…". Empieza por la afirmación, no la anuncies.]

[Segundo párrafo opcional: el matiz de la idea, una consecuencia
inmediata, una pista de por dónde va el ejemplo.]

---

## 2. El problema real que hay detrás

[Aquí va la **historia ancla** — el desastre, el caso del alumno, el
descubrimiento. Una a tres situaciones reales o plausibles que
justifican por qué este ejemplo existe. Formato típico: tres "casos"
numerados, cada uno con su contexto, su detalle concreto y su lección.
Mezcla largos densos con cortos.]

**Caso 1 — [título descriptivo].** [Contexto + problema concreto +
solución correcta + lección aprendida. 4-6 frases. Si hay números
concretos (minutos, días, coste), inclúyelos — dan textura real.]

**Caso 2 — [título descriptivo].** [Otro caso, distinto enfoque o
distinta arista del mismo problema.]

**Caso 3 — [título descriptivo].** [Tercer caso. Si no tienes tres,
dos también valen. Mejor dos buenos que tres con uno débil.]

[Cierre del bloque que enlaza con los conceptos del ejemplo: "Los tres
casos los previene el ejemplo: <pieza A> hace X, <pieza B> hace Y,
<pieza C> hace Z."]

---

## 3. Por qué esto importa en tu stack

[Conecta con el alumno: ¿en qué situación de su trabajo real va a
aplicar esto? Tres preguntas concretas que el ejemplo le ayuda a
responder. Sin "Como vimos en el módulo anterior…". Sin "ya conoces
esto, ahora vas a…".]

Tres preguntas que conviene tener resueltas:

- **¿[Pregunta operativa concreta]?** [Una frase con la respuesta o
  con el matiz que el ejemplo resuelve.]
- **¿[Segunda pregunta]?** [Idem.]
- **¿[Tercera pregunta]?** [Idem.]

[Cierre: si tiene las respuestas, su sistema X; sin ellas, problema Y.]

---

## 4. La analogía vertebradora: [nombre concreto]

[Aquí va la **analogía vertebradora desarrollada en 3-5 párrafos**.
Concreta y cotidiana: hotel, edificio, fábrica, hospital, coche,
teatro, mercado, oficina, mudanza, monitor de UCI, almacén con
camiones, plano del arquitecto, director de orquesta, restaurante,
ascensor, cocina, biblioteca, taller mecánico, control de aduanas...
Lee `analogias-ejemplo.md` para inspiración.]

[Primer párrafo: presentación de la imagen mental. Ya describes el
escenario, no anuncias "vamos a usar una analogía". Solo "Imagina un
hotel..." (lo cual está prohibido — empieza directamente: "Un hotel
funciona así: ...").]

[Segundo párrafo: mapeo de la analogía a los conceptos técnicos del
ejemplo. Cada elemento de la imagen mental se mapea a un componente.
No hagas el mapeo en una lista bullet — hazlo en prosa fluida.]

[Tercer párrafo: detalles operativos de la analogía que ayudan a
entender decisiones del ejemplo. "El conserje no abre la puerta de
fuera; eso es del portero. El conserje gestiona el ascensor..."]

[Cuarto párrafo opcional: anti-ejemplo en la analogía — qué pasaría
si el hotel funcionara mal, qué error operativo equivale al
anti-pattern del ejemplo.]

[Cierre del §4: "Mantén la imagen mientras lees el código. El conserje
es X, el portero es Y, los huéspedes son Z, las habitaciones son W."
Este cierre activa la imagen para los siguientes §.]

---

## 5. Recorrido por el código

[Sección variable según el ejemplo. Suele ser 3-5 subsecciones, una
por pieza principal de código (clase, función pura, módulo). Cada
subsección incluye:
- Nombre del archivo y propósito en una línea.
- Fragmento de código relevante (5-20 líneas).
- Explicación de por qué la pieza está así, no qué hace literalmente.
- Si aplica: referencia a la analogía vertebradora.
- Si aplica: cita a la teoría/slide.]

### [Nombre de la pieza 1]

[Propósito en una frase. Por qué importa.]

```[lenguaje]
[fragmento de código real, verificado contra el repo]
```

[Explicación de la decisión de diseño, no del literal del código.
Conecta con la analogía: "Esta función es el conserje preguntando..."]

### [Nombre de la pieza 2]

[Idem. 3-5 piezas en total.]

---

## 6. [Sección técnica específica del ejemplo]

[Aquí puedes tener varias secciones técnicas según el ejemplo:
algoritmos, decisiones críticas, comparativas, datos numéricos,
configuraciones. Cada una con tabla si es una comparativa, prosa de
formador si es una explicación.]

[Ejemplos según el dominio:
- Para un ejemplo de cloud: comparativa de servicios o tiers.
- Para un ejemplo de seguridad: análisis de amenazas o threat model.
- Para un ejemplo de ML: hiperparámetros y trade-offs.
- Para un ejemplo de backend: trade-offs de arquitectura.
- Para un ejemplo de frontend: comparativa de patrones de UI.]

---

## 7. [Otra sección técnica si aplica]

[Igual que §6. Adapta según el tema.]

---

## 8. La conversación con [el equipo / seguridad / producto / IT]

[Esta sección recoge **conversaciones críticas** que el ejemplo deja
abiertas. Típicamente:
- "La conversación con seguridad sobre X" (cómo defender una decisión
  de diseño ante revisión de seguridad).
- "La conversación con el equipo sobre Y" (un debate clásico que el
  ejemplo resuelve o ilumina).
- "La conversación con producto sobre Z" (cómo explicarle a un product
  manager una decisión técnica con impacto en UX).

Prosa de formador con opinión y matiz. No bullets simétricos; argumentos.]

---

## 9. [Lección operativa específica si aplica]

[Si el ejemplo tiene una lección operativa que se merece sección
propia (ej: "la regla del incremento de versión", "el patrón del
correlation ID", "la decisión sticky settings"), aquí. Si no, sálta
esta sección — no es obligatoria.]

---

## 10. Cómo probarlo en local

[Comandos exactos, verificados contra el repo. Suele ser:
- Cómo arrancar (`dotnet run`, `npm start`, `cargo run`, etc.).
- Qué endpoints/comandos probar.
- Qué tests ejecutar y qué cubren.
- Si hay scripts para Azure/AWS/etc., cómo invocarlos.]

```bash
[comando de arrancar el ejemplo]
# [puerto u output esperado, verificado contra el repo]
```

Endpoints/comandos para jugar:

```[lenguaje]
[ejemplos concretos verificados]
```

Los [N] tests cubren [resumen breve de qué cubren las capas].

[Si el repo tiene scripts adicionales:]
Para [tarea específica]:

```bash
[comando del script]
# [explicación de qué hace, solo lectura o destructivo]
```

> [Recordatorio de la regla del usuario si aplica al proyecto:
> "Yo no lanzo apps. Tú haces X y Y."]

---

## 11. Anti-patterns

[Lista de prácticas a evitar. Cada anti-pattern con su porqué operativo.
Suelen ser 3-5. Numerados, con título descriptivo, una explicación
breve del problema y la solución correcta.]

**Anti-pattern 1 — [título].** [Problema concreto + por qué es malo +
qué hacer en su lugar.]

**Anti-pattern 2 — [título].** [Idem.]

**Anti-pattern N — [título].** [Idem.]

---

## 12. Glosario breve

[Términos del dominio que aparecen en el manual, con definición de una
línea. No es una enciclopedia; son las palabras que un alumno nuevo
necesita conocer para seguir el resto del módulo.]

- **[Término]**: [definición de una línea].
- **[Término]**: [definición de una línea].
- **[N términos en total — entre 8 y 15 según la densidad del ejemplo].**

---

## 13. Cierre

[Sin recap. Sin "Si solo te llevas una frase...". Sin "Hemos visto...".
Una afirmación con voz de formador que cierra con matiz o con gancho
al siguiente paso. 2-3 párrafos.]

[Primer párrafo: matiz que se ganó el manual — algo que ahora el
alumno puede defender en su trabajo.]

[Segundo párrafo opcional: gancho al siguiente ejemplo o submódulo
con un enlace concreto.]

Lo siguiente es [`SX.Y — Título`](../SX.Y-slug/MANUAL.md), donde
[anticipo de una frase].
```

---

## Notas para el redactor (no van en el manual)

- **Adaptación por dominio**:
  - **Cloud**: §6-§7 suelen ser comparativas de servicios + costes; §8 conversación con seguridad o FinOps; §11 anti-patterns operativos.
  - **Backend**: §6-§7 patrones de arquitectura + decisiones de diseño; §8 conversación con el equipo sobre escalado; §11 anti-patterns de código.
  - **Frontend**: §6-§7 patrones de UI + accesibilidad; §8 conversación con producto o UX; §11 anti-patterns visuales.
  - **ML/AI**: §6-§7 hiperparámetros + trade-offs; §8 conversación con producto o legal; §11 anti-patterns de modelado.
  - **DevOps**: §6-§7 piezas del pipeline + decisiones; §8 conversación con seguridad o operaciones; §11 anti-patterns de pipeline.
  - **Seguridad**: §6-§7 amenazas + mitigaciones; §8 conversación con compliance; §11 anti-patterns clásicos.

- **Tamaño orientativo**: 300-450 líneas de markdown. Sin
  hard-wrap, así que 300 líneas son ~5500 palabras.

- **Análisis de la analogía vertebradora**: dedica 3-5 párrafos. Es la
  pieza pedagógica más valiosa del manual y la que justifica el doble
  documento (manual ≠ README).

- **Cierre con gancho al siguiente paso**: si el ejemplo es parte de
  una serie, enlaza al siguiente. Si es el último, enlaza al cierre
  del módulo o al próximo módulo.

- **Citas a teoría**: usa `Slide N` o `lección Y` cuando el documento
  de teoría las tiene. Si no hay teoría documentada, omítelas — no
  inventes referencias.
