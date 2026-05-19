# M06 — Seguridad, Autenticación e Identidad · ejemplos

Ejemplos de código que acompañan al
[Módulo 6 — Seguridad y Auth](../../doc/M06-Seguridad-Auth).

Cambia el dominio respecto a M05: ya no son servicios de datos sino
**seguridad transversal** — modelo de responsabilidad compartida, Entra
ID, OAuth2/OIDC, autenticación desktop/MSIX, cifrado y Key Vault. Varios
submódulos son **conceptuales**: el valor está en lógica pura testeable
+ el grafo DI real (patrón establecido en M05-S5.4/S5.5: CAPA 1 + CAPA 0,
sin integración cuando no hay nada emulable; los de Key Vault/Entra sí
pueden llevar integración).

## Submódulos cubiertos

| Submódulo | Tema | Ejemplo | Estado |
| --- | --- | --- | --- |
| [S6.1](../../doc/M06-Seguridad-Auth/v3-actual/M06-S6.1-responsabilidad-compartida-v3.md) | Responsabilidad compartida, defense in depth, STRIDE | [`S6.1-responsabilidad-compartida/`](S6.1-responsabilidad-compartida/README.md) | ✅ Disponible |
| [S6.2](../../doc/M06-Seguridad-Auth/v3-actual/M06-S6.2-entra-id-v3.md) | Microsoft Entra ID (identidades, roles, JWT, App Roles) | [`S6.2-entra-id/`](S6.2-entra-id/README.md) | ✅ Disponible |
| [S6.3](../../doc/M06-Seguridad-Auth/v3-actual/M06-S6.3-oauth2-openid-connect-v3.md) | OAuth2 / OpenID Connect (flujos, PKCE, authorize URL) | [`S6.3-oauth2-oidc/`](S6.3-oauth2-oidc/README.md) | ✅ Disponible |
| S6.4 | Auth desktop / MSIX | _Pendiente_ | ⏳ |
| S6.5 | Seguridad de datos | _Pendiente_ | ⏳ |
| S6.6 | Key Vault | _Pendiente_ | ⏳ |
| S6.P | Práctica — OAuth2 + Key Vault | _Pendiente_ | ⏳ |
| S6.P2 | Práctica — Easy Auth | _Pendiente_ | ⏳ |

## Patrón de tests

- **CAPA 1 · Unit**: la lógica de decisión de seguridad como funciones
  puras (matriz de responsabilidad, STRIDE, detección de secretos,
  cálculo de Secure Score).
- **CAPA 0 · DI**: `WebApplicationFactory` resuelve el grafo real (cubre
  la [lección DI de M03-S3.4](../M04-Azure-Functions-II/S4.5-testing-depuracion/README.md))
  — corre sin Docker.
- **Integración**: solo donde haya algo emulable (Key Vault con
  emulador, etc.); en submódulos puramente conceptuales **no** se fuerza
  una CAPA de integración (documentado en cada README).

## Requisitos comunes

- .NET SDK 10
- (Para los despliegues) suscripción de Azure + Azure CLI
- Docker solo para los submódulos con integración (si aplica)
