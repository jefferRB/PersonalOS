# PersonalOS - Implementation, Quality, and Operations

**Version:** 1.3
**Status:** Milestone 3 daily operating system

## 0. Milestone 3

Objective:

```text
Angular daily modules -> ASP.NET Core daily API -> SQL Server
IClock + persisted IANA time zone -> one local day seen from every module at once
```

Observable result:

1. sign in;
2. Today shows the correct local date, in English, decided by the server;
3. Today shows a daily timeline;
4. create a timed task and see it on the timeline immediately;
5. mark it complete and see the completion survive a reload;
6. create a repeating activity and find it on every matching future day;
7. open a workout routine and record sets, repetitions, and weight;
8. record a meal and see Today's calorie total move against the configured target;
9. record a study session and a progress note;
10. write the daily reflection and reload it;
11. see the calendar for the selected month and day;
12. confirm a second account sees none of the first account's data.

### Milestone 3 scope

Domain:

- `PlanningItem` with status, priority, and an idempotent completion;
- `RecurrenceRule` as an owned value object, with calculated occurrences;
- `RoutineTemplate`, `RoutineStep`, `RoutineSession`, `RoutineStepResult`;
- `NutritionGoal` and `MealEntry` with technical, non-medical ranges;
- `StudyProject`, `StudyResource`, `StudySession`, with `ExternalUrlRules`;
- `DailyJournalEntry`, one per account per local day;
- shared `TextRules` for trimming and length.

Application:

- one service and one persistence port per module;
- `RoutineOccurrenceCalculator`, a pure function over rules and recorded sessions;
- `TodayService`, which composes the feature services rather than adding a sixth query path;
- shared `OperationResult<T>` and `ValidationErrorCollector`.

Infrastructure:

- eleven entity configurations, EF Core stores, and one `AddDailyOperatingSystem` migration.

API:

- `TodayController`, `PlanningController`, `RoutinesController`, `RoutineSessionsController`,
  `NutritionController`, `MealsController`, `StudyController`, `JournalController`;
- explicit request contracts and response contracts for every route;
- enumerations serialized as camel-case names rather than as numbers;
- five write-only rate-limit policies.

Angular:

- `core/calendar`, `core/routines`, `core/nutrition`, `core/study`, `core/journal`, `core/today`;
- `core/time/local-date.ts` for calendar arithmetic on native `Date`;
- `core/forms/validators.ts`, shared by every capture form;
- lazy features: Today, Calendar, Routines, Routine detail, Nutrition, Study, Journal;
- sidebar extended to seven destinations.

### Outside Milestone 3

Everything in section 3, plus: exception dates and positional recurrence rules, drag and drop,
file upload, a nutrition database, analytics and trends, notifications, PWA, and the password
vault, which stays excluded until it has its own threat model and cryptographic review.

### Milestone 3 API

| Method | Route | Authentication | Antiforgery | Rate limit |
|---|---|---:|---:|---:|
| GET | `/api/today?date=` | Yes | No | No |
| GET | `/api/calendar/month?year=&month=` | Yes | No | No |
| GET | `/api/calendar/day?date=` | Yes | No | No |
| GET | `/api/calendar/upcoming?from=` | Yes | No | No |
| GET | `/api/calendar/items/{id}` | Yes | No | No |
| POST | `/api/calendar/items` | Yes | Yes | `calendar` |
| PUT | `/api/calendar/items/{id}` | Yes | Yes | `calendar` |
| DELETE | `/api/calendar/items/{id}` | Yes | Yes | `calendar` |
| PUT | `/api/calendar/items/{id}/occurrences/{date}/status` | Yes | Yes | `calendar` |
| GET | `/api/routines?activeOnly=` | Yes | No | No |
| GET | `/api/routines/occurrences?from=&to=` | Yes | No | No |
| GET | `/api/routines/{id}` | Yes | No | No |
| POST | `/api/routines` | Yes | Yes | `routines` |
| PUT | `/api/routines/{id}` | Yes | Yes | `routines` |
| DELETE | `/api/routines/{id}` | Yes | Yes | `routines` |
| POST | `/api/routines/{id}/sessions` | Yes | Yes | `routines` |
| GET | `/api/routine-sessions/{sessionId}` | Yes | No | No |
| PUT | `/api/routine-sessions/{sessionId}` | Yes | Yes | `routines` |
| GET | `/api/nutrition/day?date=` | Yes | No | No |
| GET | `/api/nutrition/goal` | Yes | No | No |
| PUT | `/api/nutrition/goal` | Yes | Yes | `nutrition` |
| POST | `/api/meals` | Yes | Yes | `nutrition` |
| PUT | `/api/meals/{id}` | Yes | Yes | `nutrition` |
| DELETE | `/api/meals/{id}` | Yes | Yes | `nutrition` |
| GET | `/api/study/projects` | Yes | No | No |
| POST | `/api/study/projects` | Yes | Yes | `study` |
| PUT | `/api/study/projects/{id}` | Yes | Yes | `study` |
| GET | `/api/study/sessions?from=&to=` | Yes | No | No |
| POST | `/api/study/sessions` | Yes | Yes | `study` |
| PUT | `/api/study/sessions/{id}` | Yes | Yes | `study` |
| DELETE | `/api/study/sessions/{id}` | Yes | Yes | `study` |
| GET | `/api/journal/{date}` | Yes | No | No |
| PUT | `/api/journal/{date}` | Yes | Yes | `journal` |

