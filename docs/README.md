# PersonalOS — Índice y gobierno documental

**Estado:** Active  
**Última actualización:** 2026-07-23

## Propósito

Esta carpeta es la fuente de verdad de producto, arquitectura, seguridad, calidad y operación. Busca reducir improvisación, dar contexto a desarrolladores y agentes, y conservar evidencia profesional.

## Mapa

| Área | Documento | Pregunta |
|---|---|---|
| Visión | `PersonalOS_Dossier_v1.0.md` | ¿Cuál es la visión completa? |
| Producto | `product/PRD.md` | ¿Qué construimos y por qué? |
| Técnica | `technical/TRD.md` | ¿Qué debe garantizar técnicamente? |
| UX | `ux/UI_UX_SPEC.md` | ¿Cómo debe sentirse y comportarse? |
| Flujos | `ux/APP_FLOW.md` | ¿Cómo recorre el usuario el sistema? |
| Arquitectura | `architecture/SYSTEM_ARCHITECTURE.md` | ¿Cómo se divide el sistema? |
| Backend | `architecture/BACKEND_SCHEMA.md` | ¿Cómo se estructuran API y datos? |
| MVC a React | `architecture/MVC_TO_REACT_GUIDE.md` | ¿Cómo se traducen conceptos conocidos? |
| Decisiones | `architecture/adr/` | ¿Por qué se eligió cada decisión? |
| Seguridad | `security/THREAT_MODEL.md` | ¿Qué protegemos y de qué? |
| Pruebas | `testing/TEST_STRATEGY.md` | ¿Cómo demostramos que funciona? |
| Operación | `operations/OPERATIONS_AND_OBSERVABILITY.md` | ¿Cómo se opera y recupera? |
| Estándares | `governance/ENGINEERING_STANDARDS.md` | ¿Cómo trabajamos? |
| DoD | `governance/DEFINITION_OF_DONE.md` | ¿Cuándo está realmente terminado? |
| IA | `ai/AI_WORKING_AGREEMENT.md` | ¿Cómo usamos agentes con control? |
| Plan | `planning/IMPLEMENTATION_PLAN.md` | ¿En qué orden se construye? |
| Trazabilidad | `traceability/REQUIREMENTS_TRACEABILITY_MATRIX.md` | ¿Qué prueba cubre cada requisito? |
| Portafolio | `portfolio/PORTFOLIO_EVIDENCE_PLAN.md` | ¿Qué evidencia demuestra competencias? |

## Estados

- Draft
- Proposed
- Accepted
- Superseded
- Deprecated

## Precedencia

1. ADR aceptado para arquitectura.
2. PRD para alcance.
3. TRD para requisitos técnicos.
4. Feature spec activa.
5. Dossier para visión.

## Actualización obligatoria

Actualizar cuando cambie:

- alcance;
- modelo;
- API;
- datos;
- autenticación;
- dependencia significativa;
- despliegue;
- seguridad;
- pruebas;
- atributo de calidad.

## Revisión por incremento

Antes de programar:

- requisitos revisados;
- flujo identificado;
- feature spec;
- threat check;
- ADR cuando aplique.

Antes de fusionar:

- docs coherentes con código;
- pruebas enlazadas;
- trazabilidad;
- evidencia de comandos.

> La documentación no debe describir una arquitectura distinta de la que realmente compila y se despliega.
