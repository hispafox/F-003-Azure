# Antipatrones de IA y Ejemplos de Transformación

## Índice
1. Los 15 antipatrones (con ejemplos IA vs humano)
2. Ejemplos completos — dominio técnico
3. Ejemplos completos — dominio no técnico

---

## ⚠️ Antes de aplicar nada: no planches la voz

Estos antipatrones sirven para quitar lo que suena a máquina, **no para dejar el texto plano**. El error más caro al corregir: cambiar un tic de IA por **prosa de informe**. Eso es peor que el tic — has cambiado "suena a IA por patrón" por "suena a IA por soso".

- Antes de tocar una frase pregúntate: ¿esto es un tic, o es un **remate de autor**? Una metáfora seca con verbo fuerte («la proporción se da la vuelta», «con un aplomo que asusta»), un golpe corto («No por nostalgia.»), un giro de calle («quitar de encima», «le robaba las horas») **no son tics**: son lo que hace que NO suene a IA. No los toques.
- El reemplazo tiene que **mantener o subir** la voz, nunca bajarla. Si tu corrección es más "correcta" pero más sosa, no la apliques: busca otra que arregle el patrón y conserve el remate.
- Tic ≠ frase memorable. El tic se reconoce porque es **estructura repetida** (tres «no es X, es Y», tripletas encadenadas, aforismos en serie). Una sola frase rotunda y concreta no es un tic: es el texto teniendo voz.
- Señal de que te pasaste: el revisor te pide *recuperar* versiones anteriores. Si pasa, paraste tarde.
- **El árbitro final es el oído del autor, no el review** (ni este skill). Los reviews informan; decide quien firma el texto. Si un review rescata un remate coloquial pero el autor prefiere la forma sobria ahí, manda el autor — sin bucle. Cuando dos reviews discrepan sobre una frase, es señal de que es *borderline*: no la resuelvas tú a base de iteraciones, llévala a quien firma.

Con eso claro:

## Los 15 antipatrones que debes destruir

### 1. El eco de la pregunta
La IA siempre reformula lo que le han pedido antes de responder. Un humano jamás hace esto.

❌ IA: «Entiendo que quieres saber cómo implementar autenticación en tu API REST. La autenticación en APIs REST es un tema fundamental que...»

✅ Humano: «Para esto tienes dos caminos: JWT si es una API interna, u OAuth2 si vas a exponer endpoints a terceros. Te explico las dos.»

### 2. La lista tripartita simétrica
La IA pone todo en grupos de tres con la misma longitud. Rompe la simetría.

❌ IA: «Los tres pilares fundamentales son: 1) Escalabilidad, que permite... 2) Mantenibilidad, que asegura... 3) Seguridad, que garantiza... [cada uno con exactamente 2 líneas]»

✅ Humano: «Lo que más importa aquí es la mantenibilidad — si no puedes cambiar el código sin rezar, algo falla. La seguridad también, claro. Y la escalabilidad, aunque siendo honestos, el 80% de los proyectos no necesitan escalar tanto como creen.»

### 3. El resumen-espejo
La IA cierra cada sección repitiendo lo que acaba de decir con otras palabras. Si ya lo dijiste, no lo repitas. Avanza o para.

### 4. La excesiva amabilidad

❌ IA: «¡Excelente pregunta! Me alegra que preguntes esto. Es un tema realmente fascinante y apasionante que...»

✅ Humano: «Esto tiene miga. A ver cómo te lo explico...»

### 5. El hedge universal (cubrirse demasiado)
La IA pone disclaimers en todo. Dosificado es útil; en cada respuesta es cobardía.

❌ IA: «Ambas opciones tienen sus ventajas y desventajas. La elección dependerá de tus necesidades específicas y del contexto particular de tu proyecto.»

✅ Humano: «Iría con PostgreSQL. No porque MongoDB sea malo, sino porque para lo que describes — relaciones entre entidades, consultas complejas, transacciones — SQL te va a ahorrar dolores de cabeza.»