Rules:

- every route requires authentication and derives the account from the authentication cookie;
- every response carries `Cache-Control: no-store`;
- a resource owned by another account returns 404, not 403, because confirming that an identifier
  names something real would leak information;
- request contracts carry no account identifier, no status, and no timestamps, so over-posting
  cannot change ownership or fake a completion instant;
- validation errors use camel-case field names and never repeat the submitted text;
- reads are not rate limited, because limiting ordinary navigation makes the application feel
  broken long before it stops anything abusive.

Rate-limit policies, all fixed one-minute windows partitioned by client address:

| Policy | Permits | Why |
|---|---:|---|
| `auth` | 20 | pre-existing |
| `profile` | 30 | pre-existing |
| `planning` | 120 | checking off a morning of tasks is a burst from one honest user |
| `routines` | 120 | a session is saved after each step |
| `nutrition` | 90 | meals are entered a few times a day, with edits |
| `study` | 90 | one write per study block |
| `journal` | 30 | strictest: the most sensitive text, the least throughput needed, and still one save every two seconds |

### Milestone 3 migration

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'

dotnet ef migrations script AddUserPreferences AddDailyOperatingSystem `
  --project .\src\PersonalOS.Infrastructure\PersonalOS.Infrastructure.csproj `
  --startup-project .\src\PersonalOS.Api\PersonalOS.Api.csproj

dotnet ef database update `
  --project .\src\PersonalOS.Infrastructure\PersonalOS.Infrastructure.csproj `
  --startup-project .\src\PersonalOS.Api\PersonalOS.Api.csproj
