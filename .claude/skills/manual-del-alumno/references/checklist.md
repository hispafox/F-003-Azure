# Checklist de validación del manual

Aplica este checklist antes de entregar el manual. Bloque por bloque,
reporta qué cumple y qué no. **El bloque J (escritura-humana) es
gating** — si falla, no entregues; reescribe y vuelve a pasar.

---

## A. Cabecera y meta-data

- [ ] **Título** con formato `# Manual del alumno — [identificador] · [Título]`.
- [ ] **Disclaimer "Esto no es el README"** con enlace al README y resumen del ángulo pedagógico del manual.
- [ ] **Tiempo de lectura** estimado (`~N min`).
- [ ] **Referencia a documento de teoría** (si existe).
- [ ] **Fecha de creación** en cursiva con formato `*Creado: YYYY-MM-DD HH:MM ±ZZZZ*` con hora real (capturada con `date "+%Y-%m-%d %H:%M %z"`, no inventada).
- [ ] **Separador `---`** entre cabecera y §1.

## B. Estructura (13 secciones)

- [ ] §1 La idea en una frase.
- [ ] §2 El problema real que hay detrás.
- [ ] §3 Por qué esto importa en tu stack.
- [ ] §4 La analogía vertebradora.
- [ ] §5-§7 Recorrido por el código (puede ser §5-§9 según el ejemplo).
- [ ] §8 Conversaciones críticas (si aplica).
- [ ] §9 Lección operativa específica (si aplica).
- [ ] §10 Cómo probarlo en local.
- [ ] §11 Anti-patterns.
- [ ] §12 Glosario breve.
- [ ] §13 Cierre con gancho al siguiente paso.

Las secciones intermedias §6-§9 son flexibles según el dominio; el
resto del esqueleto es fijo.

## C. Datos técnicos verificados contra el repo

- [ ] **Puerto exacto** de la app (de `launchSettings.json`, `package.json`, etc.).
- [ ] **Conteo de tests** verificado contra el repo.
- [ ] **Versión del runtime** (`global.json`, `runtime`, etc.) correcto.
- [ ] **Nombres de archivos** en fragmentos de código existen y están bien escritos.
- [ ] **Comandos** (`dotnet run`, `npm start`, `cargo build`...) son los que el repo soporta.
- [ ] **Endpoints** del `api.http` o equivalente existen y responden lo declarado.

Jamás supongas estos datos. Cázalos del filesystem antes de escribir.

## D. La analogía vertebradora

- [ ] §4 desarrolla la analogía en **3-5 párrafos** (no es una frase suelta).
- [ ] La analogía es **concreta y cotidiana** (hotel, fábrica, hospital, coche, mercado...) — no abstracta.
- [ ] La analogía se **refuerza** en secciones posteriores cuando ayuda a explicar una pieza concreta del código.
- [ ] El cierre de §4 invita a "mantener la imagen" mientras lee el resto.
- [ ] La analogía no se inventa por inventar — ilustra realmente la decisión técnica.

## E. Tono y voz (escritura-humana)

- [ ] §1 NO abre con callout-tesis-cápsula (`>` con la frase central del manual).
- [ ] Sin "Imagina que…", "En este capítulo veremos…", "Quédate con esta imagen…", "Tres cosas que entender…", "Vamos al lío…", "Aquí está el malentendido más común…", "Si tengo que resumir esto en una idea…".
- [ ] Sin tripletes simétricos (tres bullets de la misma longitud).
- [ ] Sin "no es X, es Y" en más de dos lugares por manual.
- [ ] Cero "es así. Punto." sentenciados a cada párrafo.
- [ ] Guion largo `—` no aparece más de 12-15 veces en todo el manual.
- [ ] §13 cierra sin "Si solo te llevas una frase…" ni "Hemos visto…".
- [ ] Tuteo consistente ("puedes hacer X", no "se puede hacer X").
- [ ] Ritmo asimétrico: nunca tres párrafos seguidos de la misma longitud.

## F. Estructura de markdown

