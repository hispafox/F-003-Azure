# M10 — Proyecto Integrador · ejemplos

Ejemplos de código que acompañan al
[Módulo 10 — Proyecto Integrador](../../doc/M10-Proyecto-Integrador).

Este módulo cierra el curso: el alumno construye un sistema completo
de gestión de pedidos que integra los 9 módulos anteriores (App
Service + Functions + Cosmos DB + Service Bus + Entra ID + Key Vault
+ Managed Identity + Pipeline CI/CD + App Insights + Bicep).

Los ejemplos son **conceptuales** (lección 9 del HANDOFF): el alumno
construye la solución real en su entorno Azure; aquí extraemos las
heurísticas pedagógicas (checklist de arquitectura, recomendador de
bloque, evaluador de entrega).

## Submódulos cubiertos

| Submódulo | Tema | Ejemplo | Estado |
| --- | --- | --- | --- |
| [S10.1](../../doc/M10-Proyecto-Integrador/v3-actual/M10-S10.1-diseno-arquitectura-v3.md) | Diseño y arquitectura (checklist, bloques A-D, entrega) | [`S10.1-diseno-arquitectura/`](S10.1-diseno-arquitectura/README.md) | ✅ Disponible |
| [S10.P2](../../doc/M10-Proyecto-Integrador/v3-actual/M10-S10.P2-practica-mini-proyecto-notas-v1.md) | Práctica — mini-proyecto notas | — | ⏳ Pendiente |

⏳ **Módulo M10 en construcción** (1/2).

## Patrón de tests

- **CAPA 1 · Unit**: heurísticas puras (checklist de componentes,
  recomendador de bloque por progreso, evaluador de entrega con
  pesos).
- **CAPA 0 · DI**: `WebApplicationFactory` resuelve el planner real.
- **CAPA E2E**: la API completa vía `WebApplicationFactory`.
- **Sin integración**: el sistema real se construye en clase
  siguiendo el README + los bloques A-D del slide 5.

## Requisitos comunes

- .NET SDK 10
- Para construir el sistema real: suscripción Azure + Azure CLI +
  Bicep + (opcional) Claude Code de M09.
