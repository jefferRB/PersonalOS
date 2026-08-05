# PersonalOS - Coding Agent Instructions

## 1. Language rule

Everything added or modified in this repository must be written in English:

- source-code identifiers;
- filenames and folders;
- comments;
- XML documentation;
- tests;
- logs and event names;
- API contracts;
- validation messages;
- user-facing text;
- accessibility labels;
- Markdown documentation;
- diagrams and examples.

Do not introduce bilingual repository content.

Do not rename framework-generated migration history or existing database objects only for cosmetic translation when that change creates migration risk. Report unavoidable legacy identifiers instead.

## 2. Read before modifying

Read in this order:

1. `README.md`
2. `docs/01_PRODUCT.md`
3. `docs/02_ARCHITECTURE.md`
4. `docs/03_DELIVERY.md`
5. affected source and test files

Do not resolve contradictions silently. Report the contradiction, determine which source reflects the real implementation, and update the documentation deliberately.

Always inspect:

- current branch and `git status`;
- `PersonalOS.slnx`;
- project references;
- Angular `package.json`;
- `angular.json`;
- TypeScript configuration;
- API `launchSettings.json`;
- authentication, antiforgery, CORS, rate limiting, Problem Details, and health configuration;
- relevant tests.

Use the Angular version that is actually installed. Do not assume APIs from another version.

## 3. Mandatory architecture

Projects:

- `PersonalOS.Domain`
- `PersonalOS.Application`
- `PersonalOS.Infrastructure`
- `PersonalOS.Api`
- `PersonalOS.Web`
- `PersonalOS.UnitTests`
- `PersonalOS.IntegrationTests`

Allowed dependencies:

```text
Application -> Domain
Infrastructure -> Application + Domain
Api -> Application + Infrastructure
UnitTests -> Domain + Application
IntegrationTests -> Api + Infrastructure + Application
Angular -> Api through HTTPS/JSON only
```

Do not add reverse or circular references.

Domain must not depend on:

- EF Core;
- SQL Server;
- ASP.NET Core;
- Identity;
- Angular;
- HTTP.

Angular must not access SQL Server or contain server secrets.

## 4. Current scope

Milestone 3 added the daily operating system: the calendar, recurring routines with workout
recording, nutrition, study, the daily journal, and an integrated Today screen.

Milestone 4 rebuilt the calendar around one aggregate. `PlanningItem` carries a kind, a category, a
priority, and its own recurrence rule; `PlanningItemOccurrenceState` records what the user decided
about a single day, and exists only once they decide something. Today reads the calendar's
occurrence projection rather than a second task model.

Reusable rules that came out of it:

- a local calendar day is a `DateOnly` and is never converted through a time zone on the client;
  build and read `Date` objects with `Date.UTC` and the `getUTC*` accessors;
- recurrence is calculated on demand for the window a screen asks for, never generated as rows;
- an occurrence state row is written only when the user acts on a specific day; the absence of a row
  means planned. Once a day has been acted on, the repetition and start date are frozen, though the
  series may still be ended earlier;
- a month response carries counts and kind indicators, never titles or descriptions;
- kind colours a calendar block; category never does. Every colour is paired with an icon and a
  word, and priority is a badge rather than a hue;
- give every table exactly one cascade parent. SQL Server rejects two cascade paths into the same
  table, so a child of a user-owned aggregate hangs off that aggregate and carries an indexed
  `UserId` column without a second foreign key;
- history must not be rewritten by an edit. A recorded result references the thing it measured by
  identifier, without a cascading foreign key;
- a resource owned by another account returns 404, not 403;
- `<input type="number">` binds through Angular's number value accessor, so a control declared as
  text receives a real `number`. Parsing helpers must accept both or every entered number is
  silently dropped;
- SQLite cannot order by `DateTimeOffset`. Do not use one as a sort key in a store, because the
  behaviour tests would then depend on which provider is running;
- nutrition wording stays factual. Never propose a target, label a value, or give advice.

The first increment includes:

- EF Core and SQL Server;
- ASP.NET Core Identity;
- HttpOnly authentication cookie;
- antiforgery;
- registration;
- login;
- current user;
- logout;
- Angular authentication state;
- protected Angular routes;
- anonymous-only routes;
- protected application shell;
- Today empty state;
- Settings account summary;
- not-found page;
- Problem Details;
- rate-limit handling;
- health checks;
- accessibility baseline;
- backend and frontend tests;
- CI;
- dependency audits.

Do not implement yet:

- weekly review and aggregates;
- trends and export;
- recurrence exceptions and positional rules;
- reminders;
- PWA;
- push notifications;
- AI recommendations;
- password vault;
- workspaces;
- multi-tenancy;
- billing;
- commercial roles;
- email confirmation;
- account recovery;
- MFA;
- passkeys;
- external login;
- production deployment.

Do not create fake persisted data or fake analytics for unfinished modules.

## 5. Angular rules

Use the conventions supported by the installed Angular version.

Required defaults:

- strict TypeScript;
- standalone components;
- no new NgModules;
- feature-oriented folders;
- lazy-loaded feature routes;
- typed reactive forms;
- `HttpClient`;
- functional route guards;
- functional interceptors;
- signals for local or in-memory state where appropriate;
- RxJS for HTTP and real streams;
- relative `/api` URLs;
- accessible semantic HTML;
- SCSS already provided by the project.

Do not:

- use `any` without documented necessity;
- install a global state-management package for authentication;
- install a form library;
- install an HTTP wrapper library;
- hard-code Development ports in TypeScript;
- use experimental APIs without justification and tests;
- replace the configured test runner merely because another tool is familiar;
- create abstractions with no current responsibility;
- use guards as authorization.

## 6. Prohibited by default

Without explicit approval, do not introduce:

- microservices;
- event sourcing;
- MediatR;
- AutoMapper;
- generic repository;
- artificial Unit of Work;
- Redux, NgRx, or another global store;
- JWT in `localStorage` or `sessionStorage`;
- permissive CORS;
- custom cryptography;
- automatic production migrations;
- preview packages;
- mass restructuring outside scope;
- a UI framework or icon package without a demonstrated need;
- automatic Git commits;
- direct merges into `main`.

## 7. Security

Never commit, log, return, persist in browser storage, or display:

- passwords;
- authentication cookies;
- tokens;
- antiforgery tokens;
- authorization headers;
- connection strings containing secrets;
- User Secrets;
- API keys;
- security stamps;
- password hashes;
- sensitive journal content;
- real production data.

Do not remove the direct safe `Microsoft.OpenApi` reference without verifying that the previously addressed transitive vulnerability remains resolved.

### Authentication

- keep ASP.NET Core Identity;
- keep server-managed cookie authentication;
- authentication cookie must be HttpOnly;
- production cookie must be Secure;
- API returns 401 and 403, not HTML redirects;
- `/api/auth/me` is the source of truth;
- Angular keeps the current user in memory only;
- logout clears all private frontend state.

### Antiforgery

- state-changing requests require antiforgery;
- align Angular XSRF cookie and header names with ASP.NET Core;
- do not disable protection on login or registration;
- do not store the request token in browser storage;
- test missing, invalid, and valid token behavior.

### XSS

Do not use untrusted data with:

- `[innerHTML]`;
- direct DOM injection;
- `document.write`;
- `eval`;
- `new Function`;
- dynamic template compilation;
- Angular security-bypass APIs.

Render backend error content as text.

### CORS

Prefer the Angular Development proxy and same-origin production hosting.

Never combine credentialed requests with any-origin CORS.

### Authorization

- enforce every protected operation on the server;
- derive ownership from the authenticated principal;
- never trust a client-supplied `UserId`;
- test anonymous and cross-user access when resources exist.

### Time and time zones

- UTC is the internal source of truth; store instants as UTC;
- read the current instant from `IClock`, never from `DateTime.Now` or `DateTimeOffset.Now`;
- store IANA time-zone identifiers, never a fixed UTC offset;
- validate every submitted identifier on the server and reject Windows-only identifiers;
- never calculate an offset by hand; let `TimeZoneInfo` apply daylight-saving rules;
- treat the browser time zone as a suggestion that requires an explicit user action to save;
- render dates with an explicit English locale rather than the browser default;
- let the server decide the local calendar date and the client decide its wording;
- use a fixed clock in tests so results never depend on when or where they run;
- do not add a time-zone package unless native .NET or browser behaviour is proven insufficient
  through an actual failing test.

