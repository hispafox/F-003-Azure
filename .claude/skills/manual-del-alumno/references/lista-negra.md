# Lista negra del manual del alumno

Patrones que NO deben aparecer en el manual final. Cada uno con su
porqué y su alternativa.

Esta lista es complementaria a la `lista-negra.md` del skill
`escritura-humana` (si está instalado). Las dos se solapan; las dos
aplican.

---

## 1. Anuncios de transición y etiquetas-de-sección

**Prohibidos**:
- "Imagina que..." / "Imagínate..." / "Pongamos por caso..."
- "En este capítulo / submódulo / manual veremos..."
- "Quédate con esta imagen..."
- "Vamos al lío..."
- "Aquí está el malentendido más común..."
- "Si tengo que resumir esto en una idea..."
- "Tres cosas que entender, no solo copiar:"
- "Lo importante de esa frase es:"
- "Como vimos en el módulo anterior..."

**Por qué**: anuncian lo que viene en lugar de presentarlo. Sustituyen
la voz del formador por una voz de manual de instrucciones.

**Alternativa**: empieza por la afirmación directamente. Si vas a usar
una analogía, **descríbela**, no la anuncies.

❌ "Imagina un hotel donde los conserjes..."
✅ "Un hotel con buen servicio funciona así: los conserjes..."

---

## 2. Frase-cápsula como entrega por defecto

**Prohibido**: usar la fórmula "No es A: es B." / "Esto es Y. Punto." /
"La respuesta corta es X." más de una vez por manual.

**Por qué**: una cápsula bien colocada en su clímax es potente. Una por
sección es jerga de plantilla. Suena a tweet, no a libro.

**Alternativa**: desarrolla la idea con su matiz, su porqué, su
subordinada. La cápsula se gana por contexto, no por estructura.

❌ "El error #1 es éste. Punto."
✅ "El error que aparece más veces en revisiones reales es éste — y la
razón tiene más historia de la que parece..."

---

## 3. Tripletes simétricos

**Prohibido**: tres bullets de la misma longitud con la misma estructura
gramatical, especialmente cuando enumeras "tres cosas a entender".

**Por qué**: la simetría perfecta es firma de IA. Los humanos solemos
romper el ritmo (uno largo + uno corto + un aparte; o dos cosas y un
matiz).

**Alternativa**: si tienes tres ideas, prueba con dos largas + una
breve, o una idea bien desarrollada + dos notas.

❌
```
- Es rápido: 5ms por request.
- Es seguro: encripta con AES-256.
- Es barato: 0.50€/M operaciones.
```

✅
```
- Es rápido (5ms por request) y, sobre todo, predecible — el percentil
  99 también está en 5ms, no en 200ms como otras opciones.
- A cambio cuesta 0.50€/M operaciones, lo cual escala mal si tienes
  millones diarios.
```

---

## 4. Aforismos en bucle y guion largo

**Prohibido**: más de dos veces "no es X, es Y" en una página. Más de
una tripleta encadenada por sección. Guion largo `—` más de 12-15
veces en todo el documento.

**Por qué**: cada uno es legítimo. En bucle son tics de IA.

**Alternativa**: desármalo. Convierte el segundo en frase normal.

### 4.bis Patrón de heading que infla el conteo de guion largo

**Prohibido en exceso**: cabeceras estilo `**Tema N — título.**` o
`**Caso N — título.**` repetidas a lo largo del manual.

**Por qué**: cada anti-pattern, cada caso de uso y cada subsección que
adopte este patrón añade un guion largo. En un manual con 5 anti-patterns
+ 3 casos + 3 subsecciones técnicas tienes 11 guiones consumidos solo
en headings, antes de empezar la prosa. En la primera ejecución real
del skill (S9.1 de M09) el manual se fue a 29 guiones largos
exactamente por este efecto.

