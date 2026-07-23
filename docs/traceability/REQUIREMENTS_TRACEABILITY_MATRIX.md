# PersonalOS — Matriz de trazabilidad

**Versión:** 1.0  
**Estado:** Draft

| ID | Requisito | Fuente | Milestone | Evidencia | Estado |
|---|---|---|---|---|---|
| AUTH-001 | registrar | PRD US-AUTH-001 | M1 | integration + frontend | Planned |
| AUTH-002 | login rememberMe | PRD US-AUTH-002 | M1 | integration + manual | Planned |
| AUTH-003 | cookie HttpOnly | TRD 8 | M1 | header test | Planned |
| AUTH-004 | antiforgery | TRD 9 | M1 | negative/positive tests | Planned |
| AUTH-005 | current user | PRD US-AUTH-003 | M1 | `/api/auth/me` | Planned |
| AUTH-006 | logout invalida | PRD US-AUTH-004 | M1 | integration | Planned |
| OPS-001 | live | TRD 18 | M1 | integration | Planned |
| OPS-002 | ready | TRD 18 | M1 | integration | Planned |
| CI-001 | backend CI | TRD 15 | M1 | GitHub Actions | Planned |
| CI-002 | frontend CI | TRD 15 | M1 | GitHub Actions | Planned |
| TIME-001 | zone | PRD identity | M2 | unit/integration | Planned |
| PLAN-001 | crear tarea | PRD planning | M3 | E2E | Planned |
| PLAN-002 | completar | PRD planning | M3 | domain/integration | Planned |
| HAB-001 | definir hábito | PRD habits | M4 | E2E | Planned |
| HAB-002 | unique por fecha | Backend Schema | M4 | DB/unit | Planned |
| NUT-001 | target versionado | PRD nutrition | M5 | migration/unit | Planned |
| JRN-001 | cierre | PRD journal | M6 | E2E | Planned |
| REV-001 | review semanal | PRD review | M7 | integration/E2E | Planned |
| PWA-001 | install | PRD MVP | M8 | Lighthouse/manual | Planned |
| NOT-001 | reminder idempotente | TRD | M8 | integration | Planned |

## Regla

Cada feature recibe ID, fuente, milestone, evidencia y estado. No se marca Done sin prueba verificable.
