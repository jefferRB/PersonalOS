# PersonalOS - Implementation, Quality, and Operations

**Version:** 1.1
**Status:** Milestone 1 Angular walking skeleton

## 1. Milestone 1

Objective:

```text
Angular -> ASP.NET Core API -> Identity -> EF Core -> SQL Server
```

Observable result:

1. open the application;
2. see authentication loading without private-content flash;
3. register;
4. return to login without being signed in automatically;
5. sign in explicitly;
6. open a protected Today page;
7. reload and keep the server session;
8. query the current user;
9. open Settings;
10. sign out;
11. become unable to access protected routes;
12. receive safe validation, authentication, and rate-limit feedback.

Milestone 1 registration decision:

- `POST /api/auth/register` creates the account;
- registration does not automatically authenticate;
- Angular shows a safe success message on the login page;
- the user signs in explicitly.

## 2. Milestone 1 scope

### Backend

- `AppUser`;
- `IdentityRole<Guid>`;
- `ApplicationDbContext`;
- SQL Server;
- initial migration;
- ASP.NET Core Identity;
- server-managed authentication cookie;
- antiforgery;
- rate limiting;
- Problem Details;
- live and ready health checks;
- OpenAPI in Development only;
- integration tests;
- safe 401 and 403 API behavior;
- safe authentication logging.

### Angular frontend

- standalone components;
- Angular Router;
- lazy-loaded feature routes;
- Angular HttpClient;
- typed API contracts;
- typed reactive forms;
- signals for authentication and local state where appropriate;
- functional route guards;
- functional HTTP interceptors;
- Problem Details handling;
- Angular-compatible antiforgery;
- login page;
- registration page;
- protected application shell;
- Today page with a truthful first-use empty state;
- Settings page with non-sensitive account information;
- not-found page;
- logout;
- responsive navigation;
- accessibility baseline;
- frontend tests;
- lint or configured static analysis.

### Delivery

- CI;
- English technical documentation;
- npm and NuGet auditing;
- manual verification;
- reviewed diff;
- no automatic commit.

## 3. Outside Milestone 1

- task persistence;
- habits;
- nutrition;
- journal;
- reminders;
- PWA;
- push notifications;
- email confirmation;
- account recovery;
- MFA;
- passkeys;
- external login;
- workspaces;
- multi-tenancy;
- billing;
- final visual design;
- production deployment;
- AI recommendations;
- password manager or vault.

Navigation may reserve space for future modules only when it clearly communicates that they are not yet available. No fake persisted data or fake metrics are allowed.

## 4. Milestone 1 API

| Method | Route | Authentication | Antiforgery |
|---|---|---:|---:|
| GET | `/api/antiforgery/token` | No | N/A |
| POST | `/api/auth/register` | No | Yes |
| POST | `/api/auth/login` | No | Yes |
| GET | `/api/auth/me` | Yes | No |
| POST | `/api/auth/logout` | Yes | Yes |
| GET | `/health/live` | No | No |
| GET | `/health/ready` | No | No |

Registration request:

```json
{
  "displayName": "Jefferson",
  "email": "user@example.com",
  "password": "example-only"
}
```

Login request:

```json
{
  "email": "user@example.com",
  "password": "example-only",
  "rememberMe": true
}
```

Current-user response:

```json
{
  "id": "guid",
  "displayName": "Jefferson",
  "email": "user@example.com"
}
```

The user identifier is part of the API contract but does not need to be displayed in the UI without a defined user benefit.

Errors:

- use `application/problem+json`;
- do not include stack traces;
- do not include SQL;
- do not include internal type names;
- do not include secrets;
- do not reveal whether a login email exists;
- do not return Identity entities directly.

## 5. Angular Milestone 1

### Implemented structure