### 6. La transición forzada
«Ahora que hemos visto X, pasemos a explorar Y». Simplemente salta a Y.

### 7. El párrafo de contexto innecesario
La IA siempre empieza explicando por qué el tema es importante. Si alguien lee sobre ello, ya le interesa.

❌ IA: «Entity Framework Core es uno de los ORMs más populares del ecosistema .NET. Los ORMs permiten a los desarrolladores interactuar con bases de datos sin escribir SQL directamente...»

✅ Humano: «EF Core hace bien muchas cosas, pero tiene trampas que no son obvias hasta que te muerden en producción. La más común: el tracking de entidades.»

### 8. La enumeración exhaustiva
La IA intenta cubrir TODOS los puntos. Un humano elige 2-3 y profundiza. Profundidad > amplitud.

### 9. El formato excesivo
Negritas con moderación, headers solo cuando cambia el tema, bullet points como último recurso. Si todo está formateado, nada destaca. Prefiere integrar en prosa (ver `lista-blanca.md` sección 6).

### 10. La uniformidad tonal
La IA mantiene exactamente el mismo tono todo el rato. Un humano varía: a veces serio, a veces coloquial, a veces un aparte irónico. Esa variación da vida.

### 11. La frase-golpe forzada (la metáfora de escritor)

El tic más difícil de ver desde dentro: adornar para *sonar* potente en vez de decir la cosa. Frases de efecto, metáforas que no aclaran nada, anuncios de transición y coloquialismos calcados del inglés. Suena a charla TED traducida o a gurú de LinkedIn, no a alguien explicando en una sala. El formador no *hace* de formador — habla.

❌ IA / escritor inseguro (casos reales cazados en producción):
- «Vamos con la definición, y luego la peleamos.» — anuncio de transición + metáfora vacía
- «es un buscador con esteroides» — calco de *X on steroids*
- «llega con una de estas tres ideas metidas» / «hay que limpiar esas ideas antes de enseñar» — coloquialismo cutre + narrar la didáctica en vez de enseñar
- «la palabra que aguanta toda la frase es *crea*» — metáfora de escritor; las palabras no "aguantan frases"
- «aquí abrimos la caja» — calco de *open the black box*
- «le quita el miedo y el papanatismo a partes iguales» — ritmo calcado de *in equal measure*

✅ Humano:
- «La IA generativa crea contenido nuevo a partir de una instrucción escrita con palabras normales. Lo importante de esa frase es el verbo: *crea*.»
- «lo tiene archivado como un buscador un poco más espabilado — y se queda corto justo en lo que importa»
- «llega convencido de que esto es el robot que viene a quitarle el trabajo»

Tres preguntas para cazarlo antes de entregar:
1. ¿Esta frase está para que se entienda mejor o para sonar ingenioso? Si es lo segundo, fuera.
2. ¿La metáfora aclara algo que sin ella no se vería? Si no, es decoración: fuera.
3. ¿Estoy *anunciando* lo que voy a hacer, o *contando cómo doy la clase*? Salta el anuncio y enseña directo.

A matar en seco: «Vamos con…», «Déjame que te cuente…», «Y aquí viene lo bueno» (cuando no viene), «X con esteroides», «el santo grial de», cualquier «X que [verbo dramático] toda la frase/idea/sección», y narrar la pedagogía («lo primero es desmontar sus ideas previas»). En material de curso, además: las etiquetas de plantilla traducida —*Encuadre, Núcleo, Desarrollo, Concreción, Destilación, throughline*— no son español de formador, son un índice de diseño instruccional en inglés. Si el texto las lleva, se reescribe como se habla.

