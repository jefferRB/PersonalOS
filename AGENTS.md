# PersonalOS — Repository Instructions

## 1. Fuente de verdad

Antes de cambiar producto o arquitectura, lea:

1. `docs/PersonalOS_Dossier_v1.0.md`
2. `docs/README.md`
3. `docs/product/PRD.md`
4. `docs/technical/TRD.md`
5. ADRs relevantes en `docs/architecture/adr/`

Precedencia:

1. ADR aceptado para decisiones arquitectónicas.
2. PRD vigente para alcance.
3. TRD vigente para requisitos técnicos.
4. Especificación de la feature activa.
5. Dossier para visión de largo plazo.

No resuelva contradicciones silenciosamente.

## 2. Arquitectura

Backend:

- `PersonalOS.Domain`
- `PersonalOS.Application`
- `PersonalOS.Infrastructure`
- `PersonalOS.Api`

Dependencias permitidas:

- Application -> Domain
- Infrastructure -> Application + Domain
- Api -> Application + Infrastructure
- Domain -> ninguna capa externa
- UnitTests -> Domain + Application
- IntegrationTests -> Api + Infrastructure + Application

Frontend:

- React + TypeScript + Vite en `web/PersonalOS.Web`

No introduzca referencias inversas, dependencias circulares ni carpetas globales de utilidades sin ownership.

## 3. Alcance actual

Primer incremento:

- SQL Server y EF Core;
- ASP.NET Core Identity;
- cookie authentication;
- antiforgery;
- register, login, logout y current user;
- ruta React protegida;
- dashboard provisional;
- health checks;
- tests;
- CI.

No implementar todavía tareas, hábitos, nutrición, diario, recordatorios, IA, vault, Workspace o multi-tenancy.

## 4. Prohibido por defecto

Sin ADR o aprobación explícita:

- microservicios;
- event sourcing;
- MediatR;
- AutoMapper;
- repositorio genérico;
- Unit of Work artificial sobre EF Core;
- Redux o Zustand;
- JWT en localStorage/sessionStorage;
- CORS permisivo;
- criptografía propia;
- migraciones automáticas en producción;
- versiones preview o floating.

## 5. Seguridad

Nunca versionar, registrar ni pegar en prompts:

- passwords;
- cookies;
- antiforgery tokens;
- connection strings con secretos;
- API keys;
- User Secrets;
- contenido sensible del diario;
- plaintext de un vault futuro.

La autenticación web debe usar cookies HttpOnly.

No elimine la referencia directa segura de `Microsoft.OpenApi` sin demostrar que la dependencia transitiva continúa corregida.

## 6. Protocolo

Antes de editar:

1. inspeccionar repositorio y rama;
2. leer documentación;
3. describir estado;
4. identificar riesgos;
5. presentar plan;
6. listar archivos esperados.

Después de editar:

1. revisar `git diff`;
2. ejecutar validaciones;
3. reportar resultados exactos;
4. declarar lo no verificado;
5. indicar riesgos residuales;
6. no hacer commit salvo petición explícita.

## 7. Validación

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

Si un script aún no existe, indíquelo claramente.

## 8. Finalización

Una feature solo está completa cuando cumple:

- criterios de aceptación;
- `docs/governance/DEFINITION_OF_DONE.md`;
- seguridad;
- pruebas;
- documentación;
- auditorías;
- revisión humana del diff.

No fusionar directamente en `main`.