```text
web/PersonalOS.Web/src/app/
+-- core/
|   +-- auth/
|   |   +-- antiforgery.service.ts
|   |   +-- auth-api.service.ts
|   |   +-- auth.guards.ts
|   |   +-- auth.models.ts
|   |   +-- auth.service.ts
|   |   +-- auth.store.ts
|   +-- errors/
|   |   +-- problem-details.ts
|   +-- http/
|   |   +-- http-error.interceptor.ts
|   +-- layout/
|       +-- application-shell/
+-- shared/
|   +-- components/
|       +-- not-found/
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

The implementation uses Angular 22.1.0 standalone components, functional guards, a functional HTTP interceptor, typed reactive forms, signals for in-memory authentication state, and Vitest through Angular's unit-test builder.

### Startup

```text
GET /api/auth/me
-> authentication state: loading
-> 200: authenticated application
or
-> 401: anonymous login experience
```

Rules:

- `/me` is server state;
- current-user state stays in memory;
- no authentication token in `localStorage`;
- no authentication token in `sessionStorage`;
- no current-user object in browser storage;
- no private-content flash;
- login refreshes current-user state;
- logout clears private state;
- route guards improve navigation only;
- the API remains responsible for authorization;
- client validation improves UX;
- the server validates again.

### Public routes

- `/login`;
- `/register`.

Authenticated users are redirected away from anonymous-only routes.

### Protected routes

- `/app/today`;
- `/app/settings`.

Anonymous users are redirected to login.

### Today page

The initial page:

- greets the authenticated user;
- shows the current local date;
- explains the PersonalOS cycle;
- presents a useful empty state;
- does not invent tasks, habits, nutrition, trends, or productivity scores.

### Settings page

The initial page:

- shows the current display name and email;
- explains that the session is server managed;
- provides logout;
- does not show cookie values, tokens, claims, security stamps, or internal account data.

## 6. Authentication and antiforgery flow

### Authentication

```text
Login form
-> obtain or ensure XSRF request token
-> POST /api/auth/login
-> server creates HttpOnly authentication cookie
-> GET /api/auth/me
-> authenticated in-memory state
```

Angular never reads the authentication cookie.

### Antiforgery

```text
GET /api/antiforgery/token
-> server creates antiforgery token pair
-> readable request-token cookie uses the agreed Angular-compatible name
-> state-changing request sends X-XSRF-TOKEN
-> server validates
```

Requirements:

- login, registration, logout, POST, PUT, PATCH, and DELETE are protected;
- no token is saved to browser storage;
- missing token is rejected safely;
- invalid token is rejected safely;
- tests cover rejection and success;
- CORS is not used as a replacement.

## 7. Backend tests

Minimum coverage:

1. valid registration;
2. duplicate registration;
3. invalid registration;
4. registration does not authenticate automatically;
5. valid login;
6. invalid login;
7. generic invalid-login response;
8. lockout;
9. `/me` without a session;
10. `/me` with a session;
11. logout;
12. POST without antiforgery;
13. POST with antiforgery;
14. invalid antiforgery;
15. live health check;
16. ready health check;
17. 401 without HTML redirect;
18. 403 without HTML redirect;
19. authentication cookie configuration;
20. sanitized Problem Details;
21. current-user response excludes sensitive Identity fields;
22. rate limiting where it can be tested reliably.

SQLite in-memory may be used for fast integration tests, but it does not replace SQL Server migration testing.

Current integration strategy may use:

- `WebApplicationFactory`;
- SQLite in-memory;
- an open SQLite connection for each factory;
- `EnsureCreated()` only inside the isolated test host when the SQL Server migration contains non-portable types.

This strategy validates HTTP and application behavior. It does not validate SQL Server migration scripts.

## 8. Angular tests

Minimum behavior coverage:

1. login form validation;
2. registration form validation;
3. password-confirmation behavior;
4. duplicate-submit prevention;
5. successful login;
6. invalid login;
7. registration does not authenticate;
8. successful registration returns to login;
9. authentication startup loading state;
10. `/me` success creates authenticated state;
11. `/me` 401 creates anonymous state;
12. anonymous user redirected from a protected route;
13. authenticated user redirected from login or registration;
14. Today renders the authenticated display name;
15. logout clears private state;
16. logout redirects to login;
17. Problem Details parsing;
18. rate-limit feedback;
19. antiforgery behavior for a state-changing request;
20. no authentication token is written to `localStorage`;
21. no current-user object is written to `localStorage`;
22. not-found route behavior;
23. one meaningful keyboard or focus interaction;
24. no private-content flash while authentication is unresolved.

Use Angular's configured test infrastructure and HttpClient testing utilities. Avoid tests that only assert that a component exists.

## 9. Manual Milestone 1 verification

1. apply the Development migration;
2. start the API;
3. start Angular;
4. open the application;
5. verify the authentication loading state;
6. open registration;
7. submit invalid values;
8. verify field and form-level errors;
9. register a valid account;
10. verify navigation to login with a success message;
11. confirm registration did not create a signed-in session;
12. sign in;
13. confirm `/app/today` loads;
14. confirm the display name appears;
15. reload the browser;
16. confirm the session remains through `/api/auth/me`;
17. inspect browser storage and confirm no auth token or current-user object exists;
18. open `/app/settings`;
19. confirm no cookie, token, claim, or security stamp is displayed;
20. sign out;
21. confirm private in-memory state is cleared;
22. request `/app/today`;
23. confirm redirect to login;
24. test invalid login;
25. test a state-changing request without antiforgery;
26. confirm safe rejection;
27. verify `/health/live`;
28. verify `/health/ready`;
29. verify no unexpected browser-console errors;
30. verify no High or Critical dependency vulnerability remains.

## 10. CI

### Backend

```text
restore
build --no-restore
test --no-build
NuGet vulnerable-package audit
```

### Frontend

```text
npm ci
lint or configured static analysis
test in non-watch mode
production build
npm audit at High severity
```

### Commands from the repository root

```powershell
dotnet restore .\PersonalOS.slnx
dotnet build .\PersonalOS.slnx --no-restore
dotnet test .\PersonalOS.slnx --no-build
dotnet package list --project .\PersonalOS.slnx --vulnerable --include-transitive