**Dónde más se cuela: aperturas y cierres.** El cuerpo del párrafo suele estar bien; el tic se concentra en *cómo abres y cómo cierras* cada sección. Fórmulas a matar en seco, todas vistas en producción real:
- «Aquí está el malentendido más común…» / «Aquí tienes la idea clave…» → empieza por la afirmación, no por anunciarla.
- «Si tengo que resumir esto en una idea, es esta: …» → di la idea y punto.
- «Lo abstracto no convence a nadie, así que bajemos a un ejemplo» → pon el ejemplo; no narres por qué lo pones.
- «El aviso honesto:», «La parte incómoda:», «Vamos al lío:», «Y las tres importan», «Por qué importa (para X): porque…», «Lo importante aquí es que…» → etiquetas de sección disfrazadas de frase: anuncias la relevancia y te autorrespondes en vez de afirmar. Empieza por la afirmación. Es el tell de IA más fino de este grupo, a matar en seco.
- **La muletilla del aula**: armar la prosa sobre «la clase / la sala / el aula / la gente / antes de que salgáis de aquí» una y otra vez. Una escena de aula como gancho vale **una vez** en todo el texto; repetida en cada apertura es predecible y es meta (hablas de la situación de clase, no del tema). Y **en un título, jamás**: «Con qué llega la gente a esta clase» → el título nombra el contenido o la idea del alumno («Con qué idea llegas»), no el aula.
- Cierre con recap: «Hemos visto qué es, en qué se diferencia y dónde funciona…» → el cierre es una pregunta o un gancho a lo que viene, nunca el índice de lo ya dicho (ver antipatrón 3).
- **El cierre-coda que explica el papel de la sección**: «No entramos más aquí, cada cosa tiene su sitio», «esto es solo la vacuna para que el alumno no se vaya con los ojos cerrados», «sin dramatizar» (instrucción de tono metida en la prosa). Es narrar la pedagogía al final. La sección ya hizo su trabajo: termina en el último contenido o en un gancho — no expliques para qué servía la sección.
- **Notas de producción metidas en la prosa**: «una foto del momento, fechada y sin cifras que caduquen», «esto es una slide propia», «criterio de éxito del bloque». Eso es para las notas de montaje, no para quien lee o escucha. Va aparte, jamás en el texto.

**Aviso de proceso (cuesta caro):** este patrón **no lo caza un grep** — no es una palabra, es una forma de abrir y cerrar. La única verificación válida es leer cada apertura y cada cierre y preguntarse: *¿esto se lo digo a la clase, o le estoy contando cómo monto la clase?* Si es lo segundo, reescribe. No declares un texto limpio por haber pasado el grep de la lista negra: el grep es condición necesaria, no suficiente. Y lee **cada título por separado**, aislado del cuerpo: comprueba que el verbo pega con el sustantivo (un mapa no se «junta», una pregunta no «se queda colgando»). La colocación rota en títulos es reincidente y el grep tampoco la caza.

### 12. El absoluto y el eslogan (sobreafirmar)

En material formativo —y más si es corporativo— la sobreafirmación se paga: hay un experto en la sala esperando el absoluto para rebatirlo, y el resto huele el eslogan. La idea puede ser buena; el problema es decirla como verdad universal o como lema de camiseta.

❌ Absolutos que dan munición:
- «No lo busca en ningún sitio» → con RAG, búsqueda web o Copilot empresarial, a veces sí. Di: «no funciona como un buscador clásico; compone una respuesta nueva».
- «Lo revisa un humano. Sin excepción.» → «Sin excepción» es innecesario y frágil. Quítalo.
- «una regla casi infalible» → «una regla práctica».
- «la versión gratuita ya te da el resultado para el 80% del trabajo» → suena a dato inventado. Si la cifra no está medida, preséntala como estimación orientativa para el aula, nunca como dato: «en muchos casos, incluso una versión gratuita sirve de punto de partida».
- «la IA no ha eliminado el puesto» (categórico) → «en muchos trabajos, la IA no elimina el puesto entero: reduce la parte mecánica…».
- Precisión histórica/técnica: «la IA generativa llegó en 2022» → «se popularizó a partir de 2022». No nació entonces.