**Alternativa**:
- Para listas numeradas tipo "anti-pattern", "regla", "lección":
  cambia `— ` por `: `. Mismo efecto visual, cero guion largo. Ej:
  `**Anti-pattern 1: empezar sin settings.json.**`
- Para casos ancla del §2 (historia del desastre / del alumno) sí
  puedes mantener `— `, porque suele haber pocos (2-4) y el guion
  ayuda al ritmo narrativo.
- Para subsecciones técnicas `### NombrePieza — descripción`, también
  se aceptan porque son 3-5 y aportan claridad.

**Regla de pulgar**: cuenta a ojo los headings con `— ` antes de escribir
prosa. Si pasas de 8 entre cabeceras, cambia los anti-patterns y
similares a `:` para reservar el cupo del guion largo para la prosa,
donde tiene más valor expresivo.

---

## 5. Calcos del inglés

**Prohibidos**:
- "con esteroides" / "con esteroides X"
- "estado del arte" / "state of the art"
- "modelos frontera" / "frontier models"
- "abrir la caja" / "abrir la caja negra"
- "throughline" (sí, aunque sea inglés directo en cursiva)
- "caso de uso" / "casos de uso" (decir "tarea", "sitio donde encaja",
  "escenario")
- "ideas metidas" / "ideas embebidas"
- "limpiar ideas" ("aclarar", "depurar", "ordenar")
- "X que aguanta toda la frase"
- "ride or die"
- "low-hanging fruit" → "lo más fácil"
- "out of the box" → "por defecto", "tal cual"
- "deal-breaker" → "no negociable"
- "gotcha" / "gotchas" → "trampa", "pega"
- "session" en español hablando de "sesión" en sentido amplio
- "feature flag" sí es aceptable (no hay calco español natural)

**Por qué**: rompen el registro español natural. Algunos son palabras
que en inglés son normales pero en español suenan acartonadas o
extrañas.

---

## 6. Intensificadores de relleno

**Prohibidos**:
- "radicalmente + adjetivo"
- "absolutamente + adjetivo"
- "completamente + adjetivo"
- "literalmente + verbo" (cuando no es literal)
- "totalmente + adjetivo"

**Por qué**: no añaden información, solo enfatizan sin causa. La IA
los usa para subir el ritmo; en humano se nota artificial.

**Alternativa**: o quítalos, o sé concreto con un número, o ensucia
con coloquialismo controlado según registro.

❌ "Esta solución es radicalmente diferente."
✅ "Esta solución toma otro camino: en lugar de A, hace B."
✅ "Esta solución cambia el enfoque por completo." (si quieres
mantener el énfasis sin "radicalmente").

---

## 7. Notas de producción en la prosa

**Prohibido**:
- "Esto es una slide propia."
- "Criterio de éxito del bloque..."
- "[ADAPTAR]" (placeholder de plantilla)
- "Aquí va una imagen / un diagrama / un ejemplo"
- Referencias internas a "este skill", "la plantilla", "el checklist"

**Por qué**: el alumno no debe ver el andamiaje del manual. Las notas
para el redactor se quedan en el archivo de plantilla, no llegan al
manual final.

---

## 8. Símbolo silcrow `§`

**Prohibido en el manual**: no usar `§` en headings ni en prosa.

**Por qué**: rompe el anclaje en algunos previews de markdown y se ve
como ruido tipográfico para el alumno.

**Alternativa**: "sección 5", "apartado 3", o simplemente "más
arriba", "abajo".

---

## 9. Cierres con recap

**Prohibido en §13 (cierre)**:
- "Si solo te llevas una frase del manual..."
- "Hemos visto..."
- "En este manual hemos cubierto..."
- "Para resumir..."
- "En resumen..."

**Por qué**: el alumno acaba de leer el manual. No necesita un resumen
de lo que acaba de leer; necesita un gancho a lo siguiente, una
provocación que invite a aplicar, o un matiz que se ganó la lectura.

**Alternativa**: pregunta abierta, gancho al siguiente ejemplo,
recordatorio operativo, una idea que el alumno se llevará.

