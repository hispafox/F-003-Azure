# M01 — Intro Azure · ejemplos

Ejemplos de código que acompañan al [Módulo 1 — Intro Azure](../../doc/M01-Intro-Azure).
Por ahora solo está la práctica. Los submódulos teóricos (S1.1-S1.5) son
conceptuales y no llevan ejemplo de código asociado.

## Submódulos cubiertos

| Submódulo | Tema | Ejemplo | Estado |
| --- | --- | --- | --- |
| S1.1 | Conceptos de la nube (IaaS / PaaS / SaaS) | _conceptual_ | — |
| S1.2 | Portal, CLI, PowerShell | _conceptual_ | — |
| S1.3 | Suscripciones, recursos y costes | _conceptual_ | — |
| S1.4 | VS Code, SDK y extensiones | _conceptual_ | — |
| S1.5 | Conexión a App Service | _conceptual_ | — |
| [S1.P](../../doc/M01-Intro-Azure/v5-actual/M01-S1.P-practica-helloworld-v5.md) | **Práctica:** Hello World end-to-end | [`S1.P-practica-helloworld/`](S1.P-practica-helloworld/README.md) | ✅ Disponible |
| S1.P2 | Práctica — Cloud Shell | _Pendiente_ | ⏳ |

## Hilo conductor

S1.P es la **primera práctica del curso entero**: provisiona los recursos
mínimos en Azure, despliega un Hello World en F1 (gratuito) y verifica el
ciclo end-to-end. El RG y la web app que se crean aquí se **reutilizan en
M02-S2.P** (slots y swap), así que el cleanup pregunta si quieres conservarlos.

## Requisitos comunes

- .NET SDK 10
- Suscripción de Azure
- Azure CLI (`az`) para los scripts
- VS Code + extensión **Azure App Service** (recomendada para el deploy)