❌ Eslogan / frase-manifiesto: frases demasiado simétricas y redondas que suenan a claim de landing. Tener una frase-eje está bien (una). Convertir cada cierre en aforismo, no. Si una frase suena para ponerla en una camiseta, sospecha.

✅ Regla: afirma lo que puedes defender ante el más listo de la sala, y matiza el resto. La duda honesta («depende de…», «en muchos casos») da más autoridad que el absoluto. Cifras: con fuente o como estimación declarada; jamás un número redondo suelto que parezca medido.

**Precisión que no se negocia (aunque cueste un poco de brillo):**
- *El gancho no puede mentir.* «No te dice qué hay en la foto: te hace la foto» suena bien pero es falso (genera una imagen, no "hace una foto"). Si el gancho es técnicamente incorrecto, no es gancho: es un error que el experto de la sala caza. Reescribe: «no clasifica la imagen: genera una nueva».
- *«Generar contenido», no «crear información».* La IA produce contenido, y puede ser contenido falso. Decir que «crea información» le concede veracidad que no tiene. Usa «generar contenido».
- *Verbo preciso, no metáfora cuca.* «la máquina dibuja y programa» → «genera imágenes y escribe código». «Dibuja/programa» antropomorfiza y simplifica; el de perfil técnico de la sala lo nota.
- *«Lo hace» vs «puede empezar a hacerlo».* «Buena parte lo hace la máquina en segundos» sobrepromete el resultado. Lo honesto: «puede empezar a hacerlo / te deja una primera versión». No prometas que lo termina bien, solo que arranca.
- *Nada de cierre de coach en material corporativo.* «Y eso lo decides tú» es motivación de póster. En profesional: «depende del criterio con el que la uses».

### 13. El aforismo en bucle (el ritmo que delata a la IA)

Una frase con forma de sentencia, sola, funciona. En serie, es la firma rítmica de la máquina. Cuatro formas, todas cazadas en producción:

- **«No es X, es Y» repetido.** Una o dos veces, bien. A la tercera en pocos párrafos, suena a aforismo de máquina. Deja uno; el resto, afirmación normal — o deja que el ejemplo lo demuestre solo.
- **Paralelismo binario sentencioso.** «Ninguna es mentira del todo. Ninguna es verdad del todo.» / «Quien lo entiende, gana; quien lo sufre, pierde.» Bonito de ritmo y MUY de IA. Una por sección como mucho; si hay dos seguidas, mata una.
- **Tripletas encadenadas.** Una enumeración de tres está bien (ojo, ya es el antipatrón 2). Tres o cuatro listas de tres en bloques contiguos es marca registrada. Rompe el patrón: convierte alguna en pareja, mete un cuarto elemento irregular, o pásala a frase con un «y a veces…».
- **Intensificador de relleno.** «radicalmente distintos», «absolutamente clave», «completamente nuevo». El adverbio no añade nada y huele a IA. Quítalo o ensúcialo: «que no tienen nada que ver».

Tells de proceso (cuéntalos al revisar):
- «no es X, es Y» → más de 2 en una página = desármalos.
- Listas de tres en bloques seguidos → 3+ encadenadas, afloja una.
- **Guion largo (—): en texto de aula en castellano, más de ~12-15 en el documento es sospechoso.** En inglés el em-dash es endémico y al traducir mentalmente se cuela. Sustituye la mitad por punto, dos puntos o punto y aparte; reserva el `—` para incisos de verdad. Receta para el caso más común, la enumeración apositiva entre «— … —,» que alarga la frase: ábrela con dos puntos y ciérrala con punto. «Antes estaba en hacer la tarea — redactar, montar, preparar —, y hoy…» → «Antes estaba en hacer la tarea: redactar, montar, preparar. Hoy,…».

✅ Regla: el ritmo lo da la variación, no la simetría. Si al releerlo en voz alta suena a colección de citas, has encadenado aforismos — desármalos.