- [ ] **Headings en texto plano**: cero cursiva, código o enlaces dentro de `#`/`##`/`###`.
- [ ] **Sin hard-wrap en prosa**: cada párrafo va en una sola línea del fuente. Saltos solo entre párrafos (línea en blanco) o en tablas, listas, bloques de código y headings.
- [ ] **Tablas bien formadas** con encabezado, separador y filas alineadas (no es obligatoria la alineación visual, pero sí la estructura).
- [ ] **Bloques de código** con lenguaje declarado (` ```csharp `, ` ```yaml `, ` ```bash `).
- [ ] **Enlaces relativos** correctos (`../X/MANUAL.md`, `(README.md)`, etc.).

## G. Honestidad didáctica

- [ ] Si el ejemplo NO tiene tests de integración, se explica por qué (no se omite).
- [ ] Si una decisión es "fuera de alcance" del ejemplo (ej. CMK, Bicep, etc.), se nombra y se anota dónde se cubrirá.
- [ ] No se inventan números, costes ni referencias a slides.
- [ ] Los "anti-patterns" del §11 son reales o documentados — no inventados para rellenar.

## H. Recorrido por el código (§5-§7)

- [ ] Cada pieza importante del código tiene su subsección.
- [ ] Los fragmentos de código son **reales** (copiados del archivo, no editados para parecer mejor).
- [ ] Se explica el **porqué** de cada decisión, no el qué literal.
- [ ] Se referencia la analogía vertebradora cuando ayuda.
- [ ] Si hay citas a slides/lecciones, son correctas.

## I. Enlaces y agregadores

- [ ] El **README del ejemplo** tiene un callout 📘 enlazando al MANUAL.md.
- [ ] El **README del módulo/sección agregadora** tiene el manual en su lista de manuales (formato según convención del repo).
- [ ] No se han modificado archivos fuera del ejemplo y su agregador inmediato.

## J. Test de la lista negra (gating)

Aplica `grep -niE` al manual con los patrones de `lista-negra.md`:

```bash
# Si el grep encuentra algo, hay que reescribir.
grep -niE 'gotcha|en definitiva|en resumen|en última instancia|por otro lado|cabe destacar|merece la pena destacar|no obstante|caso de uso' MANUAL.md
grep -niE 'imagina que|en este capítulo veremos|quédate con esta imagen|tres cosas que entender|vamos al lío' MANUAL.md
grep -niE 'estado del arte|con esteroides|modelos frontera|abrir la caja|throughline|caso de uso' MANUAL.md
grep -niE 'radicalmente|absolutamente|completamente' MANUAL.md   # intensificadores de relleno
grep -n '§' MANUAL.md   # silcrow no debe aparecer en headings ni en prosa
```

Si **alguno** devuelve resultado, **revisa caso por caso**: alguna
ocurrencia puede ser legítima (ej. "absolutamente" en una cita
literal), pero el grep debe disparar la pregunta. Sin disciplina aquí,
el manual sale a "documentación que rellena espacio".

### J.bis Conteo de guion largo (gating numérico)

Cuenta los `—` del manual con:

```bash
grep -oc '—' MANUAL.md
```

El umbral aceptable es **≤ 15** en todo el documento. Si el conteo
pasa, **bloquea la entrega** y aplica estas tres correcciones en orden
hasta bajar:

1. **Cambia `**Anti-pattern N — título**` por `**Anti-pattern N:
   título**`** (y lo mismo con cualquier `**Regla N — ...**`,
   `**Lección N — ...**`, etc.). Lee `lista-negra.md §4.bis` para el
   detalle. Esta sola corrección suele bajar 5-8 guiones de golpe.
2. **Sustituye guion largo entre incisos por paréntesis**:
   "X — aclaración — sigue" → "X (aclaración) sigue".
3. **Sustituye guion largo de conector por `:` o `,`**: "Y — es así"
   → "Y: es así" / "Y, es así".

**Por qué este bloque es gating numérico**: el guion largo es endémico
en traducciones mentales del inglés y la IA lo mete sin darse cuenta.
La primera ejecución real del skill (M09-S9.1) llegó a 29 guiones
antes de revisión. La métrica objetiva (`grep -oc`) hace el límite
auditable, no opinable.

## K. Test del café (gating)

Frase a frase, pregúntate:

> "Si estuviera tomando un café con un alumno después de clase y
> tuviera que explicarle esto que acabo de escribir, ¿lo diría así?"

Si la respuesta es "no, en un café diría algo más directo / menos
acartonado / con una pausa aquí / con menos palabras", **reescribe**.

Síntomas típicos de fallar el test:
- Frases largas con varias subordinadas anidadas.
- Vocabulario corporativo que nadie usa hablando ("a fin de cuentas
  optimizar el throughput").
- Tripletas o aforismos encadenados que nadie diría así en voz alta.
- Anuncios de transición ("Como hemos visto antes...").

## L. Cierre operativo

- [ ] El manual está completo (todas las secciones presentes).
- [ ] El grep de J está limpio.
- [ ] El conteo de guion largo (J.bis) está en ≤ 15.
- [ ] El test del café pasa.
- [ ] El callout en el README del ejemplo está añadido.
- [ ] El README agregador está actualizado si aplica.
- [ ] **No se ha hecho commit ni push**. El usuario decide cuándo subir.

---

## Cómo reportar el resultado de la validación

Cuando termines de aplicar el checklist, reporta al usuario:

```
Manual validado:
- ✅ A. Cabecera y meta-data
- ✅ B. Estructura (13 secciones)
- ✅ C. Datos técnicos verificados
- ✅ D. Analogía vertebradora (5 párrafos en §4, retomada en §6 y §8)
- ✅ E. Tono y voz (escritura-humana)
- ✅ F. Estructura de markdown
- ✅ G. Honestidad didáctica
- ✅ H. Recorrido por el código
- ⚠ I. Enlaces (pendiente añadir callout al README)
- ✅ J. Grep de lista negra limpio
- ✅ J.bis. Guion largo en 14 (umbral ≤ 15)
- ✅ K. Test del café superado
- ⏳ L. Pendiente commit/push (esperando tu OK)
```

Si J, J.bis o K fallan, **no entregues**; reescribe primero. El resto
pueden quedar pendientes según el flujo del usuario.
