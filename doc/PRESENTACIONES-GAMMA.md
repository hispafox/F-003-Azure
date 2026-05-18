# Índice de presentaciones Gamma — F-003-Azure

Este documento es el **registro maestro** de las presentaciones de Gamma del curso.
Cada vez que se publica una presentación se anota aquí con su URL de compartir; desde
este índice se propaga el enlace al material que corresponda:

1. **Doc del submódulo** en [`doc/<MOD>/v*-actual/<SUB>.md`](.) — siempre.
2. **README del ejemplo asociado** en [`examples/<MOD>/<SUB>/README.md`](../examples) —
   sólo cuando el submódulo tiene un ejemplo de código.

> **Convención de enlace en los `.md` destino:** una línea en la cabecera, justo bajo
> los metadatos del archivo:
>
> ```markdown
> > **Presentación Gamma:** [<Título>](<url>)
> ```

## Estado por módulo

Leyenda: ✅ enlace registrado · ⏳ pendiente de URL.

### M01 — Introducción a Azure (v5)

| Submódulo | Título | Doc | Presentación |
| --- | --- | --- | --- |
| S1.1 | Conceptos clave de la nube: IaaS, PaaS, SaaS | [doc](M01-Intro-Azure/v5-actual/M01-S1.1-conceptos-nube-iaas-paas-saas-v5.md) | ✅ [Gamma](https://gamma.app/docs/M01-S11-conceptos-nube-iaas-paas-saas-v3-5dxgrs1uctthe4h) |
| S1.2 | Portal, CLI y PowerShell | [doc](M01-Intro-Azure/v5-actual/M01-S1.2-portal-cli-powershell-v5.md) | ✅ [Gamma](https://gamma.app/docs/M01-S12-portal-cli-powershell-v3-j4a0kcn9fhi0rod) |
| S1.3 | Suscripciones, recursos y costes | [doc](M01-Intro-Azure/v5-actual/M01-S1.3-suscripciones-recursos-costes-v5.md) | ✅ [Gamma](https://gamma.app/docs/M01-S13-suscripciones-recursos-costes-v3-i4w3un8b2v7c6gh) |
| S1.4 | VS Code, SDK y extensiones | [doc](M01-Intro-Azure/v5-actual/M01-S1.4-vscode-sdk-extensiones-v5.md) | ✅ [Gamma](https://gamma.app/docs/M01-S14-vscode-sdk-extensiones-v3-21dwg87cxf0tp8r) |
| S1.5 | Conexión App Service y verificación | [doc](M01-Intro-Azure/v5-actual/M01-S1.5-conexion-appservice-verificacion-v4.md) | ✅ [Gamma](https://gamma.app/docs/M01-S15-conexion-appservice-verificacion-v4-mfym980op6zuuz8) |
| S1.P | Práctica — Hello World desde VS Code a Azure | [doc](M01-Intro-Azure/v5-actual/M01-S1.P-practica-helloworld-v5.md) | ✅ [Gamma](https://gamma.app/docs/Submodulo-1P-Hello-World-desde-VS-Code-a-Azure-1z29lshl7zhyirq) |
| S1.P2 | Práctica — Cloud Shell | [doc](M01-Intro-Azure/v5-actual/M01-S1.P2-practica-cloud-shell-v1.md) | ✅ [Gamma](https://gamma.app/docs/M01-S1P2-practica-cloud-shell-v1-hl0xfh7s8m1ash0) |

### M02 — App Services (v4)

| Submódulo | Título | Doc | Ejemplo | Presentación |
| --- | --- | --- | --- | --- |
| S2.1 | Creación, configuración y publicación | [doc](M02-App-Services/v4-actual/M02-S2.1-creacion-configuracion-publicacion-v4.md) | [ejemplo](../examples/M02-App-Services/S2.1-creacion-config-publicacion/README.md) | ✅ [Gamma](https://gamma.app/docs/M02-S21-creacion-configuracion-publicacion-v4-5agxfm0zw2elcea) |
| S2.2 | Slots staging / producción | [doc](M02-App-Services/v4-actual/M02-S2.2-slots-staging-produccion-v4.md) | [ejemplo](../examples/M02-App-Services/S2.2-slots-staging-produccion/README.md) | ✅ [Gamma](https://gamma.app/docs/M02-S22-slots-staging-produccion-v4-mtdov7l5xomml5r) |
| S2.3 | Escalado automático y planes | [doc](M02-App-Services/v4-actual/M02-S2.3-escalado-automatico-planes-v4.md) | [ejemplo](../examples/M02-App-Services/S2.3-escalado-automatico-planes/README.md) | ✅ [Gamma](https://gamma.app/docs/M02-S23-escalado-automatico-planes-v4-4sl35nk4ua4zelk) |
| S2.4 | Variables de conexión y configuración segura | [doc](M02-App-Services/v4-actual/M02-S2.4-variables-conexion-config-segura-v4.md) | _Pendiente_ | ✅ [Gamma](https://gamma.app/docs/M02-S24-variables-conexion-config-segura-v4-1c3foflz4lunlqq) |
| S2.5 | Monitorización y diagnóstico | [doc](M02-App-Services/v4-actual/M02-S2.5-monitorizacion-diagnostico-v4.md) | _Pendiente_ | ✅ [Gamma](https://gamma.app/docs/M02-S25-monitorizacion-diagnostico-v4-phxokefb4pou6ph) |
| S2.P | Práctica — slots y swap | [doc](M02-App-Services/v4-actual/M02-S2.P-practica-slots-swap-v4.md) | _Pendiente_ | ✅ [Gamma](https://gamma.app/docs/M02-S2P-practica-slots-swap-v4-wcqckma6lp90clf) |
| S2.P2 | Práctica — deploy básico | [doc](M02-App-Services/v4-actual/M02-S2.P2-practica-deploy-basico-v1.md) | _Pendiente_ | ✅ [Gamma](https://gamma.app/docs/M02-S2P2-practica-deploy-basico-v1-tvsz5lbyy37e7z1) |

### M03 — Azure Functions I (v4)

| Submódulo | Título | Doc | Presentación |
| --- | --- | --- | --- |
| S3.1 | Principios de cómputo sin servidor | [doc](M03-Azure-Functions-I/v4-actual/M03-S3.1-principios-computo-sin-servidor-v4.md) | ✅ [Gamma](https://gamma.app/docs/M03-S31-principios-computo-sin-servidor-v4-z0ool9h55cli1k1) |
| S3.2 | Trigger HTTP | [doc](M03-Azure-Functions-I/v4-actual/M03-S3.2-trigger-http-v4.md) | ✅ [Gamma](https://gamma.app/docs/M03-S32-trigger-http-v4-dgjw1m9zxv4s2zy) |
| S3.3 | Trigger Timer | [doc](M03-Azure-Functions-I/v4-actual/M03-S3.3-trigger-timer-v4.md) | ✅ [Gamma](https://gamma.app/docs/M03-S33-trigger-timer-v4-r7sgkzkseur72gi) |
| S3.4 | Trigger Blob Storage | [doc](M03-Azure-Functions-I/v4-actual/M03-S3.4-trigger-blob-storage-v4.md) | ✅ [Gamma](https://gamma.app/docs/M03-S34-trigger-blob-storage-v4-2f27qjd0hgb6607) |
| S3.5 | Trigger Cosmos DB Change Feed | [doc](M03-Azure-Functions-I/v4-actual/M03-S3.5-trigger-cosmosdb-changefeed-v4.md) | ✅ [Gamma](https://gamma.app/docs/M03-S35-trigger-cosmosdb-changefeed-v4-3i6gyca4g24wd2x) |
| S3.6 | Bindings de entrada y salida | [doc](M03-Azure-Functions-I/v4-actual/M03-S3.6-bindings-entrada-salida-v4.md) | ✅ [Gamma](https://gamma.app/docs/M03-S36-bindings-entrada-salida-v4-hjw2a5wm4pvpsxu) |
| S3.P | Práctica — 4 triggers | [doc](M03-Azure-Functions-I/v4-actual/M03-S3.P-practica-4-triggers-v4.md) | ✅ [Gamma](https://gamma.app/docs/M03-S3P-practica-4-triggers-v4-j7cy67ltiekqlef) |
| S3.P2 | Práctica — HTTP CRUD en memoria | [doc](M03-Azure-Functions-I/v4-actual/M03-S3.P2-practica-http-crud-memoria-v1.md) | ✅ [Gamma](https://gamma.app/docs/M03-S3P2-practica-http-crud-memoria-v1-xqecs4s8bzcjei0) |

### M04 — Azure Functions II (v4)

| Submódulo | Título | Doc | Presentación |
| --- | --- | --- | --- |
| S4.1 | Event Grid y Service Bus | [doc](M04-Azure-Functions-II/v4-actual/M04-S4.1-event-grid-service-bus-v4.md) | ✅ [Gamma](https://gamma.app/docs/M04-S41-event-grid-service-bus-v4-prcx2nohqxr2i62) |
| S4.2 | Durable Functions | [doc](M04-Azure-Functions-II/v4-actual/M04-S4.2-durable-functions-v4.md) | ✅ [Gamma](https://gamma.app/docs/M04-S42-durable-functions-v4-c3bz4rzy8u3zyd6) |
| S4.3 | Errores, reintentos y dead-letter | [doc](M04-Azure-Functions-II/v4-actual/M04-S4.3-errores-reintentos-deadletter-v4.md) | ✅ [Gamma](https://gamma.app/docs/M04-S43-errores-reintentos-deadletter-v4-fz1unh2pl9dk78u) |
| S4.4 | Despliegue y versionado | [doc](M04-Azure-Functions-II/v4-actual/M04-S4.4-despliegue-versionado-v4.md) | ✅ [Gamma](https://gamma.app/docs/M04-S44-despliegue-versionado-v4-9uak65v4dsuguuq) |
| S4.5 | Testing y depuración | [doc](M04-Azure-Functions-II/v4-actual/M04-S4.5-testing-depuracion-v4.md) | ✅ [Gamma](https://gamma.app/docs/M04-S45-testing-depuracion-v4-llpcf4fh27ismuk) |
| S4.P | Práctica — flujo completo | [doc](M04-Azure-Functions-II/v4-actual/M04-S4.P-practica-flujo-completo-v4.md) | ✅ [Gamma](https://gamma.app/docs/M04-S4P-practica-flujo-completo-v4-pkm9fb47rmknrjj) |
| S4.P2 | Práctica — Durable Hello World | [doc](M04-Azure-Functions-II/v4-actual/M04-S4.P2-practica-durable-hello-world-v1.md) | ✅ [Gamma](https://gamma.app/docs/M04-S4P2-practica-durable-hello-world-v1-ykteli17bj890kc) |

### M05 — Almacenamiento y Bases de datos (v3)

| Submódulo | Título | Doc | Presentación |
| --- | --- | --- | --- |
| S5.1 | Azure Storage | [doc](M05-Almacenamiento-BBDD/v3-actual/M05-S5.1-azure-storage-v3.md) | ✅ [Gamma](https://gamma.app/docs/M05-S51-azure-storage-v3-5tvdt3qk9z4wtos) |
| S5.2 | Azure SQL Database | [doc](M05-Almacenamiento-BBDD/v3-actual/M05-S5.2-azure-sql-database-v3.md) | ✅ [Gamma](https://gamma.app/docs/M05-S52-azure-sql-database-v3-rlng1fv6xapc49p) |
| S5.3 | Cosmos DB | [doc](M05-Almacenamiento-BBDD/v3-actual/M05-S5.3-cosmosdb-v3.md) | ✅ [Gamma](https://gamma.app/docs/M05-S53-cosmosdb-v3-wnlexms0b7cwf2k) |
| S5.4 | Managed Identity | [doc](M05-Almacenamiento-BBDD/v3-actual/M05-S5.4-managed-identity-v3.md) | ✅ [Gamma](https://gamma.app/docs/M05-S54-managed-identity-v3-pczqq4mze7fbwkq) |
| S5.5 | Backups | [doc](M05-Almacenamiento-BBDD/v3-actual/M05-S5.5-backups-v3.md) | ✅ [Gamma](https://gamma.app/docs/M05-S55-backups-v3-opeii84zson0iyr) |
| S5.P | Práctica | [doc](M05-Almacenamiento-BBDD/v3-actual/M05-S5.P-practica-v3.md) | ✅ [Gamma](https://gamma.app/docs/M05-S5P-practica-v3-i92urasf8zu6f4t) |
| S5.P2 | Práctica — Table Storage CRUD | [doc](M05-Almacenamiento-BBDD/v3-actual/M05-S5.P2-practica-table-storage-crud-v1.md) | ✅ [Gamma](https://gamma.app/docs/M05-S5P2-practica-table-storage-crud-v1-mitxewba96tmidk) |

### M06 — Seguridad y Autenticación (v3)

| Submódulo | Título | Doc | Presentación |
| --- | --- | --- | --- |
| S6.1 | Responsabilidad compartida | [doc](M06-Seguridad-Auth/v3-actual/M06-S6.1-responsabilidad-compartida-v3.md) | ✅ [Gamma](https://gamma.app/docs/M06-S61-responsabilidad-compartida-v3-z2j7sfm3hkt87wu) |
| S6.2 | Microsoft Entra ID | [doc](M06-Seguridad-Auth/v3-actual/M06-S6.2-entra-id-v3.md) | ✅ [Gamma](https://gamma.app/docs/M06-S62-entra-id-v3-9i0dic10neiat9y) |
| S6.3 | OAuth2 / OpenID Connect | [doc](M06-Seguridad-Auth/v3-actual/M06-S6.3-oauth2-openid-connect-v3.md) | ✅ [Gamma](https://gamma.app/docs/M06-S63-oauth2-openid-connect-v3-12jg9nqhgtdqywv) |
| S6.4 | Auth en desktop / MSIX | [doc](M06-Seguridad-Auth/v3-actual/M06-S6.4-auth-desktop-msix-v3.md) | ✅ [Gamma](https://gamma.app/docs/M06-S64-auth-desktop-msix-v3-px3jdg024gualdw) |
| S6.5 | Seguridad de datos | [doc](M06-Seguridad-Auth/v3-actual/M06-S6.5-seguridad-datos-v3.md) | ✅ [Gamma](https://gamma.app/docs/M06-S65-seguridad-datos-v3-cojakvoxa89w8pw) |
| S6.6 | Key Vault | [doc](M06-Seguridad-Auth/v3-actual/M06-S6.6-key-vault-v3.md) | ✅ [Gamma](https://gamma.app/docs/M06-S66-key-vault-v3-33iw7hpns4p2zao) |
| S6.P | Práctica — OAuth2 + Key Vault | [doc](M06-Seguridad-Auth/v3-actual/M06-S6.P-practica-oauth2-keyvault-v3.md) | ✅ [Gamma](https://gamma.app/docs/M06-S6P-practica-oauth2-keyvault-v3-p5sr50wmn8y18m7) |
| S6.P2 | Práctica — Easy Auth | [doc](M06-Seguridad-Auth/v3-actual/M06-S6.P2-practica-easy-auth-v1.md) | ✅ [Gamma](https://gamma.app/docs/M06-S6P2-practica-easy-auth-v1-neozj8hhlfbiwv3) |

### M07 — Integración y MSIX (v3)

| Submódulo | Título | Doc | Presentación |
| --- | --- | --- | --- |
| S7.1 | Service Bus / Event Grid avanzado | [doc](M07-Integracion-MSIX/v3-actual/M07-S7.1-service-bus-event-grid-avanzado-v3.md) | ✅ [Gamma](https://gamma.app/docs/M07-S71-service-bus-event-grid-avanzado-v3-dphl3ncrf3oy5ls) |
| S7.2 | Diseño event-driven | [doc](M07-Integracion-MSIX/v3-actual/M07-S7.2-diseno-event-driven-v3.md) | ✅ [Gamma](https://gamma.app/docs/M07-S72-diseno-event-driven-v3-4hhr6lt8vl2j2p4) |
| S7.3 | API Management | [doc](M07-Integracion-MSIX/v3-actual/M07-S7.3-api-management-v3.md) | ✅ [Gamma](https://gamma.app/docs/M07-S73-api-management-v3-fl0q5hz7r202p6u) |
| S7.4 | ClickOnce vs MSIX | [doc](M07-Integracion-MSIX/v3-actual/M07-S7.4-clickonce-vs-msix-v3.md) | ✅ [Gamma](https://gamma.app/docs/M07-S74-clickonce-vs-msix-v3-ee4p2ff379pg2yw) |
| S7.5 | MSIX — empaquetado y distribución | [doc](M07-Integracion-MSIX/v3-actual/M07-S7.5-msix-empaquetado-distribucion-v3.md) | ✅ [Gamma](https://gamma.app/docs/M07-S75-msix-empaquetado-distribucion-v3-chtzr206fu8vvr7) |
| S7.6 | MSIX — auto-update | [doc](M07-Integracion-MSIX/v3-actual/M07-S7.6-msix-auto-update-v3.md) | ✅ [Gamma](https://gamma.app/docs/M07-S76-msix-auto-update-v3-5htllixrd96ojzj) |
| S7.7 | Migración ClickOnce → MSIX | [doc](M07-Integracion-MSIX/v3-actual/M07-S7.7-migracion-clickonce-msix-v3.md) | ✅ [Gamma](https://gamma.app/docs/M07-S77-migracion-clickonce-msix-v3-i8usmduhmbi517r) |
| S7.P | Práctica — MSIX | [doc](M07-Integracion-MSIX/v3-actual/M07-S7.P-practica-msix-v3.md) | ✅ [Gamma](https://gamma.app/docs/M07-S7P-practica-msix-v3-xuhzrd0gxu7o5n5) |
| S7.P2 | Práctica — MSIX wizard | [doc](M07-Integracion-MSIX/v3-actual/M07-S7.P2-practica-msix-wizard-v1.md) | ✅ [Gamma](https://gamma.app/docs/M07-S7P2-practica-msix-wizard-v1-uqxsxaebep63vw7) |

### M08 — DevOps y Automatización (v3)

| Submódulo | Título | Doc | Presentación |
| --- | --- | --- | --- |
| S8.1 | Azure DevOps — Repos y Boards | [doc](M08-DevOps-Automatizacion/v3-actual/M08-S8.1-azure-devops-repos-boards-v3.md) | ✅ [Gamma](https://gamma.app/docs/M08-S81-azure-devops-repos-boards-v3-0y5b16e5eb3xlak) |
| S8.2 | Pipelines CI/CD YAML | [doc](M08-DevOps-Automatizacion/v3-actual/M08-S8.2-pipelines-cicd-yaml-v3.md) | ✅ [Gamma](https://gamma.app/docs/M08-S82-pipelines-cicd-yaml-v3-e11ff7wix69eltu) |
| S8.3 | Despliegue automatizado | [doc](M08-DevOps-Automatizacion/v3-actual/M08-S8.3-despliegue-automatizado-v3.md) | ✅ [Gamma](https://gamma.app/docs/M08-S83-despliegue-automatizado-v3-7gk397w6ppqqm55) |
| S8.4 | ADO vs GitHub Actions | [doc](M08-DevOps-Automatizacion/v3-actual/M08-S8.4-ado-vs-github-actions-v3.md) | ✅ [Gamma](https://gamma.app/docs/M08-S84-ado-vs-github-actions-v3-d5jqvsvzzj8eea0) |
| S8.5 | IaC con Bicep | [doc](M08-DevOps-Automatizacion/v3-actual/M08-S8.5-iac-bicep-v3.md) | ✅ [Gamma](https://gamma.app/docs/M08-S85-iac-bicep-v3-pssjpacv9y6yc56) |
| S8.6 | App Insights y Monitor | [doc](M08-DevOps-Automatizacion/v3-actual/M08-S8.6-app-insights-monitor-v3.md) | ✅ [Gamma](https://gamma.app/docs/M08-S86-app-insights-monitor-v3-goe1ecns17gjowq) |
| S8.P | Práctica — Pipeline CI/CD | [doc](M08-DevOps-Automatizacion/v3-actual/M08-S8.P-practica-pipeline-cicd-v3.md) | ✅ [Gamma](https://gamma.app/docs/M08-S8P-practica-pipeline-cicd-v3-zxa4gftu0rlaanr) |
| S8.P2 | Práctica — GitHub Actions + publish profile | [doc](M08-DevOps-Automatizacion/v3-actual/M08-S8.P2-practica-github-actions-publish-profile-v1.md) | ✅ [Gamma](https://gamma.app/docs/M08-S8P2-practica-github-actions-publish-profile-v1-ef0q3jcujuv1532) |

### M09 — IA y Claude Code (v3)

| Submódulo | Título | Doc | Presentación |
| --- | --- | --- | --- |
| S9.1 | Claude Code — Introducción | [doc](M09-IA-Claude-Code/v3-actual/M09-S9.1-claude-code-intro-v3.md) | ⏳ |
| S9.2 | Claude Code — Casos de uso | [doc](M09-IA-Claude-Code/v3-actual/M09-S9.2-claude-code-casos-uso-v3.md) | ⏳ |
| S9.3 | Claude Code — Infraestructura | [doc](M09-IA-Claude-Code/v3-actual/M09-S9.3-cc-infraestructura-v3.md) | ⏳ |
| S9.4 | MCP — Herramientas | [doc](M09-IA-Claude-Code/v3-actual/M09-S9.4-mcp-herramientas-v3.md) | ⏳ |
| S9.5 | Buenas prácticas y limitaciones | [doc](M09-IA-Claude-Code/v3-actual/M09-S9.5-buenas-practicas-limitaciones-v3.md) | ⏳ |
| S9.P | Práctica — Claude Code + MCP | [doc](M09-IA-Claude-Code/v3-actual/M09-S9.P-practica-cc-mcp-v3.md) | ⏳ |
| S9.P2 | Práctica — Claude Code primer comando | [doc](M09-IA-Claude-Code/v3-actual/M09-S9.P2-practica-claude-code-primer-comando-v1.md) | ⏳ |

### M10 — Proyecto integrador (v3)

| Submódulo | Título | Doc | Presentación |
| --- | --- | --- | --- |
| S10.1 | Diseño y arquitectura | [doc](M10-Proyecto-Integrador/v3-actual/M10-S10.1-diseno-arquitectura-v3.md) | ⏳ |
| S10.P2 | Práctica — Mini proyecto Notas | [doc](M10-Proyecto-Integrador/v3-actual/M10-S10.P2-practica-mini-proyecto-notas-v1.md) | ⏳ |

### M11 — Bonus: Claude Code en Azure (v1)

| Submódulo | Título | Doc | Presentación |
| --- | --- | --- | --- |
| S11.1 | Introducción a la IA agéntica en Azure | [doc](M11-Bonus-Claude-Code-Azure/v1-actual/M11-S11.1-introduccion-ia-agentica-azure.md) | ⏳ |
| S11.2 | Claude Code — Setup en Azure | [doc](M11-Bonus-Claude-Code-Azure/v1-actual/M11-S11.2-claude-code-setup-azure.md) | ⏳ |
| S11.3 | Skills y capacidades especializadas | [doc](M11-Bonus-Claude-Code-Azure/v1-actual/M11-S11.3-skills-capacidades-especializadas.md) | ⏳ |
| S11.4 | Agentes y subagentes en Azure | [doc](M11-Bonus-Claude-Code-Azure/v1-actual/M11-S11.4-agentes-subagentes-azure.md) | ⏳ |
| S11.5 | MCP y servicios de Azure | [doc](M11-Bonus-Claude-Code-Azure/v1-actual/M11-S11.5-mcp-servicios-azure.md) | ⏳ |
| S11.6 | Claude Code en cada módulo | [doc](M11-Bonus-Claude-Code-Azure/v1-actual/M11-S11.6-claude-code-en-cada-modulo.md) | ⏳ |
| S11.7 | Claude Cowork para Azure | [doc](M11-Bonus-Claude-Code-Azure/v1-actual/M11-S11.7-claude-cowork-para-azure.md) | ⏳ |
| S11.8 | Workflows avanzados de automatización | [doc](M11-Bonus-Claude-Code-Azure/v1-actual/M11-S11.8-workflows-avanzados-automatizacion.md) | ⏳ |
| S11.P | Práctica — Solución Azure + IA | [doc](M11-Bonus-Claude-Code-Azure/v1-actual/M11-S11.P-practica-solucion-azure-ia.md) | ⏳ |
| S11.P2 | Práctica — Claude + Azure light | [doc](M11-Bonus-Claude-Code-Azure/v1-actual/M11-S11.P2-practica-claude-azure-light-v1.md) | ⏳ |

## Flujo de actualización

Cuando me pases una presentación:

1. Marco la fila como ✅ con el enlace en este índice.
2. Añado la línea `> **Presentación Gamma:** [...]` en la cabecera del `.md` del submódulo.
3. Si el submódulo tiene README de ejemplo en `examples/`, añado la misma línea allí.
4. Si la presentación cubre varios submódulos, lo anoto explícitamente en cada fila.