```

`AddDailyOperatingSystem` is purely additive: eleven `CREATE TABLE`, eight indexes, four unique
indexes, and eleven cascade foreign keys. It contains no `DROP`, no `ALTER` of an existing table,
and no change to any Identity table or to `UserPreferences`. Existing accounts keep their data and
need no backfill, because every new table starts empty.

### Manual Milestone 3 verification

1. apply the migration and confirm existing accounts still sign in;
2. start the API and Angular;
3. sign in and confirm Today shows the correct local date in English;
4. create a timed task from the Today quick-add panel and see it on the timeline;
5. mark it complete, reload, and confirm the completion persisted;
6. open Calendar and create an activity that repeats weekly;
7. navigate to a matching future day and confirm the occurrence appears there and not on the
   neighbouring days;
8. create a workout routine and add three exercise steps;
9. start today's session, record sets, repetitions, and weight, and save partial progress;
10. complete the routine and confirm Today's routine counter moves;
11. open Nutrition, set a calorie target, and record breakfast;
12. confirm the Today calorie summary shows consumed against target;
13. open Study, create a project, record a session with a progress note, and add an `https` link;
14. confirm a `javascript:` link is refused in the browser and by the server;
15. open Journal, save the reflection, reload, and confirm it persisted;
16. confirm `localStorage`, `sessionStorage`, and IndexedDB hold nothing;
17. sign in with a second account and confirm none of the first account's data is visible;
18. send a write without `X-XSRF-TOKEN` and confirm a safe rejection that stores nothing;
19. confirm the browser console has no unexpected errors.

## 0.0.1 Post-Milestone 3 appearance refinement

Objective:

```text
Settings appearance control -> ThemeService -> semantic CSS tokens
index.html pre-render script -> no visible theme flash
```

Observable result:

1. open Settings;
2. choose Light, Dark, or System;
3. reload and keep the selected appearance;
4. choose System and confirm the app follows the operating-system preference;
5. confirm the theme applies before Angular renders;
6. confirm no account, token, profile, journal, meal, routine, study, or calendar data is written to
   browser storage.

Design and security decisions:

- theme state is non-sensitive client state and is not sent to the API;
- the only persisted key is `personalos.themePreference`;
- invalid stored values fall back to System;
- semantic tokens define app background, sidebar background, surfaces, text, borders, brand,
  success, warning, danger, information, focus, disabled state, overlay, and shadows;
- existing color aliases still map to the semantic tokens so older daily screens inherit the theme;
- the pre-render script runs before Angular starts, sets `data-theme`, and updates
  `color-scheme`;
- no dependency, database migration, controller, cookie, CSRF exception, CORS change, or server
  authorization change was introduced.

Targeted verification:

```powershell
npm --prefix .\web\PersonalOS.Web run test -- --watch=false --include src/app/core/appearance/theme.service.spec.ts --include src/app/features/settings/settings.component.spec.ts
```

## 0.1 Milestone 2

Objective:

```text
Angular Settings -> ASP.NET Core profile API -> UserPreferences -> SQL Server
IClock + persisted IANA time zone -> Today local date in English
```

Observable result:

1. sign in with an existing Milestone 1 account;
2. open Settings;
3. see the current display name and a read-only email;
4. see the saved time zone and the browser suggestion;
5. apply the browser suggestion or type an IANA identifier;
6. save through `PUT /api/profile`;
7. see accessible success feedback;
8. see the header and Today greeting update immediately;
9. reload and keep the saved values;
10. see Today render the correct local date for the saved time zone, in English;
11. receive safe validation feedback for an invalid time zone;
12. be unable to read or modify another account's profile.

### Milestone 2 scope

Backend:

- `UserPreferences` domain entity;
- `DisplayNameRules` domain rules;
- `IClock` application abstraction and `SystemClock` implementation;
- `TimeZoneCatalog` IANA validation;
- `LocalTimeService` and `TimeContextService`;
- `UserProfileService` and the `IUserProfileStore` port;
- EF Core `UserPreferencesConfiguration` and the `AddUserPreferences` migration;
- `GET /api/profile`, `PUT /api/profile`, `GET /api/time/context`;
- default preferences created during registration;
- a rate-limit policy for profile updates;
- unit tests with a fixed clock and integration tests with a controllable host clock.

Angular:

- profile models, API service, and profile service;
- browser time-zone detection through an injection token;
- explicit English date formatting;
- Settings account and time-zone sections with a typed reactive form;
- `Use browser time zone` action;
- unsaved-changes route guard;
- Today local date, loading state, error state, and UTC review prompt;
- in-memory display-name update through the existing auth store.

### Outside Milestone 2

Everything listed in section 3, plus email change, email confirmation, a localization selector,
themes, and any second time-related preference such as week start or a 12/24-hour setting.

### Milestone 2 API

| Method | Route | Authentication | Antiforgery | Rate limit |
|---|---|---:|---:|---:|
| GET | `/api/profile` | Yes | No | No |
| PUT | `/api/profile` | Yes | Yes | `profile` |
| PUT | `/api/profile/calendar-display` | Yes | Yes | `profile` |
| GET | `/api/time/context` | Yes | No | No |

Profile response:

```json
{
  "displayName": "Jefferson",
  "email": "user@example.com",
  "timeZoneId": "America/Costa_Rica",
  "updatedAtUtc": "2026-07-30T19:00:00+00:00"
}
```

Profile update request:

```json
{
  "displayName": "Jefferson",
  "timeZoneId": "America/Costa_Rica"
}
```

Time-context response:

```json
{
  "utcNow": "2026-07-30T19:24:00+00:00",
  "localNow": "2026-07-30T13:24:00-06:00",
  "localDate": "2026-07-30",
  "timeZoneId": "America/Costa_Rica",
  "utcOffsetMinutes": -360
}
```

Rules:

- both endpoints require authentication and return sanitized Problem Details;
- both responses use `Cache-Control: no-store`;
- the account is derived from the authentication cookie, never from request data;
- the update contract carries no account identifier and no email;
- validation errors use the camel-case field names `displayName` and `timeZoneId`;
- time-context values are machine-readable and are never localized by the server.

### Milestone 2 migration

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'

dotnet tool run dotnet-ef migrations script InitialIdentity AddUserPreferences `
  --project .\src\PersonalOS.Infrastructure\PersonalOS.Infrastructure.csproj `
  --startup-project .\src\PersonalOS.Api\PersonalOS.Api.csproj

dotnet tool run dotnet-ef database update `
  --project .\src\PersonalOS.Infrastructure\PersonalOS.Infrastructure.csproj `
  --startup-project .\src\PersonalOS.Api\PersonalOS.Api.csproj
```

