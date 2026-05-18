# Seguimiento de presentaciones — F-003-Azure

Checklist de estado por submódulo. Indica si la **presentación Gamma** está
registrada en el repo y si el **PDF** está subido (Git LFS).

> Registro de URLs detallado: [`PRESENTACIONES-GAMMA.md`](PRESENTACIONES-GAMMA.md).
> Los PDF viven en `doc/<MOD>/presentaciones/*.pdf` (Git LFS).

**Última actualización:** 2026-05-18

## Leyenda

| Símbolo | Gamma | PDF |
| --- | --- | --- |
| ✅ | enlace en índice + doc del submódulo | PDF subido al repo (LFS) |
| 🟡 | URL existe en Gamma, **falta aplicarla al repo** | — |
| ⏳ | presentación **no creada** en Gamma todavía | — |
| ❌ | — | PDF **pendiente** de subir |
| ⚠️ | requiere decisión | — |

## Resumen

| | Gamma ✅ | Gamma 🟡 | Gamma ⏳ | PDF ✅ | PDF ❌ |
| --- | --- | --- | --- | --- | --- |
| **Totales** | 69 | 0 | 11 | 66 | 14 |

- **Gamma:** M01–M09 en repo · M10-S10.1 aplicada · 11 sin crear (M10×1 · M11×10).
- **PDF:** M01–M08 completos · M09 5/7 → 66 subidos · 14 pendientes.

---

## M01 — Introducción a Azure (v5)

| Submódulo | Título | Gamma | PDF |
| --- | --- | :---: | :---: |
| S1.1 | Conceptos clave de la nube: IaaS, PaaS, SaaS | ✅ | ✅ |
| S1.2 | Portal, CLI y PowerShell | ✅ | ✅ |
| S1.3 | Suscripciones, recursos y costes | ✅ | ✅ |
| S1.4 | VS Code, SDK y extensiones | ✅ | ✅ |
| S1.5 | Conexión App Service y verificación | ✅ | ✅ |
| S1.P | Práctica — Hello World desde VS Code a Azure | ✅ | ✅ |
| S1.P2 | Práctica — Cloud Shell | ✅ | ✅ |

## M02 — App Services (v4)

| Submódulo | Título | Gamma | PDF |
| --- | --- | :---: | :---: |
| S2.1 | Creación, configuración y publicación | ✅ | ✅ |
| S2.2 | Slots staging / producción | ✅ | ✅ |
| S2.3 | Escalado automático y planes | ✅ | ✅ |
| S2.4 | Variables de conexión y configuración segura | ✅ | ✅ |
| S2.5 | Monitorización y diagnóstico | ✅ | ✅ |
| S2.P | Práctica — slots y swap | ✅ | ✅ |
| S2.P2 | Práctica — deploy básico | ✅ | ✅ |

## M03 — Azure Functions I (v4)

| Submódulo | Título | Gamma | PDF |
| --- | --- | :---: | :---: |
| S3.1 | Principios de cómputo sin servidor | ✅ | ✅ |
| S3.2 | Trigger HTTP | ✅ | ✅ |
| S3.3 | Trigger Timer | ✅ | ✅ |
| S3.4 | Trigger Blob Storage | ✅ | ✅ |
| S3.5 | Trigger Cosmos DB Change Feed | ✅ | ✅ |
| S3.6 | Bindings de entrada y salida | ✅ | ✅ |
| S3.P | Práctica — 4 triggers | ✅ | ✅ |
| S3.P2 | Práctica — HTTP CRUD en memoria | ✅ | ✅ |

## M04 — Azure Functions II (v4)

| Submódulo | Título | Gamma | PDF |
| --- | --- | :---: | :---: |
| S4.1 | Event Grid y Service Bus | ✅ | ✅ |
| S4.2 | Durable Functions | ✅ | ✅ |
| S4.3 | Errores, reintentos y dead-letter | ✅ | ✅ |
| S4.4 | Despliegue y versionado | ✅ | ✅ |
| S4.5 | Testing y depuración | ✅ | ✅ |
| S4.P | Práctica — flujo completo | ✅ | ✅ |
| S4.P2 | Práctica — Durable Hello World | ✅ | ✅ |

## M05 — Almacenamiento y BBDD (v3)

| Submódulo | Título | Gamma | PDF |
| --- | --- | :---: | :---: |
| S5.1 | Azure Storage | ✅ | ✅ |
| S5.2 | Azure SQL Database | ✅ | ✅ |
| S5.3 | Cosmos DB | ✅ | ✅ |
| S5.4 | Managed Identity | ✅ | ✅ |
| S5.5 | Backups | ✅ | ✅ |
| S5.P | Práctica — Cosmos + Managed Identity | ✅ | ✅ |
| S5.P2 | Práctica — Table Storage CRUD | ✅ | ✅ |

