# PersonalOS

PersonalOS is a personal platform for planning, executing, recording, and reviewing daily life. It brings together scheduling, tasks, habits, nutrition, journaling, and weekly analysis.

It also serves as a professional case study demonstrating Angular, ASP.NET Core, software architecture, application security, automated testing, CI/CD, and technical documentation.

## Vision

```text
Capture -> Plan -> Execute -> Record -> Review -> Adjust
```

The goal is not to create another task list. PersonalOS should help answer:

- What should I do now?
- What am I neglecting?
- Why am I falling behind?
- What concrete adjustment should I try?
- Am I improving sustainably?

## Technology stack

### Backend

- .NET 10
- ASP.NET Core Web API
- ASP.NET Core Identity
- Entity Framework Core
- SQL Server
- xUnit

### Frontend

- Angular
- TypeScript
- SCSS
- Angular Router
- Angular HttpClient
- Signals
- Typed reactive forms
- The test runner configured by the installed Angular CLI version

### Engineering

- modular monolith;
- REST and OpenAPI;
- same-origin cookie authentication;
- antiforgery protection;
- server-side authorization;
- structured Problem Details;
- rate limiting;
- secure browser-state rules;
- dependency auditing;
- automated tests;
- GitHub Actions;
- living technical documentation.

## Architecture

```mermaid
flowchart LR
    WEB[Angular SPA] -->|HTTPS + JSON + Cookie| API[ASP.NET Core API]
    API --> APP[Application]
    API --> INFRA[Infrastructure]
    APP --> DOMAIN[Domain]
    INFRA --> APP
    INFRA --> DOMAIN
    INFRA --> DB[(SQL Server)]
```

Allowed dependency direction:

```text
Application -> Domain
Infrastructure -> Application + Domain
Api -> Application + Infrastructure
Domain -> no external layer
Angular -> API only
```

The Angular application never accesses SQL Server directly. Route guards improve navigation and user experience, but every protected API operation must enforce authorization on the server.

## Repository structure

```text
PersonalOS/
+-- src/
|   +-- PersonalOS.Domain/
|   +-- PersonalOS.Application/
|   +-- PersonalOS.Infrastructure/
|   +-- PersonalOS.Api/
+-- web/
|   +-- PersonalOS.Web/
+-- tests/
|   +-- PersonalOS.UnitTests/
|   +-- PersonalOS.IntegrationTests/
+-- docs/
|   +-- 01_PRODUCT.md
|   +-- 02_ARCHITECTURE.md
|   +-- 03_DELIVERY.md
+-- AGENTS.md
+-- README.md
+-- global.json
+-- PersonalOS.slnx
```

Angular application structure:

```text
web/PersonalOS.Web/src/app/
+-- core/
|   +-- auth/
|   +-- errors/
|   +-- http/
|   +-- layout/
+-- shared/
|   +-- components/
+-- features/
|   +-- authentication/
|   |   +-- login/
|   |   +-- register/
|   +-- today/
|   +-- settings/
+-- app.config.ts
+-- app.routes.ts
+-- app.ts
```

The Angular implementation uses Angular 22.1.0, standalone components, lazy-loaded feature routes, Vitest through the Angular CLI unit-test builder, and a development proxy for relative `/api` and `/health` calls.

## Documentation

1. [`docs/01_PRODUCT.md`](docs/01_PRODUCT.md): product vision, scope, experience, modules, and roadmap.
2. [`docs/02_ARCHITECTURE.md`](docs/02_ARCHITECTURE.md): architecture, Angular concepts, data, security, and future SaaS evolution.
3. [`docs/03_DELIVERY.md`](docs/03_DELIVERY.md): Milestone 1, API contracts, tests, CI, Definition of Done, and operations.
4. [`AGENTS.md`](AGENTS.md): mandatory rules for Codex, Claude, and other coding agents.

## Current status

- [x] Layered .NET solution scaffold.
- [x] Project references and dependency direction.
- [x] ASP.NET Core Identity and persistence baseline available for audit.
- [x] Angular project scaffold created.
- [x] Previous frontend implementation removed before the Angular migration.
- [x] Product, architecture, delivery, and agent documentation migrated to English and Angular.
- [x] M1: secure Angular authentication walking skeleton implemented locally.
- [x] M1: full local validation gate completed after final review.
- [ ] M1: remote CI validated after push or pull request.
- [ ] M2: profile and time.
- [ ] M3: planning.
- [ ] M4: habits.
- [ ] M5: nutrition.
- [ ] M6: journal.
- [ ] M7: weekly review.
- [ ] M8: PWA and reminders.
- [ ] M9: security hardening and production readiness.

