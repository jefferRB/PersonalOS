# PersonalOS — Implementación, calidad y operación

**Versión:** 1.0  
**Estado:** Baseline

## 1. Milestone 1

Objetivo:

```text
React -> API -> Identity -> EF Core -> SQL Server
```

Resultado observable:

1. abrir aplicación;
2. registrarse;
3. iniciar sesión;
4. ver dashboard protegido;
5. recargar y conservar sesión;
6. consultar usuario actual;
7. cerrar sesión;
8. quedar fuera de la ruta protegida.

Decisión de registro M1:

- `POST /api/auth/register` crea la cuenta y no inicia sesión automáticamente;
- React muestra confirmación en la pantalla de login;
- el usuario debe iniciar sesión explícitamente.

## 2. Alcance M1

### Backend

- AppUser;
- IdentityRole;
- ApplicationDbContext;
- SQL Server;
- migración inicial;
- Identity;
- cookies;
- antiforgery;
- rate limit;
- ProblemDetails;
- health;
- OpenAPI en Development;
- integration tests.

### Frontend

- React Router;
- TanStack Query;
- React Hook Form;
- Zod;
- fetch centralizado;
- ProblemDetails;
- antiforgery;
- RegisterPage;
- LoginPage;
- ProtectedRoute;
- AuthenticatedLayout;
- DashboardPage;
- logout;
- tests;
- lint.

### Delivery

- CI;
- documentación;
- auditorías;
- prueba manual.

## 3. No alcance M1

- tareas;
- hábitos;
- nutrición;
- diario;
- recordatorios;
- PWA;
- confirmación de correo;
- recuperación;
- MFA;
- external login;
- Workspace;
- multi-tenancy;
- billing;
- UI final;
- producción.

## 4. API M1

| Method | Route | Auth | Antiforgery |
|---|---|---:|---:|
| GET | `/api/antiforgery/token` | No | N/A |
| POST | `/api/auth/register` | No | Sí |
| POST | `/api/auth/login` | No | Sí |
| GET | `/api/auth/me` | Sí | No |
| POST | `/api/auth/logout` | Sí | Sí |
| GET | `/health/live` | No | No |
| GET | `/health/ready` | No | No |

Register:

```json
{
  "displayName": "Jefferson",
  "email": "user@example.com",
  "password": "secret"
}
```

Login:

```json
{
  "email": "user@example.com",
  "password": "secret",
  "rememberMe": true
}
```

Current user:

```json
{
  "id": "guid",
  "displayName": "Jefferson",
  "email": "user@example.com"
}
```

Errores:

- `application/problem+json`;
- sin stack trace;
- sin SQL;
- sin nombres internos;
- sin secretos.

## 5. React M1

Estructura objetivo:

```text
src/
├── app/
├── components/
├── features/
│   └── auth/
├── lib/
├── pages/
└── styles/
```

Startup:

```text
GET /api/auth/me
-> loading
-> authenticated dashboard
   o
-> anonymous login
```

Reglas:

- `/me` es server state;
- no guardar usuario o token como fuente de verdad en localStorage;
- `credentials: include`;
- evitar flash de contenido privado;
- invalidar auth query tras login;
- limpiar cache privada tras logout;
- Zod valida cliente;
- servidor vuelve a validar.

Dashboard provisional:

```text
PersonalOS está funcionando
```

Debe mostrar nombre y logout.

## 6. Backend tests

Mínimos:

1. registro válido;
2. duplicado;
3. validación inválida;
4. login válido;
5. login inválido;
6. lockout;
7. `/me` sin sesión;
8. `/me` con sesión;
9. logout;
10. POST sin antiforgery;
11. POST con antiforgery;
12. live;
13. ready;
14. 401 sin redirect HTML;
15. cookie configurada.

SQLite in-memory puede usarse al inicio para integración, pero no sustituye pruebas de migración en SQL Server.

Implementación actual:

- `WebApplicationFactory` reemplaza SQL Server por SQLite in-memory;
- la conexión SQLite permanece abierta durante cada factory;
- SQLite crea el esquema efímero con `EnsureCreated()` porque la migración SQL Server contiene tipos no portables como `nvarchar(max)`;
- esto no valida migraciones SQL Server; la migración real se verifica contra LocalDB Development.

## 7. Frontend tests

Mínimos:

1. validación register;
2. validación login;
3. loading auth;
4. redirect anonymous;
5. dashboard con usuario;
6. ProblemDetails;
7. logout;
8. 401 como estado anónimo;
9. rate limit;
10. ausencia de auth token en storage.

## 8. Prueba manual M1