### 14. La frase-cápsula como entrega por defecto (el tic más difícil de ver)

El reflejo del modelo para soltar cualquier idea importante: una cápsula corta, antitética o declarativa. «No es X: es Y.» «A. B.» «Esto es Y. Punto.» «El peso cambia de sitio.» Suelta, cada una parece buena — por eso es el olor a IA más persistente y el más difícil de cazar. El problema no es la frase: es que **es la forma por defecto de entregar el concepto**. El lector siente "esto lo ha escrito una máquina lista" sin saber por qué: lo que nota es que le *sentencian* las ideas en vez de *explicárselas*.

No confundir con el antipatrón inverso (no planches la voz): **una** cápsula, en el clímax que la merece y sin otra cerca, es un remate de autor y se queda. El tic es usarla de continuo.

- **Tell**: cuenta las ideas clave que entregas en cápsula de ≤8 palabras o en molde «no es X: es Y / sino». Si es la mayoría, recalibra.
- **Fix**: desarrolla la idea en una frase que respira — con su matiz, su porqué, su subordinada. La cápsula se reserva para cuando (a) la idea es de verdad el remate y (b) no hay otra cápsula a tres frases de distancia.
- **Regla**: el concepto se explica; el remate se gana. Explicar no es sentenciar. Si cada párrafo cierra con su frasecita lapidaria, has escrito un tablón de citas, no una clase.

❌ «Por eso, lo que te llevas de este curso no es una marca: es criterio.»
✅ «Por eso lo que de verdad te llevas no es saber manejar tal o cual herramienta —esa puede cambiar el día que haga falta—, sino el criterio para decidir cuál usar y cuándo no, que es lo que no se queda anticuado en seis meses.»

**La forma que sale sola (crónica — cázala en relectura).** La pareja antitética corta como entrega por defecto: «La respuesta corta es X. La larga es Y.», «Versión corta: … Versión larga: …», «Spoiler: …», «No es A. Es B.» abriendo o cerrando. Es el reflejo más automático del modelo y el que más se cuela sin que te des cuenta — y en la variante «respuesta corta/larga» encima no respondes, *anuncias cómo vas a responder*. Fix: da la respuesta ya desarrollada, en una frase que respira; el matiz («en realidad es más largo de contar») va dentro de esa frase, no en una segunda cápsula que le haga de contrapeso. Regla rápida: si has escrito «la respuesta corta», bórralo y empieza por la respuesta.

### 15. El registro de la locución colado en el contenido base

El contenido base es el texto fuente escrito; la locución es lo que se dice en voz alta. No comparten registro. Un oralismo que da calor en la pista hablada suena de charla en el texto base —y al revés, lo que en base está bien medido, leído en alto puede sonar tieso—. La raya la pone el oído del autor: el skill informa, decide quien firma (ver el preámbulo "no planches la voz").

Oralismos que valen en locución y chirrían en base, cazados en producción:
- «resumir un montón de documentación» → en base, «documentación extensa» o «mucha documentación»; en locución, «un montón de» se queda.
- «esa la cambias el día que haga falta» (tú impersonal con clítico, muy de hablar) → en base, «esa puede cambiar el día que haga falta».
- «resultados que no tienen nada que ver» → en base sobrio, «resultados completamente distintos».

Ese último caso **matiza los antipatrones 12 y 13 y la regla del coloquialismo**: ensuciar el intensificador —«completamente distinto» a «que no tiene nada que ver»— es el ajuste por defecto del registro coloquial/aula, no una orden universal. En material base sobrio la forma limpia con su adverbio puede ser la correcta: «completamente» se gana el sitio cuando la versión sucia rompe el registro de la pista. No ensucies contra el registro que ha fijado quien firma.

Cuidado con el rebote: esto no es permiso para planchar. El base no es un informe corporativo. Sigue siendo voz de formador, solo que un escalón de oralidad por debajo de la locución, no a cero.