A checked item describes repository preparation or an already established baseline. Feature completion must never be claimed without executing the documented validation commands.

## Security baseline

PersonalOS will eventually store sensitive and highly sensitive personal data. Security is therefore part of the architecture from the first milestone.

Core rules:

- authentication uses an ASP.NET Core Identity cookie;
- the authentication cookie is `HttpOnly`;
- production cookies are `Secure`;
- state-changing requests require antiforgery validation;
- authentication tokens are never stored in `localStorage` or `sessionStorage`;
- the current user is loaded from `/api/auth/me` and kept in memory only;
- Angular route guards are not authorization boundaries;
- user ownership is validated by the API;
- permissive CORS is forbidden;
- Angular environment files never contain secrets;
- backend strings are rendered as text, not trusted HTML;
- passwords, cookies, tokens, connection strings, and sensitive content must not be logged;
- High and Critical dependency vulnerabilities block delivery unless explicitly reviewed and documented.

See [`docs/02_ARCHITECTURE.md`](docs/02_ARCHITECTURE.md) for the complete security model.

## Validation

### Backend

```powershell
dotnet restore .\PersonalOS.slnx
dotnet build .\PersonalOS.slnx --no-restore
dotnet test .\PersonalOS.slnx --no-build
dotnet package list --project .\PersonalOS.slnx --vulnerable --include-transitive
```

### Frontend

Use the scripts defined in `web/PersonalOS.Web/package.json`. The intended validation gate is:

```powershell
npm --prefix .\web\PersonalOS.Web ci
npm --prefix .\web\PersonalOS.Web run lint
npm --prefix .\web\PersonalOS.Web run test -- --watch=false
npm --prefix .\web\PersonalOS.Web run build
npm --prefix .\web\PersonalOS.Web audit --audit-level=high
```

If the installed Angular test runner uses a different non-watch argument, use its supported equivalent and update this documentation.

Quality gate:

- zero compilation errors;
- zero failed tests;
- zero known High or Critical dependency vulnerabilities, unless a documented exception is approved;
- no committed secrets;
- no hard-coded development API URLs in the production bundle;
- documentation updated;
- reviewed diff.

## Local Milestone 1 execution

Restore local tools and apply the existing Development migration:

```powershell
dotnet tool restore
$env:ASPNETCORE_ENVIRONMENT='Development'
dotnet tool run dotnet-ef database update `
  --project .\src\PersonalOS.Infrastructure\PersonalOS.Infrastructure.csproj `
  --startup-project .\src\PersonalOS.Api\PersonalOS.Api.csproj
```

Start the API:

```powershell
dotnet run `
  --project .\src\PersonalOS.Api\PersonalOS.Api.csproj `
  --launch-profile https
```

Start Angular in a second terminal:

```powershell
npm --prefix .\web\PersonalOS.Web start
```

The Angular development proxy must forward relative `/api` and `/health` requests to the HTTPS address defined by the API launch profile. Do not hard-code that address in TypeScript.

## Future evolution

PersonalOS begins as a personal product. Multi-tenancy is not introduced prematurely.

Initial ownership:

```text
AppUser -> resources through UserId
```

Possible future evolution:

```text
AppUser -> WorkspaceMembership -> Workspace -> Resources
```

That evolution would require an explicit review of authorization, isolation, migration, invitations, roles, billing, caching, storage, backups, and tenant-aware jobs.

## Professional objective

This repository should demonstrate that the developer can:

- define a product;
- design architectural boundaries;
- build an Angular frontend and an ASP.NET Core backend;
- protect sensitive data;
- reason about XSS, CSRF, cookies, CORS, and browser storage;
- implement server-side authorization;
- test behavior and negative security cases;
- operate and monitor software;
- document decisions;
- explain why a technology was selected;
- justify what should not be built yet.

## Author

Jefferson Rojas
Costa Rica
Systems Engineering
Focus: .NET, Angular, SaaS, application security, and software architecture.