1. aplicar migración;
2. iniciar API;
3. iniciar React;
4. registrar cuenta;
5. revisar cookie HttpOnly;
6. recargar;
7. verificar `/me`;
8. cerrar sesión;
9. intentar dashboard;
10. probar login inválido;
11. probar POST sin antiforgery;
12. revisar health.

## 9. CI

Backend:

```text
restore
build --no-restore
test --no-build
NuGet audit
```

Frontend:

```text
npm ci
lint
test --run
build
npm audit
```

Comandos definitivos desde la raíz:

```powershell
dotnet restore .\PersonalOS.slnx
dotnet build .\PersonalOS.slnx --no-restore
dotnet test .\PersonalOS.slnx --no-build
dotnet package list --project .\PersonalOS.slnx --vulnerable --include-transitive

npm --prefix .\web\PersonalOS.Web ci
npm --prefix .\web\PersonalOS.Web run lint
npm --prefix .\web\PersonalOS.Web run test -- --run
npm --prefix .\web\PersonalOS.Web run build
npm --prefix .\web\PersonalOS.Web audit --audit-level=high
```

Resultado comprobado:

- `dotnet package list --project .\PersonalOS.slnx --vulnerable --include-transitive`
  se ejecuta sobre la solución completa y no reporta paquetes vulnerables en
  los orígenes actuales.

Gate:

- cero errores;
- cero tests fallidos;
- cero vulnerabilidades High/Critical;
- no secretos;
- documentación actualizada.

## 10. Definition of Done

Una feature queda terminada si:

### Producto

- problema definido;
- aceptación cumplida;
- no alcance respetado;
- estados alternos.

### Arquitectura

- dependencias correctas;
- no tecnología innecesaria;
- contratos coherentes.

### Backend

- server validation;
- authorization;
- CancellationToken;
- ProblemDetails;
- logs seguros;
- queries revisadas;
- migración revisada.

### Frontend

- loading;
- empty si aplica;
- error;
- success;
- responsive;
- teclado;
- labels;
- cache correcta.

### Seguridad

- ownership;
- antiforgery;
- XSS revisado;
- rate limit;
- no secretos;
- pruebas negativas;
- auditoría limpia.

### Pruebas

- unitarias, integración y frontend según riesgo;
- independencia del orden;
- casos de error.

### Entrega

- build;
- tests;
- lint;
- auditoría;
- diff revisado;
- documentación;
- no merge directo a main.

## 11. Trabajo con IA

Flujo:

```text
Contexto -> Auditoría -> Plan -> Implementación -> Diff -> Tests -> Auditoría -> Decisión humana
```

Reglas:

- un agente puede implementar;
- otro puede auditar;
- Jefferson ejecuta comandos críticos;
- no aceptar cambios masivos sin revisar;
- no aceptar criptografía o autorización sin auditoría;
- no hacer commit automático salvo petición.

## 12. Aprendizaje de React

Por milestone registrar:

- concepto;
- equivalencia con MVC;
- problema resuelto;
- qué provoca render;
- estado local frente a remoto;
- effects usados;
- error encontrado;
- prueba;
- explicación para entrevista.

M1 debe responder:

- ¿por qué `/me` es server state?;
- ¿por qué no usar localStorage?;
- ¿cómo se evita el flash privado?;
- ¿por qué invalidar query?;
- ¿qué ocurre al logout?;
- ¿qué valida Zod y qué valida el servidor?;
- ¿por qué no usar `useEffect` para todo?

## 13. Operación futura

Antes de producción:

- staging;
- TLS;
- health;
- logs estructurados;
- métricas;
- alertas;
- backups;
- restore probado;
- rollback;
- confirmación de correo;
- recuperación;
- MFA/passkeys;
- CSP;
- security headers;
- abuse monitoring.

Objetivos orientativos:

- API simple p95 < 500 ms;
- disponibilidad 99.5%;
- RPO 24 h;
- RTO 4 h.

No prometer hasta medir.

## 14. Explicación para entrevista

> PersonalOS es una plataforma personal construida con React y ASP.NET Core. Elegí un monolito modular para tener límites claros sin introducir microservicios prematuramente. El frontend consume una API JSON y la autenticación usa Identity, cookies HttpOnly y antiforgery. El proyecto incluye pruebas, CI, seguridad, documentación y una ruta explícita para evolucionar a SaaS solo cuando exista una necesidad real.

Decisiones que se deben poder defender:

- React en vez de Razor para este proyecto;
- monolito modular en vez de microservicios;
- cookies en vez de JWT en localStorage;
- SQL Server por reducción de incertidumbre;
- no multi-tenancy anticipado;
- pruebas por riesgo;
- documentación mínima y viva.