`AddUserPreferences` creates the `UserPreferences` table and backfills every existing account with
`UTC`. It makes no change to any Identity table.

### Manual Milestone 2 verification

1. apply the migration;
2. start the API and Angular;
3. sign in with an existing Milestone 1 account;
4. open Settings and confirm the display name, read-only email, and saved time zone;
5. confirm the browser suggestion appears and is not saved automatically;
6. select `America/Costa_Rica` and change the display name;
7. save and confirm accessible success feedback;
8. confirm the header and the Today greeting update without a reload;
9. reload and confirm the values persisted;
10. confirm Today shows the correct Costa Rica local date in English;
11. submit an invalid time zone and confirm safe field-level feedback;
12. change a field and navigate away to confirm the unsaved-changes prompt;
13. send `PUT /api/profile` without `X-XSRF-TOKEN` and confirm rejection;
14. sign in with a second account and confirm independent settings;
15. inspect `localStorage`, `sessionStorage`, and IndexedDB and confirm no profile or token data;
16. confirm the browser console has no unexpected errors.

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

The implementation uses Angular framework packages at 22.1.0, Angular CLI 22.1.3, standalone
components, functional guards, a functional HTTP interceptor, typed reactive forms, signals for
in-memory authentication state, and Vitest through Angular's unit-test builder.

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
- `/app/settings`, which additionally uses a `CanDeactivate` unsaved-changes guard.

Anonymous users are redirected to login.

### Today page

The page:

- greets the authenticated user through the in-memory current-user signal;
- requests `GET /api/time/context` and shows a loading state while it is pending;
- shows the local date derived from the persisted time zone, rendered with an explicit `en-US`
  locale so it stays English on a Spanish machine;
- shows the saved time zone and the offset in effect;
- shows a safe error state with a retry action;
- shows a calm, non-blocking prompt when the account still uses UTC while the browser reports a
  different zone, and never changes the preference by itself;
- explains the PersonalOS cycle and presents a useful empty state;
- does not invent tasks, habits, nutrition, trends, or productivity scores.

### Settings page

The page:

- loads the profile from `GET /api/profile` with loading and error states;
- edits the display name through a typed reactive form;
- shows the email as read-only and explains why changing it is not available yet;
- shows the saved time zone, the browser suggestion, and a `Use browser time zone` action;
- offers an accessible time-zone input backed by a `datalist` of supported identifiers;
- disables saving while the form is unchanged, invalid, or already saving;
- maps server validation messages to `displayName` and `timeZoneId`;
- handles rate-limit and conflict responses safely;
- announces success through an accessible status region and makes the saved data the new
  baseline;
- updates the in-memory current user so the header and Today greeting react through signals;
- confirms before discarding unsaved edits, on navigation and on sign-out;
- offers Light, Dark, and System appearance choices that apply immediately and persist as the
  non-sensitive `personalos.themePreference` browser key;
- states `Server-managed cookie session` and shows no cookie values, tokens, claims, security
  stamps, or internal account data.

### Unsaved-changes decision

The unsaved-changes behaviour uses a functional `CanDeactivate` guard and a native
`window.confirm`. A native confirmation is accessible, needs no modal framework, and is
straightforward to test. The guard is skipped once the session has ended, so signing out never
asks the user to decide about edits that can no longer be saved. Settings asks before signing out
and then suppresses the guard, so the user is never prompted twice.

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

Milestone 2 unit coverage:

