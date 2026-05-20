# Lista Blanca — Patrones que SÍ Funcionan

Este archivo es el complemento positivo de la lista negra. No basta con saber qué evitar:
necesitas un repertorio de patrones que suenan humanos y que puedes rotar.

---

## 1. Arranques de frase (rota entre estos)

### Con conjunción (rompe la "regla" de no empezar con Y/Pero)
- «Y aquí viene lo bueno.»
- «Pero ojo.»
- «Así que...»
- «Y no, no es tan sencillo.»
- «Pero hay un matiz.»

### Frase corta de impacto
- «Funciona.»
- «No es suficiente.»
- «Ojo con esto.»
- «Exacto.»
- «Spoiler: va a fallar.»
- «Pues no.»
- «Depende.» (solo si luego explicas de qué)

### Pregunta real (no retórica barata)
- «¿Qué pasa si el servidor se cae a mitad de transacción?»
- «¿Y cuántas veces has hecho esto sin pensar en las consecuencias?»
- «¿El resultado? Nada bueno.»
- «¿Por qué? Porque nadie lo testea.»

### Afirmación directa
- «Esto no funciona en producción.»
- «El problema es otro.»
- «La realidad es más fea.»
- «No te lo recomiendo.»

### Con aparte o inciso
- «Lo típico — y lo he visto cientos de veces — es que...»
- «El truco (que no sale en la documentación) es...»
- «La versión corta: no lo hagas.»

### Con experiencia implícita
- «En la práctica, lo que suele pasar es que...»
- «El error más común con esto es...»
- «Lo que nadie te cuenta es que...»
- «Después de verlo fallar unas cuantas veces...»

---

## 2. Conectores humanos (alternativas a los de IA)

### Para matizar o contraponer
- «Ahora bien...»
- «Eso sí...»
- «Lo que pasa es que...»
- «Claro que...»
- «El problema es que...»
- «Ojo:»
- «A ver, esto tiene truco:»

### Para añadir información
- «Y encima...»
- «Por si fuera poco...»
- «Ah, y otra cosa:»
- «Que además...»
- «Lo mejor (o lo peor, según se mire):»

### Para concretar
- «Mira...»
- «A ver, en cristiano:»
- «Dicho de otra forma:»
- «O sea:»
- «Traducido a la práctica:»

### Para cambiar de tema sin transición forzada
- (Simplemente cambia de párrafo o pon un subtítulo. No necesitas conector.)
- Si insistes: «Otra cosa.» / «Hay más.» / «Y luego está el tema de...»

### Para cerrar un punto
- «Y punto.»
- «Así de simple.»
- «Ni más ni menos.»
- «Eso es todo lo que necesitas.»

### Conectores castellanos de registro alto (reflexión, ensayo, capítulo de libro)

Para textos más reposados — capítulo de libro, ensayo técnico, propuesta — donde
los conectores muy coloquiales de arriba quedarían fuera de tono. Estos suenan
naturales en castellano escrito sin caer en formal de manual.

- «Cada tanto,» — variante humana de "a veces"; suena a quien escribe desde su
  propia rutina. *«Cada tanto, me dan ganas de revisar el código a mano antes de
  pasarlo por el linter.»*
- «Llegados a este punto,» — transición de reflexión después de exposición densa.
  *«Llegados a este punto, es razonable preguntarse si el coste compensa.»*
- «En última instancia,» — cierre de matiz que devuelve la responsabilidad al
  lector. *«En última instancia, solo tú sabes si tu equipo está preparado.»*
- «Veamos un par de ejemplos.» — transición a casos sin el robótico "a continuación".
- «Es obvio que [X], pero [matiz]» — concesión con bisagra. Reconoce lo obvio antes
  de añadir el punto fino. *«Es obvio que la caché mejora el rendimiento, pero el
  día que tengas que invalidarla entiendes el coste real.»*

---

## 3. Transiciones entre secciones

La mejor transición es ninguna. Un buen subtítulo hace el trabajo. Pero si necesitas
conectar dos ideas, estas opciones funcionan:

### Con pregunta natural
- «¿Y qué pasa cuando necesitas hacer esto mismo pero con múltiples tablas?»
- «¿Suena bien? Lo es. Hasta que metes concurrencia en la ecuación.»
- «¿Y si te digo que hay una forma mejor?»

### Con tensión
- «Todo esto funciona. Hasta que deja de funcionar.»
- «Bien, ya tienes lo básico. Ahora viene la parte interesante.»
- «Hasta aquí fácil. Lo siguiente no tanto.»

### Con gancho directo
- «Hay un detalle que cambia todo esto.»
- «Aquí es donde la mayoría se equivoca.»
- «Lo que acabas de ver tiene un problema que no es obvio.»

---

## 4. Formas de dar opinión con matiz

- «Yo iría con X. No porque Y sea malo, sino porque para lo que describes, X te va a ahorrar problemas.»
- «Mi recomendación: Z. Aunque si tu proyecto es pequeño y no va a crecer, puedes vivir sin ello.»
- «Es la mejor opción en el 80% de los casos. En el otro 20%, depende de...»
- «Funciona, pero no es mi primera opción. Te explico por qué.»
- «La respuesta fácil es X. La respuesta honesta es "depende", pero no de lo que crees.»

---

## 5. Formas de meter humor sin forzar

### Ironía seca (la verdad con una sonrisa torcida)
- «Porque nada dice "micro" como un contenedor de 2GB.»
- «Configurar CORS es ese ritual que todos odiamos pero nadie puede evitar.»
- «En mi máquina funciona — la frase que destruyó más relaciones profesionales que el café frío.»

### Understatement (decir menos de lo que es)
- «No es exactamente la herramienta más intuitiva del mundo.»
- «La documentación es... optimista.»
- «Digamos que la curva de aprendizaje tiene personalidad.»

### Hipérbole calculada (exagerar lo justo)
- «Si tu proyecto tiene más de 3 developers, esto te va a explotar.»
- «He visto configuraciones de Webpack que necesitaban su propio equipo de mantenimiento.»

### Autocrítica (te hace cercano)
- «Y sí, yo también lo hice mal las primeras veces.»
- «No me enorgullece decir cuántas veces he caído en esto.»

### Referencia compartida (el lector se siente parte del grupo)
- «El clásico "funciona en mi máquina" ahora se ha convertido en "funciona en mi pod".»
- «El README dice que son 5 minutos de setup. Multiplica por 4 y vas bien.»

---

## 6. Formas de integrar listas en prosa

En vez de bullet points, integra las enumeraciones en el texto:

- «Necesitas tres cosas: X, Y y Z.» (directa)
- «Lo que ganas es rendimiento, mantenibilidad y — de regalo — seguridad.» (con ritmo)
- «El primero es obvio: X. El segundo menos: Y. Y el tercero te va a sorprender.» (con tensión)
- «Hay dos formas de hacerlo. La fácil (que te va a dar problemas en producción) y la correcta.» (con opinión)
- «X, claro. Y — que nadie menciona pero es igual de importante. Y si me apuras, también Z, aunque este es más situacional.» (con matiz progresivo)
