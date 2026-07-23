# PersonalOS — Technical Requirements Document

**Versión:** 1.0  
**Estado:** Proposed  
**Última actualización:** 2026-07-23

## 1. Propósito

Definir requisitos técnicos y atributos de calidad. El TRD describe qué debe garantizar la solución; los ADR explican por qué se eligieron decisiones específicas.

## 2. Arquitectura objetivo

- monolito modular;
- ASP.NET Core Web API en .NET 10;
- React + TypeScript + Vite;
- EF Core + SQL Server;
- ASP.NET Core Identity con claves `Guid`;
- cookies HttpOnly same-origin;
- API REST JSON;
- PWA posterior;
- workers persistentes posteriores;
- despliegue separado de LuxuryCloud.

## 3. Estructura

```text
PersonalOS/
├── src/
│   ├── PersonalOS.Domain/
│   ├── PersonalOS.Application/
│   ├── PersonalOS.Infrastructure/
│   └── PersonalOS.Api/
├── web/PersonalOS.Web/
├── tests/
├── docs/
├── .github/
├── AGENTS.md
├── global.json
└── PersonalOS.slnx
```

## 4. Dependencias

- Domain no depende de frameworks de infraestructura o presentación.
- Application depende de Domain.
- Infrastructure depende de Application y Domain.
- Api depende de Application e Infrastructure.
- React solo se comunica con API.
- Ningún módulo accede a internals de otro sin contrato.

## 5. Atributos de calidad

### Seguridad

- Identity;
- antiforgery;
- mínimo privilegio;
- secretos externos;
- logs seguros;
- dependency audits;
- validación server-side.

### Mantenibilidad

- límites de compilación;
- código explícito;
- ADRs;
- pruebas;
- convenciones;
- paquetes justificados.

### Confiabilidad

- idempotencia;
- recordatorios persistidos;
- migraciones ensayadas;
- health;
- backups;
- tiempo correcto.

### Usabilidad

- captura rápida;
- loading/empty/error;
- accesibilidad;
- responsive.

### Observabilidad

- logs;
- métricas;
- trazas;
- correlation ID;
- health;
- alertas.

## 6. Versiones

- SDK fijado por `global.json`;
- Node LTS;
- versiones estables;
- `package-lock.json`;
- NuGet/npm audit;
- sin preview salvo ADR;
- actualizaciones aisladas cuando sea posible.

## 7. Backend

### API

- JSON;
- ProblemDetails;
- códigos HTTP correctos;
- CancellationToken;
- validación de frontera;
- paginación;
- límites;
- OpenAPI solo donde corresponda;
- sin stack trace en producción.

### Application

- casos de uso por feature;
- contratos;
- no EF Core ni HTTP;
- reloj y usuario actual abstraídos cuando aplique.

### Domain

- invariantes;
- value objects útiles;
- entidades con comportamiento real;
- sin capas ceremoniales.

### Infrastructure

- DbContext;
- Identity;
- SQL Server;
- migraciones;
- servicios externos;
- workers;
- configuración.

## 8. Identidad

- `AppUser : IdentityUser<Guid>`;
- `IdentityRole<Guid>`;
- `DisplayName`;
- `CreatedAtUtc`;
- email único;
- lockout;
- security stamp;
- cookie estable;
- 401/403 sin redirect HTML.

Endpoints:

```text
POST /api/auth/register
POST /api/auth/login
POST /api/auth/logout
GET  /api/auth/me
GET  /api/antiforgery/token
```

Fuera del primer incremento:

- email confirmation;
- recovery;
- 2FA;
- passkeys;
- external providers.

## 9. CSRF

- token antiforgery;
- header `X-XSRF-TOKEN`;
- métodos mutables validados;
- proxy Vite;
- mismo origen lógico;
- no `AllowAnyOrigin`.

## 10. Datos

- `Guid`;
- `DateTimeOffset` UTC para instantes;
- `DateOnly` para día local;
- zona IANA;
- índices;
- unique constraints;
- objetivos versionados;
- soft delete solo con necesidad.

Ownership inicial: `UserId`. No `TenantId` o `WorkspaceId` anticipado.

## 11. Tiempo

- instantes UTC;
- conversión en fronteras;
- no `DateTime.Now` en lógica;
- reloj inyectable;
- pruebas de cambio de día y zona;
- no derivar “hoy” solo del servidor.

## 12. Frontend

- TypeScript strict;
- React Router;
- TanStack Query;
- React Hook Form + Zod;
- fetch centralizado;
- `credentials: include`;
- ProblemDetails tipado;
- no credenciales en storage;
- estado local cercano;
- accesibilidad;
- responsive.

## 13. Integración

- DTOs explícitos;
- manejo de 401, 403, 409, 422, 429;
- trace ID;
- proxy `/api`;
- mismo dominio en producción;
- contratos probados posteriormente.

## 14. Pruebas

- unitarias;
- integración;
- componentes React;
- E2E críticos;
- migraciones en SQL Server real;
- seguridad;
- accesibilidad;
- restore drills;
- independencia de orden.

## 15. CI

- restore;
- build;
- backend tests;
- npm ci;
- lint;
- frontend tests;
- frontend build;
- dependency audit;
- secret scan.

## 16. Configuración

- appsettings sin secretos;
- Development sin credenciales;
- User Secrets local;
- variables/secret store en producción;
- no `.env` real versionado.

## 17. Rendimiento inicial

Objetivos orientativos:

- API simple p95 < 500 ms;
- acción primaria visible < 2 s;
- no N+1;
- paginación;
- payload mínimo;
- medir antes de cachear.

## 18. Recuperación

- live/ready;
- backup diario en producción;
- restore probado;
- deploy reversible;
- RPO inicial 24 h;
- RTO inicial 4 h.

## 19. Dependencias

- cero High/Critical al fusionar;
- Medium evaluada;
- mitigaciones directas documentadas;
- conservar `Microsoft.OpenApi` directo mientras sea necesario.

## 20. Salida del walking skeleton

- auth real;
- cookie HttpOnly;
- antiforgery probado;
- migración;
- SQL Server;
- ruta protegida;
- health;
- builds;
- tests;
- CI;
- docs;
- sin secretos;
- audits limpios.