❌ "Hemos visto las tres puertas del pipeline. Si te quedas con una
cosa, que sea ésta..."
✅ "Si tu primer pipeline en producción pasa por estas tres puertas,
no vas a tener el incidente del viernes por la tarde. Si te saltas
alguna, sí. Lo siguiente es [SX.Y — Título]."

---

## 10. Voz pasiva y "se" impersonal

**Prohibido (en exceso)**:
- "Se puede realizar X."
- "Se debe configurar Y."
- "Por el sistema es ejecutado..."

**Por qué**: distancian al alumno. La voz del manual es directa, tú.

**Alternativa**: tutea.

❌ "Se puede ejecutar el comando..."
✅ "Puedes ejecutar el comando..."

---

## 11. Anglicismos no necesarios

**Prohibidos cuando hay alternativa española natural**:
- "Performance" → "rendimiento" (salvo "performance del coro", contexto distinto)
- "Approach" → "enfoque", "manera"
- "Workaround" → "atajo", "apaño"
- "Trade-off" → puede pasar (es estándar técnico), pero si puedes "compromiso", mejor
- "Feedback" → "comentarios", "retorno"
- "Mindset" → "mentalidad", "actitud"
- "Setup" → "configuración" o "preparación"
- "Wrap-up" → "cierre"
- "Bug" → "bug" pasa (vocabulario técnico universal)
- "Deploy" → "deploy" o "despliegue" (los dos pasan)

**Por qué**: si hay palabra española natural que un alumno entendería
sin esfuerzo, usa la española. Si la palabra inglesa es jerga
profesional (deploy, bug, pipeline), está bien.

---

## 12. Listas que son párrafos disfrazados

**Prohibido**: convertir todo en bullets cuando la prosa fluiría mejor.

**Por qué**: las bullets son útiles para enumerar elementos paralelos
de verdad. Si tienes una idea que se desarrolla con matices, va en
prosa. Las bullets fragmentan el pensamiento y se ven como notas, no
como explicación.

**Alternativa**: prosa con conectores naturales ("primero", "y
después", "el problema es que", "lo curioso es que").

---

## 13. Énfasis de relleno en negrita

**Prohibido**: poner en **negrita** palabras de paso ("**Es
importante** saber que...", "**El truco es** que...").

**Por qué**: la negrita debe marcar **datos o términos** que el lector
puede querer escanear (nombres de servicios, comandos, números clave).
Cuando se usa para enfatizar palabras de relleno, se devalúa y el
lector deja de ver lo importante.

---

## 14. Diminutivos y muletillas conversacionales en exceso

**Prohibidos en exceso**:
- "pequeñito", "facilito", "rapidito" → en jerga de tutorial demasiado
  casual, salen calcadas de IA imitando "cercanía".
- "vale", "ok", "bueno" en cada párrafo.
- "súper", "mega" para enfatizar.

**Por qué**: imitan "cercanía" sin tenerla. Suenan a influencer de
LinkedIn.

**Alternativa**: voz de formador profesional con humor seco
puntual, no estilo "amigo nuevo".

---

## Cómo aplicar la lista negra

1. Al redactar, ten esta lista como referencia mental.
2. Al terminar el manual, pasa los `grep` del bloque J del checklist.
3. Si el grep encuentra ocurrencias, **revisa caso por caso**. Algunas
   pueden ser legítimas (ej. citar a un autor que dijo "estado del
   arte" entre comillas). El grep es señal, no veredicto.
4. Si dudas, aplica el **test del café**: ¿le dirías esto a un alumno
   tomando café? Si no, reescribe.

---

## Esta lista evoluciona

Con el tiempo aparecerán nuevos patrones que la IA mete sin que te des
cuenta. Cuando descubras uno, **añádelo a esta lista** con su porqué
y su alternativa. La lista negra es la memoria del trabajo de
redacción humana.