Tell de proceso: si una frase del base solo se sostiene leyéndola en voz alta, como si la dijeras en clase, es registro de locución colado. Para la pista hablada, perfecta; para el base, bájale una marcha y míralo otra vez.

---

## Ejemplos completos — dominio técnico

### Ejemplo 1: Apertura de capítulo

❌ IA:
«Capítulo 5: Entity Framework Core y el Agente

En este capítulo exploraremos cómo Entity Framework Core, uno de los ORMs más populares del ecosistema .NET, puede ser potenciado mediante el paradigma agéntico. Veremos cómo la inteligencia artificial puede asistir en la generación de modelos, la optimización de consultas y la gestión eficiente de migraciones. A lo largo del capítulo, aprenderemos las mejores prácticas para integrar agentes de IA en nuestro flujo de trabajo con EF Core.»

✅ Humano (con storytelling):
«Capítulo 5: EF Core con agente — o cómo dejar de escribir DbContext a mano

El mes pasado, en un curso de .NET, un alumno levantó la mano y preguntó: "Si el agente puede generar código, ¿por qué sigo yo escribiendo migraciones a mano?". Silencio en la sala. Buena pregunta.

La respuesta corta es que no deberías — al menos no todas. Pero la respuesta larga tiene un matiz que ese alumno descubrió esa misma tarde, cuando dejó que Claude le generara toda la capa de datos sin revisarla. Compilaba. Los tests pasaban. Pero había un N+1 que convertía cada consulta en una fiesta de 47 queries a la base de datos.

Dejar que un agente genere tu capa de datos sin entender qué está haciendo es como darle las llaves del coche a alguien que no sabe conducir. Funcionará... hasta que no.»

### Ejemplo 2: Post de LinkedIn (técnico)

❌ IA:
«En el panorama actual del desarrollo de software, los agentes de IA están transformando la forma en que los equipos abordan el desarrollo. Es importante tener en cuenta que esta revolución tecnológica trae consigo tanto oportunidades como desafíos. Sin embargo, las empresas que adopten estas herramientas innovadoras estarán mejor posicionadas para el futuro.

¿Y tú qué opinas? 👇»

✅ Humano:
«Ayer un junior de mi equipo generó un módulo completo con Claude en 40 minutos.

El código compilaba. Los tests pasaban. El PR estaba limpio.

Pero había un problema: no entendía qué había generado.

No sabía por qué se usaba inyección de dependencias, ni qué hacía el middleware de autenticación, ni por qué había un try-catch en ese punto específico.

Y ahí está la trampa del desarrollo con agentes: la velocidad sin comprensión es deuda técnica disfrazada de productividad.»

### Ejemplo 3: Documentación técnica

❌ IA:
«Sección: Configuración del middleware de autenticación

El middleware de autenticación es un componente fundamental en cualquier aplicación web moderna. Permite verificar la identidad de los usuarios y controlar el acceso a los recursos protegidos. A continuación, se presentan los pasos necesarios para configurar la autenticación basada en JWT en una aplicación ASP.NET Core.»

✅ Humano:
«Cómo añadir autenticación JWT

Sin esto, tus endpoints están abiertos a cualquiera. Literalmente cualquiera con un curl puede llamarlos. Aquí está lo mínimo que necesitas para evitarlo.

Ojo: asegúrate de que tu clave secreta NO esté hardcodeada en el código. Usa User Secrets en desarrollo y variables de entorno en producción.»

### Ejemplo 4: Analogía técnica

❌ IA: «La inyección de dependencias es un patrón de diseño que permite desacoplar los componentes de una aplicación mediante la inversión del control de las dependencias.»

✅ Humano: «La inyección de dependencias es como pedir comida a domicilio en vez de cocinar. Tu clase no va al supermercado (no crea sus dependencias): alguien se las trae a la puerta (el contenedor de DI). Si mañana quieres cambiar de restaurante (implementación), tu clase ni se entera.»

---

## Ejemplos completos — dominio no técnico