1. a fixed clock reports the configured instant and does not advance;
2. UTC conversion;
3. `America/Costa_Rica` conversion and its stable offset;
4. a daylight-saving zone in both directions, including the southern hemisphere;
5. local calendar date on both sides of a UTC day boundary;
6. unusable time-zone identifiers fall back to UTC for display;
7. IANA identifiers are accepted and Windows identifiers are rejected;
8. display-name trimming, whitespace-only rejection, and length limits;
9. profile update validation, field names, and clock usage;
10. profile update never changes the email;
11. time context uses the persisted time zone and isolates accounts.

Milestone 2 integration coverage:

1. profile read and time context require authentication;
2. a new account reads default `UTC` preferences;
3. a valid update returns and persists the new values, including across a new session;
4. update without an antiforgery token is rejected;
5. update with an invalid antiforgery token is rejected;
6. invalid time zones are rejected with a `timeZoneId` field error and no host internals;
7. whitespace-only and too-short display names are rejected with a `displayName` field error;
8. the display name is trimmed before saving;
9. over-posted `email`, `userId`, and `passwordHash` are ignored and sign-in still works;
10. `/api/auth/me` reflects the updated display name;
11. profile responses exclude sensitive Identity fields;
12. profile and time responses use `no-store`;
13. two accounts keep independent profiles;
14. one account cannot select another through body, query, or route data;
15. an account without a preferences record reads `UTC` without a write, and a save creates it.

The integration host runs on a controllable clock, so no time assertion depends on when the suite
executes.

SQLite in-memory may be used for fast integration tests, but it does not replace SQL Server migration testing. The `AddUserPreferences` migration was reviewed as generated SQL and applied to Development LocalDB against a database that already contained a Milestone 1 account.

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

Milestone 2 behaviour coverage:

1. Settings loads the authenticated profile into the form;
2. the email field is read-only and explains why;
3. display-name validation, including whitespace-only rejection;
4. an invalid time zone is represented safely;
5. the browser time-zone suggestion is detected and displayed;
6. the suggestion is never saved automatically;
7. `Use browser time zone` fills the form without saving;
8. saving is disabled while the form matches the saved values;
9. a whitespace-only edit counts as no change;
10. duplicate submissions are prevented while a save is in flight;
11. a successful save updates the Settings page and the form baseline;
12. a successful save updates the header display name;
13. a successful save updates the Today greeting;
14. server validation maps to the correct field;
15. server messages render as text, not markup;
16. rate-limit and conflict responses are displayed safely;
17. the update uses the existing antiforgery flow;
18. no profile data reaches `localStorage` or `sessionStorage`;
19. Today renders an English date, including months and weekdays;
20. Today uses the API time context rather than the browser's current date;
21. a UTC account sees the time-zone review prompt and nothing is saved;
22. the unsaved-changes state resets after a successful save;
23. the unsaved-changes guard confirms, blocks, and is skipped after sign-out;
24. loading and error states expose accessible roles and a retry action.

Post-Milestone 3 appearance coverage:

1. System is the default and writes nothing to browser storage until the user chooses a theme;
2. an explicit Light or Dark choice persists only `personalos.themePreference`;
3. a saved preference sets `data-theme`, `data-theme-preference`, and `color-scheme`;
4. System responds to operating-system theme changes while active;
5. a fixed theme ignores later operating-system theme changes.

Browser time-zone detection is provided through an injection token, so tests supply a fixed value
instead of depending on the machine's own time zone.

Use Angular's configured test infrastructure and HttpClient testing utilities. Avoid tests that only assert that a component exists. Note that `vi.mock` is not supported for relative imports under the Angular unit-test builder; use `TestBed` providers instead.

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

Milestone 2 must answer:

- Why is UTC the internal source of truth?
- Why is a time zone different from a UTC offset?
- Why does `IClock` make time-dependent code testable?
- Why is the browser time zone only a suggestion?
- Why does the server validate the time zone even though Angular offers a picker?
- How is the local calendar date calculated?
- Why does Angular state the `en-US` locale explicitly?
- How do signals update the header and the Today greeting after a save?
- Why is the profile server state rather than browser state?
- Why does a route guard not protect another user's profile?
- How does antiforgery protect `PUT /api/profile`?
- Why is email editing deferred?

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