## M06 — Seguridad y Autenticación (v3)

| Submódulo | Título | Gamma | PDF |
| --- | --- | :---: | :---: |
| S6.1 | Responsabilidad compartida | ✅ | ✅ |
| S6.2 | Microsoft Entra ID | ✅ | ✅ |
| S6.3 | OAuth2 / OpenID Connect | ✅ | ✅ |
| S6.4 | Auth en desktop / MSIX | ✅ | ✅ |
| S6.5 | Seguridad de datos | ✅ | ✅ |
| S6.6 | Key Vault | ✅ | ✅ |
| S6.P | Práctica — OAuth2 + Key Vault | ✅ | ✅ |
| S6.P2 | Práctica — Easy Auth | ✅ | ✅ |

## M07 — Integración y MSIX (v3)

| Submódulo | Título | Gamma | PDF |
| --- | --- | :---: | :---: |
| S7.1 | Service Bus / Event Grid avanzado | ✅ | ✅ |
| S7.2 | Diseño event-driven | ✅ | ✅ |
| S7.3 | API Management | ✅ | ✅ |
| S7.4 | ClickOnce vs MSIX | ✅ | ✅ |
| S7.5 | MSIX — empaquetado y distribución | ✅ | ✅ |
| S7.6 | MSIX — auto-update | ✅ | ✅ |
| S7.7 | Migración ClickOnce → MSIX | ✅ | ✅ |
| S7.P | Práctica — MSIX | ✅ | ✅ |
| S7.P2 | Práctica — MSIX wizard | ✅ | ✅ |

## M08 — DevOps y Automatización (v3)

| Submódulo | Título | Gamma | PDF |
| --- | --- | :---: | :---: |
| S8.1 | Azure DevOps — Repos y Boards | ✅ | ✅ |
| S8.2 | Pipelines CI/CD YAML | ✅ | ✅ |
| S8.3 | Despliegue automatizado | ✅ | ✅ |
| S8.4 | ADO vs GitHub Actions | ✅ | ✅ |
| S8.5 | IaC con Bicep | ✅ | ✅ |
| S8.6 | App Insights y Monitor | ✅ | ✅ |
| S8.P | Práctica — Pipeline CI/CD | ✅ | ✅ |
| S8.P2 | Práctica — GitHub Actions + publish profile | ✅ | ✅ |

> **S8.P2:** resuelto — se usa el deck nuevo `…-4jdauo28d1r9q77` (18-may). El antiguo `…-ef0q3jcujuv1532` queda descartado.

## M09 — IA y Claude Code (v3)

| Submódulo | Título | Gamma | PDF |
| --- | --- | :---: | :---: |
| S9.1 | Claude Code — Introducción | ✅ | ❌ |
| S9.2 | Claude Code — Casos de uso | ✅ | ✅ |
| S9.3 | Claude Code — Infraestructura | ✅ | ✅ |
| S9.4 | MCP — Herramientas | ✅ | ✅ |
| S9.5 | Buenas prácticas y limitaciones | ✅ | ✅ |
| S9.P | Práctica — Claude Code + MCP | ✅ | ✅ |
| S9.P2 | Práctica — Claude Code primer comando | ✅ | ❌ |

> **M09:** todas las presentaciones Gamma aplicadas (S9.1–S9.P2).

## M10 — Proyecto Integrador (v3)

| Submódulo | Título | Gamma | PDF |
| --- | --- | :---: | :---: |
| S10.1 | Diseño y arquitectura | ✅ | ❌ |
| S10.P2 | Práctica — Mini proyecto Notas | ⏳ | ❌ |

## M11 — Bonus: Claude Code en Azure (v1)

| Submódulo | Título | Gamma | PDF |
| --- | --- | :---: | :---: |
| S11.1 | Introducción a la IA agéntica en Azure | ⏳ | ❌ |
| S11.2 | Claude Code — Setup en Azure | ⏳ | ❌ |
| S11.3 | Skills y capacidades especializadas | ⏳ | ❌ |
| S11.4 | Agentes y subagentes en Azure | ⏳ | ❌ |
| S11.5 | MCP y servicios de Azure | ⏳ | ❌ |
| S11.6 | Claude Code en cada módulo | ⏳ | ❌ |
| S11.7 | Claude Cowork para Azure | ⏳ | ❌ |
| S11.8 | Workflows avanzados de automatización | ⏳ | ❌ |
| S11.P | Práctica — Solución Azure + IA | ⏳ | ❌ |
| S11.P2 | Práctica — Claude + Azure light | ⏳ | ❌ |
