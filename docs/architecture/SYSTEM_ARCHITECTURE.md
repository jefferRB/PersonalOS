# PersonalOS — Arquitectura del sistema

**Versión:** 1.0  
**Estado:** Proposed  
**Última actualización:** 2026-07-23

## 1. Drivers

- aprender React conservando .NET;
- entregar valor personal temprano;
- proteger datos sensibles;
- permitir pruebas aisladas e integración;
- mantener bajo costo;
- evolucionar sin asumir SaaS;
- generar evidencia profesional.

## 2. Estilo

Monolito modular con frontend SPA separado.

La separación en proyectos crea límites de compilación. La organización por features crea límites funcionales.

## 3. Contexto

```mermaid
flowchart LR
    U[Usuario] --> W[PersonalOS Web]
    W --> A[PersonalOS API]
    A --> D[(SQL Server)]
    A --> N[Notificaciones futuras]
    A --> O[Observabilidad]
```

## 4. Contenedores

```mermaid
flowchart TB
    subgraph Browser
        WEB[React + TypeScript + Vite]
    end
    subgraph Server
        API[ASP.NET Core Web API]
        WORKER[Workers futuros]
    end
    DB[(SQL Server)]
    WEB -->|HTTPS JSON + Cookie| API
    API --> DB
    WORKER --> DB
```

## 5. Componentes backend

```mermaid
flowchart LR
    API[Api] --> APP[Application]
    API --> INFRA[Infrastructure]
    INFRA --> APP
    INFRA --> DOMAIN[Domain]
    APP --> DOMAIN
```

### Domain

- entidades;
- value objects;
- invariantes;
- eventos solo con necesidad.

### Application

- casos de uso;
- interfaces;
- políticas;
- DTO internos.

### Infrastructure

- EF Core;
- Identity;
- SQL Server;
- servicios;
- workers;
- almacenamiento.

### API

- HTTP;
- auth;
- antiforgery;
- ProblemDetails;
- rate limiting;
- DI;
- health.

## 6. Módulos futuros

```text
Identity
Planning
Habits
Nutrition
Journal
Reviews
Insights
Notifications
Exports
```

Cada módulo debe tener ownership, contrato, reglas, casos de uso, pruebas y docs.

No crear un proyecto por módulo hasta que el tamaño lo justifique.

## 7. Frontend

```text
src/
├── app/
├── components/
├── features/
├── lib/
├── pages/
└── styles/
```

Reglas:

- remoto en TanStack Query;
- formularios en React Hook Form;
- validación Zod;
- estado local cerca;
- no store global por defecto;
- HTTP centralizado;
- features no importan internals ajenos.

## 8. Sesión

```mermaid
sequenceDiagram
    participant B as Browser
    participant R as React
    participant A as API
    participant I as Identity
    participant DB as SQL Server

    B->>R: Abre app
    R->>A: GET /api/auth/me
    A-->>R: 401 o usuario
    R->>A: GET /api/antiforgery/token
    A-->>R: Request token
    R->>A: POST /api/auth/login + header
    A->>I: Validar
    I->>DB: Consultar
    DB-->>I: Resultado
    I-->>A: Sign-in
    A-->>B: Set-Cookie HttpOnly
    R->>A: GET /api/auth/me
    A-->>R: Perfil mínimo
```

## 9. Datos

- Identity administra identidad.
- Recursos futuros contienen `UserId`.
- Filtro de ownership en servidor.
- Frontend nunca autoriza.
- DB refuerza invariantes.
- objetivos temporales versionados.

## 10. Despliegue

```mermaid
flowchart LR
    DEV[Developer] --> GIT[GitHub]
    GIT --> CI[GitHub Actions]
    CI --> ART[Artefactos]
    ART --> STG[Staging]
    STG --> PROD[Production]
    PROD --> DB[(SQL Server)]
    PROD --> OBS[Logs/Metrics/Alerts]
```

M1 implementa CI. Staging/production después.

## 11. Evolución SaaS

Solo con evidencia:

```text
AppUser
  └── WorkspaceMembership
          └── Workspace
```

Requiere usuarios reales, sharing, roles, aislamiento, threat model, ADR y migración probada.

## 12. Simplicidad

Toda tecnología nueva requiere:

- problema;
- alternativas;
- costo;
- riesgo;
- reversibilidad;
- ADR cuando sea relevante.

## 13. Revisiones

- fin de milestone;
- antes de auth pública;
- antes de recordatorios;
- antes de datos sensibles;
- antes de IA;
- antes de SaaS;
- antes de producción.