### Browser state

Do not store:

- JWTs;
- current-user objects;
- private API responses;
- profile or time-context responses;
- journal content;
- nutrition history;
- habit history.

Angular environment files are public after compilation and must not contain secrets.

### Dependencies

Before adding a package:

1. confirm the framework or browser does not already provide the capability;
2. verify maintenance and framework compatibility;
3. explain why it is needed;
4. update the lockfile;
5. run the audit.

Do not run `npm audit fix --force`.

## 8. Working method

### Before editing

1. inspect branch and repository status;
2. describe the current state;
3. run or record baseline validation;
4. identify architecture and security risks;
5. present a concise plan;
6. list expected files;
7. verify the requested work is inside the current milestone.

### During implementation

1. keep changes cohesive and reviewable;
2. preserve working behavior;
3. respect scope;
4. update tests with behavior;
5. update documentation with reality;
6. use explicit contracts;
7. keep authorization server-side;
8. avoid unrelated upgrades;
9. do not commit unless requested;
10. do not claim success without execution.

### After implementation

1. review `git diff`;
2. check for obsolete files and dependencies from the previous frontend when working on the Angular migration;
3. execute validation;
4. inspect the production Angular output for localhost URLs and accidental secrets;
5. report exact results;
6. disclose commands that failed or were not executed;
7. list remaining risks;
8. provide a manual verification guide;
9. provide an educational explanation;
10. ask a milestone quiz without supplying answers.

## 9. Validation

### Backend

```powershell
dotnet restore .\PersonalOS.slnx
dotnet build .\PersonalOS.slnx --no-restore
dotnet test .\PersonalOS.slnx --no-build
dotnet package list --project .\PersonalOS.slnx --vulnerable --include-transitive
```

### Frontend

Use the scripts defined in `web/PersonalOS.Web/package.json`.

Target gate:

```powershell
npm --prefix .\web\PersonalOS.Web ci
npm --prefix .\web\PersonalOS.Web run lint
npm --prefix .\web\PersonalOS.Web run test -- --watch=false
npm --prefix .\web\PersonalOS.Web run build
npm --prefix .\web\PersonalOS.Web audit --audit-level=high
```

Use the installed test runner's supported non-watch argument.

If a required script does not exist, report it or add it deliberately. Never state that it passed when it was not available.

Do not use `npm audit fix --force`.

## 10. Test expectations

Tests must cover behavior and risk rather than file creation.

Important frontend cases:

- authentication loading;
- current-user success;
- anonymous 401;
- login and registration validation;
- registration without automatic login;
- guard redirects;
- logout cleanup;
- Problem Details;
- rate limiting;
- antiforgery;
- absence of browser-stored authentication;
- keyboard or focus behavior.

Important backend cases:

- registration;
- duplicate user;
- invalid login;
- lockout;
- `/me`;
- logout;
- antiforgery rejection and success;
- 401 and 403 without HTML redirect;
- safe cookie configuration;
- safe Problem Details;
- health checks;
- no sensitive Identity fields in responses.

A SQLite integration host does not prove that SQL Server migrations are valid.

## 11. Completion

A task is complete only when:

- acceptance criteria are satisfied;
- scope is respected;
- architecture is preserved;
- authorization remains server-side;
- security risks are reviewed;
- tests cover relevant behavior;
- no secrets are introduced;
- documentation is updated;
- builds and audits pass or failures are disclosed;
- the diff is reviewed;
- no direct merge into `main` occurred;
- no automatic commit occurred.

## 12. Required final report

The final response for a substantial implementation must include:

1. initial audit;
2. implementation plan;
3. architecture implemented;
4. important files changed;
5. security review covering XSS, CSRF, cookies, storage, CORS, authorization, dependencies, logs, and caching;
6. exact validation commands and results;
7. manual test instructions;
8. known limitations;
9. Angular learning explanation;
10. interview-ready explanation;
11. quiz questions without answers.

Do not hide failures. Do not describe code as secure merely because it compiles.
