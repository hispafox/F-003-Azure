# M02 — App Services · ejemplos

Ejemplos de código que acompañan al [Módulo 2 — App Services](../../doc/M02-App-Services).
Cada uno se centra en los conceptos de un submódulo concreto y reutiliza el mismo
estilo: Minimal API .NET 10, xUnit, despliegue por Portal de Azure.

## Submódulos cubiertos

| Submódulo | Tema | Ejemplo | Estado |
| --- | --- | --- | --- |
| [S2.1](../../doc/M02-App-Services/v4-actual/M02-S2.1-creacion-configuracion-publicacion-v4.md) | Creación, configuración y publicación en App Service | [`S2.1-creacion-config-publicacion/`](S2.1-creacion-config-publicacion/README.md) | ✅ Disponible |
| [S2.2](../../doc/M02-App-Services/v4-actual/M02-S2.2-slots-staging-produccion-v4.md) | Slots de despliegue (staging / producción) | [`S2.2-slots-staging-produccion/`](S2.2-slots-staging-produccion/README.md) | ✅ Disponible |
| [S2.3](../../doc/M02-App-Services/v4-actual/M02-S2.3-escalado-automatico-planes-v4.md) | Escalado automático y planes | [`S2.3-escalado-automatico-planes/`](S2.3-escalado-automatico-planes/README.md) | ✅ Disponible |
| [S2.4](../../doc/M02-App-Services/v4-actual/M02-S2.4-variables-conexion-config-segura-v4.md) | Variables de conexión y configuración segura | [`S2.4-variables-conexion-config-segura/`](S2.4-variables-conexion-config-segura/README.md) | ✅ Disponible |
| [S2.5](../../doc/M02-App-Services/v4-actual/M02-S2.5-monitorizacion-diagnostico-v4.md) | Monitorización y diagnóstico | [`S2.5-monitorizacion-diagnostico/`](S2.5-monitorizacion-diagnostico/README.md) | ✅ Disponible |
| [S2.P](../../doc/M02-App-Services/v4-actual/M02-S2.P-practica-slots-swap-v4.md) | Práctica — slots y swap | [`S2.P-practica-slots-swap/`](S2.P-practica-slots-swap/README.md) | ✅ Disponible |
| S2.P2 | Práctica — deploy básico | _Pendiente_ | ⏳ |

## Hilo conductor del módulo

A medida que avanzan los submódulos, los ejemplos se construyen sobre el del
anterior cuando tiene sentido (S2.2 reaprovecha la API de S2.1 y le añade slots,
S2.3 añade escalado, etc.). Cada ejemplo es ejecutable de forma aislada, pero
leídos en orden cuentan una progresión real "de cero a producción".

## Requisitos comunes

- .NET SDK 10
- Suscripción de Azure
- VS Code + extensión **Azure App Service** (recomendada para el deploy)