npm --prefix .\web\PersonalOS.Web ci
npm --prefix .\web\PersonalOS.Web run lint
npm --prefix .\web\PersonalOS.Web run test -- --watch=false
npm --prefix .\web\PersonalOS.Web run build
npm --prefix .\web\PersonalOS.Web audit --audit-level=high
```

Use the exact non-watch argument supported by the installed Angular test runner. If a required script does not exist yet, create it deliberately or report the gap; do not pretend it ran.

Quality gate:

- zero build errors;
- zero failed tests;
- zero known High or Critical vulnerabilities, unless an approved documented exception exists;
- no committed secrets;
- no hard-coded localhost URL in the production Angular bundle;
- no obsolete dependencies or files from the previous frontend;
- documentation matches the implementation;
- diff reviewed.

## 11. Definition of Done

A feature is complete only when all relevant criteria are met.

### Product

- problem defined;
- acceptance criteria satisfied;
- out-of-scope boundaries respected;
- alternate states covered;
- unfinished capability not represented as complete.

### Architecture

- dependency direction preserved;
- no unnecessary technology;
- contracts are explicit;
- Angular communicates only with the API;
- server authorization remains authoritative.

### Backend

- server validation;
- authorization;
- ownership;
- `CancellationToken` where relevant;
- Problem Details;
- safe logs;
- reviewed queries;
- reviewed migration;
- API DTOs do not expose persistence entities.

### Frontend

- loading;
- empty, when applicable;
- error;
- success;
- responsive behavior;
- keyboard support;
- labels;
- visible focus;
- semantic HTML;
- correct private-state cleanup;
- no browser-stored authentication data.

### Security

- ownership reviewed;
- antiforgery reviewed;
- XSS reviewed;
- cookie settings reviewed;
- CORS reviewed;
- rate limiting reviewed;
- no secrets;
- negative tests;
- dependency audit clean;
- logs contain no sensitive values;
- production bundle inspected.

### Tests

- unit, integration, and frontend tests selected by risk;
- tests independent of execution order;
- error and negative cases;
- no false claim that SQLite proves SQL Server migrations.

### Delivery

- restore;
- build;
- tests;
- lint or static analysis;
- audits;
- reviewed diff;
- updated documentation;
- no direct merge into `main`;
- no automatic commit unless explicitly requested.

## 12. Work with coding agents

Flow:

```text
Context -> Audit -> Plan -> Implementation -> Diff -> Tests -> Security review -> Human decision
```

Rules:

- one agent may implement;
- another agent may audit;
- Jefferson executes or reviews critical commands;
- do not accept large changes without reviewing the diff;
- do not accept cryptography, authentication, or authorization changes without review;
- do not make automatic commits unless requested;
- do not report success without execution evidence;
- do not hide failed commands.

## 13. Angular learning record

For each milestone, record:

- Angular concept;
- MVC equivalent;
- problem solved;
- component responsibility;
- local state versus server state;
- signal or observable decision;
- dependency-injection decision;
- guard or interceptor responsibility;
- error discovered;
- test that protects the behavior;
- security implication;
- interview explanation.

Milestone 1 must answer:

- Why is `/api/auth/me` server state?
- Why is the authentication state unknown during startup?
- How is private-content flash prevented?
- Why are signals appropriate for in-memory auth state?
- When is RxJS more appropriate than a signal?
- Why are route guards not authorization?
- Why is no JWT stored in `localStorage`?
- How do Angular and ASP.NET Core cooperate for antiforgery?
- What is the difference between the authentication cookie and the XSRF request-token cookie?
- How does Angular reduce XSS risk?
- Why is `[innerHTML]` avoided?
- What is the role of an HTTP interceptor?
- What is the role of a typed reactive form?
- What state must be cleared on logout?
- What does CSP add, and what does it not replace?

## 14. Future operations

Before production:

- staging;
- TLS;
- validated same-origin hosting;
- live and ready health checks;
- structured logs;
- metrics;
- alerts;
- backups;
- tested restore;
- rollback;
- email confirmation;
- account recovery;
- MFA or passkeys;
- production CSP;
- Trusted Types evaluation;
- security headers;
- abuse monitoring;
- source-map decision;
- dependency-update process;
- incident-response notes.

Guidance targets:

- simple API p95 below 500 ms;
- availability target of 99.5%;
- RPO of 24 hours;
- RTO of 4 hours.

These are planning targets, not promises. Do not claim them until they are measured and operationally supported.

## 15. Interview explanation

> PersonalOS is a personal productivity platform built with Angular and ASP.NET Core. I chose a modular monolith to keep clear boundaries without adding premature microservices. Angular consumes a typed JSON API, while ASP.NET Core remains responsible for Identity, authorization, persistence, rate limiting, and security enforcement. Authentication uses an HttpOnly server cookie rather than a JWT in localStorage, and state-changing requests use antiforgery protection. The frontend initializes the session through `/api/auth/me`, keeps private state in memory, and uses guards only for navigation. The project includes negative security tests, dependency audits, CI, accessibility work, and living documentation.

Decisions that must be defendable:

- Angular instead of Razor for this learning project;
- keeping ASP.NET Core as the backend;
- modular monolith instead of microservices;
- cookies instead of JWT in browser storage;
- antiforgery for state-changing requests;
- same-origin architecture instead of permissive CORS;
- SQL Server to reduce backend uncertainty;
- no premature multi-tenancy;
- tests selected by risk;
- minimal, living documentation.
