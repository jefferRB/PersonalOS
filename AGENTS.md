# PersonalOS — Instrucciones para agentes de código

## 1. Leer antes de modificar

1. `README.md`
2. `docs/01_PRODUCT.md`
3. `docs/02_ARCHITECTURE.md`
4. `docs/03_DELIVERY.md`
5. archivos afectados

No resolver contradicciones silenciosamente.

## 2. Arquitectura obligatoria

Proyectos:

- `PersonalOS.Domain`
- `PersonalOS.Application`
- `PersonalOS.Infrastructure`
- `PersonalOS.Api`
- `PersonalOS.Web`
- `PersonalOS.UnitTests`
- `PersonalOS.IntegrationTests`

Dependencias permitidas:

```text
Application -> Domain
Infrastructure -> Application + Domain
Api -> Application + Infrastructure
UnitTests -> Domain + Application
IntegrationTests -> Api + Infrastructure + Application
```

No agregar referencias inversas ni circulares.

## 3. Alcance actual

Primer incremento:

- EF Core y SQL Server;
- ASP.NET Core Identity;
- cookies HttpOnly;
- antiforgery;
- registro;
- login;
- usuario actual;
- logout;
- ruta React protegida;
- dashboard provisional;
- health checks;
- pruebas;
- CI.

No implementar todavía:

- tareas;
- hábitos;
- nutrición;
- diario;
- recordatorios;
- PWA;
- IA;
- vault;
- Workspace;
- multi-tenancy;
- billing;
- roles comerciales.

## 4. Prohibido por defecto

Sin aprobación explícita:

- microservicios;
- event sourcing;
- MediatR;
- AutoMapper;
- repositorio genérico;
- Unit of Work artificial;
- Redux;
- Zustand;
- JWT en localStorage/sessionStorage;
- CORS permisivo;
- criptografía propia;
- migraciones automáticas en producción;
- paquetes preview;
- reestructuraciones masivas fuera de alcance.

## 5. Seguridad

Nunca versionar, registrar ni mostrar:

- passwords;
- cookies;
- tokens;
- antiforgery tokens;
- connection strings con secretos;
- User Secrets;
- API keys;
- contenido sensible del diario;
- datos productivos reales.

No eliminar la referencia directa segura de `Microsoft.OpenApi` sin comprobar que la vulnerabilidad transitiva continúa resuelta.

## 6. Forma de trabajo

Antes de editar:

1. inspeccionar rama y repositorio;
2. describir estado;
3. identificar riesgos;
4. presentar plan;
5. listar archivos esperados.

Durante:

1. mantener cambios pequeños;
2. respetar alcance;
3. actualizar pruebas y documentación;
4. no hacer commits salvo petición;
5. no afirmar éxito sin ejecutar.

Después:

1. revisar `git diff`;
2. ejecutar validaciones;
3. reportar resultados exactos;
4. declarar lo no verificado;
5. listar riesgos pendientes.

## 7. Validación

Backend:

```powershell
dotnet restore .\PersonalOS.slnx
dotnet build .\PersonalOS.slnx --no-restore
dotnet test .\PersonalOS.slnx --no-build
dotnet package list --project .\PersonalOS.slnx --vulnerable --include-transitive
```

Frontend:

```powershell
npm --prefix .\web\PersonalOS.Web ci
npm --prefix .\web\PersonalOS.Web run lint
npm --prefix .\web\PersonalOS.Web run test -- --run
npm --prefix .\web\PersonalOS.Web run build
npm --prefix .\web\PersonalOS.Web audit --audit-level=high
```

Si un script aún no existe, indicarlo.

## 8. Finalización

Una tarea queda terminada solo si:

- cumple criterios;
- respeta arquitectura;
- incluye pruebas;
- no introduce secretos;
- actualiza documentación;
- pasa builds y auditorías;
- el diff fue revisado;
- no se fusionó directamente en `main`.
