# PersonalOS — Plan de implementación

**Versión:** 1.0  
**Estado:** Proposed  
**Última actualización:** 2026-07-23

## Estrategia

Construir vertical slices demostrables. Cada milestone termina con comportamiento visible, pruebas, documentación, demo y working tree limpio.

No abrir varias áreas funcionales simultáneamente.

## M0 — Foundation documental

Entregables:

- PRD;
- TRD;
- UI/UX;
- App Flow;
- Backend Schema;
- Architecture;
- ADRs;
- Threat Model;
- Test Strategy;
- Standards;
- AGENTS;
- repositorio y scaffold.

Salida:

- baseline revisada;
- primer commit;
- rama de walking skeleton.

## M1 — Walking skeleton de autenticación

Objetivo:

```text
React -> API -> Identity -> EF Core -> SQL Server
```

Alcance:

- AppUser;
- DbContext;
- migración;
- register;
- login;
- me;
- logout;
- cookie;
- antiforgery;
- route guard;
- dashboard provisional;
- health;
- tests;
- CI;
- README;
- ADRs.

No alcance:

- módulos de negocio;
- email confirmation;
- recovery;
- MFA;
- SaaS;
- UI final.

Criterios:

- builds limpios;
- tests verdes;
- audits limpios;
- migración aplicable;
- flujo manual;
- docs actualizadas.

## M2 — Perfil y tiempo

- zona horaria;
- fecha local;
- preferencias;
- `ICurrentUser`;
- `IClock`;
- pruebas temporales.

Toda feature diaria depende de esto.

## M3 — Planificación

- tareas;
- prioridades;
- fechas;
- estados;
- Hoy mínimo;
- quick capture;
- reprogramación;
- tests.

Demo: crear, ver, completar e historial.

## M4 — Hábitos

- definición;
- frecuencia;
- expectativa;
- check/cantidad/duración;
- descanso;
- motivo;
- consistencia;
- recuperación.

Invariantes:

- registro único por fecha;
- descanso no es fallo;
- edición histórica controlada.

## M5 — Nutrición

- objetivos versionados;
- alimentos;
- comidas;
- porciones;
- calorías/macros;
- resumen.

Gate:

- no consejo médico;
- no déficit extremo;
- lenguaje neutral.

## M6 — Diario y cierre

- entrada libre;
- cierre;
- autosave;
- privacidad;
- conflicto;
- exportación inicial.

Gate: Threat Model revisado.

## M7 — Revisión e insights

- agregados;
- comparaciones;
- reglas explicables;
- experimento semanal;
- dashboard.

Sin IA generativa inicialmente.

## M8 — PWA y recordatorios

- manifest;
- service worker;
- offline controlado;
- push;
- scheduler;
- retries;
- delivery log;
- quiet hours.

Gate: idempotencia, tiempo y recovery.

## M9 — Hardening

- email confirmation;
- recovery;
- MFA/passkeys;
- CSP;
- security headers;
- rate limits;
- backup/restore;
- staging;
- observability;
- E2E;
- SQL migration tests;
- performance baseline.

## M10 — Portafolio

- demo;
- architecture actualizada;
- ADRs;
- métricas;
- caso de estudio;
- README público;
- CV;
- preparación de entrevista.

## Backlog

- Google Calendar;
- Telegram/email;
- wearables;
- IA;
- SaaS;
- Workspace;
- vault independiente.

## Flujo por feature

1. Feature spec.
2. Reglas.
3. App Flow.
4. Threat check.
5. API.
6. Datos.
7. UI states.
8. Tests.
9. Implementación.
10. Auditoría.
11. Demo.
12. Docs.
13. PR.
14. Retrospectiva.

## Stop conditions

Detener si:

- se contradice arquitectura;
- aparece multi-tenancy anticipado;
- hay secretos;
- migración peligrosa;
- tests dependen del orden;
- se omite seguridad;
- crece alcance;
- docs y código divergen;
- hay vulnerabilidad High/Critical.