### Ejemplo 5: Post de LinkedIn (gestión de equipos)

❌ IA:
«En el competitivo panorama empresarial actual, la gestión efectiva de equipos se ha convertido en un factor diferenciador clave. Los líderes que fomentan una cultura de feedback continuo y comunicación transparente logran equipos más comprometidos y productivos. Es fundamental reconocer que el capital humano es el activo más valioso de cualquier organización.

¿Estás de acuerdo? Comparte tu experiencia 👇»

✅ Humano:
«La semana pasada le pregunté a mi equipo: "¿Qué es lo que más os frustra de cómo trabajo?"

Silencio. Incómodo. Largo.

Hasta que alguien dijo: "Que a veces pedimos feedback y luego no cambia nada."

Dolió. Pero era verdad.

El feedback que no se convierte en acción es peor que no pedirlo. Genera la ilusión de que escuchas sin la realidad de que cambias.»

### Ejemplo 6: Propuesta comercial

❌ IA:
«Nuestra solución integral de transformación digital está diseñada para impulsar la competitividad de su organización en un entorno empresarial cada vez más exigente. A través de nuestra metodología probada y nuestro equipo de profesionales altamente cualificados, le ayudaremos a optimizar sus procesos y maximizar el retorno de su inversión tecnológica.»

✅ Humano:
«Vuestro equipo de ventas pierde una media de 3 horas al día buscando información en 4 herramientas distintas. Lo que proponemos es que esa información esté en un solo sitio, actualizada, y que el vendedor pueda preparar una reunión en 15 minutos en vez de una hora. Lo hemos hecho con 6 empresas parecidas a la vuestra. En todas, la adopción superó el 80% en el primer mes — porque la herramienta les quita trabajo en vez de añadirlo.»

### Ejemplo 7: Newsletter / artículo sobre producto

❌ IA:
«Nos complace anunciar el lanzamiento de la versión 3.0 de nuestra plataforma, que incorpora una serie de mejoras significativas diseñadas para optimizar la experiencia del usuario. Entre las novedades más destacadas se encuentran un sistema de búsqueda renovado, una interfaz más intuitiva y herramientas analíticas avanzadas que permitirán a nuestros usuarios tomar decisiones más informadas.»

✅ Humano:
«La v3.0 trae tres cambios que os habíais pedido (literalmente — salieron de la encuesta de diciembre).

El primero: la búsqueda ahora funciona como esperáis. Escribes lo que buscas y aparece. Sin filtros raros, sin tener que saber el nombre exacto del campo. Era absurdo que no funcionara así desde el principio, lo sabemos.

El segundo es más sutil pero os va a ahorrar tiempo: los dashboards se cargan en la mitad de tiempo. No hemos tocado la interfaz — hemos reescrito la capa de datos por debajo.

Y el tercero... bueno, mejor lo veis vosotros. Hay una pestaña nueva de "Insights" que os va a gustar.»

### Ejemplo 8: Email profesional

❌ IA:
«Estimado equipo,

Espero que este correo les encuentre bien. Me dirijo a ustedes con el propósito de informarles sobre las actualizaciones recientes en nuestro proceso de onboarding. Tras una exhaustiva evaluación, hemos identificado áreas de mejora significativas que nos permitirán optimizar la experiencia de incorporación de nuevos colaboradores.

Les agradezco de antemano su atención y colaboración en este importante proceso.»

✅ Humano:
«Equipo,

Hemos cambiado el proceso de onboarding. Resumen rápido:

La primera semana ya no tiene 5 sesiones de formación seguidas (que nadie retenía). Ahora son 2 sesiones + un proyecto guiado con un mentor. El feedback de los últimos 3 que entraron fue que aprendieron más en el proyecto que en todas las sesiones juntas.

El nuevo proceso empieza con la incorporación de Laura el lunes. Si algo no funciona, lo ajustamos sobre la marcha.

¿Dudas? Hablamos en el daily.»
