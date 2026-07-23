# PersonalOS — Operación y observabilidad

**Versión:** 1.0  
**Estado:** Draft  
**Última actualización:** 2026-07-23

## Entornos

### Local

- LocalDB;
- User Secrets;
- Vite;
- OpenAPI/Scalar;
- logs detallados seguros.

### CI

- build reproducible;
- tests;
- audits;
- sin secretos production.

### Staging

- similar a production;
- datos sintéticos;
- migration rehearsal;
- smoke.

### Production

- secrets externos;
- TLS;
- backup;
- monitoring;
- mínimo acceso;
- interactive docs off.

## Health

- `/health/live`: proceso;
- `/health/ready`: dependencias.

Sin datos sensibles.

## Telemetría

### Logs

- estructurados;
- trace id;
- route;
- status;
- duration;
- event id.

### Métricas

- request rate;
- latency;
- error rate;
- auth failures;
- lockouts;
- DB latency;
- jobs;
- notification delivery;
- queue depth futura.

### Traces

- request;
- DB;
- external;
- worker.

## Señales de usuario

- login fail;
- save fail;
- reminder perdido;
- export fail;
- cierre no guardado.

## SLO inicial propuesto

- availability 99.5%;
- simple API p95 < 500 ms;
- critical jobs 99%;
- 5xx < 1%;
- RPO 24 h;
- RTO 4 h.

No prometer hasta medir.

## Alertas

Cada alerta necesita condición, impacto, owner, severidad, runbook y acción.

## Runbooks

- API down;
- ready fail;
- DB fail;
- migration fail;
- disk full;
- backup fail;
- restore;
- auth spike;
- worker stuck;
- secret compromised.

## Deploy

1. CI.
2. artifact.
3. backup.
4. migration review.
5. staging.
6. smoke.
7. approval.
8. production.
9. smoke.
10. monitor.
11. record version.

## Rollback

App rollback separado de DB destructive rollback.

Para incompatibilidad:

- expand/contract;
- version compatibility;
- backup;
- plan.

## Backups

- frequency;
- retention;
- encryption;
- separate location;
- least access;
- restore test;
- evidence.

> Un backup no probado es una hipótesis.

## Incident

1. detect;
2. classify;
3. contain;
4. communicate;
5. recover;
6. verify;
7. blameless postmortem;
8. owner actions.

## Privacidad operacional

- no datos sensibles en tickets;
- anonimizar;
- limitar access;
- eliminar dumps;
- no production DB en dev;
- no datos a IA.
