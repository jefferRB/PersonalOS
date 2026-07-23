# PersonalOS — Definition of Done

Una feature está terminada solo cuando los ítems aplicables están completos.

## Producto

- [ ] problema y usuario definidos;
- [ ] aceptación cumplida;
- [ ] no alcance respetado;
- [ ] estados alternos;
- [ ] lenguaje revisado.

## Arquitectura

- [ ] dependency direction;
- [ ] no tecnología innecesaria;
- [ ] ADR;
- [ ] modelo/contratos;
- [ ] no duplicación arquitectónica.

## Backend

- [ ] server validation;
- [ ] authorization;
- [ ] CancellationToken;
- [ ] ProblemDetails;
- [ ] logs;
- [ ] queries;
- [ ] migration.

## Frontend

- [ ] loading;
- [ ] empty;
- [ ] error;
- [ ] success;
- [ ] responsive;
- [ ] keyboard;
- [ ] labels;
- [ ] no sensitive storage;
- [ ] cache correcta.

## Seguridad

- [ ] threat check;
- [ ] ownership;
- [ ] CSRF;
- [ ] XSS;
- [ ] rate limit;
- [ ] secrets;
- [ ] no High/Critical;
- [ ] negative tests.

## Pruebas

- [ ] unit;
- [ ] integration;
- [ ] frontend;
- [ ] E2E si crítico;
- [ ] order independent;
- [ ] errors;
- [ ] time/zone.

## Operación

- [ ] health/telemetry;
- [ ] config;
- [ ] rollback;
- [ ] backup/migration;
- [ ] alerts/runbook.

## Documentación

- [ ] PRD/TRD/feature spec;
- [ ] API;
- [ ] flow;
- [ ] ADR;
- [ ] README;
- [ ] traceability;
- [ ] portfolio evidence.

## Verificación

- [ ] dotnet build;
- [ ] dotnet test;
- [ ] lint;
- [ ] frontend tests;
- [ ] frontend build;
- [ ] NuGet audit;
- [ ] npm audit;
- [ ] diff reviewed;
- [ ] working tree esperado.

## Entrega

- [ ] PR comprensible;
- [ ] no commits accidentales;
- [ ] no merge sin review;
- [ ] limitaciones declaradas;
- [ ] demo.
