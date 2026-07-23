# PersonalOS — Acuerdo de trabajo con IA

**Versión:** 1.0  
**Estado:** Active  
**Aplica a:** ChatGPT, Claude, Codex, Copilot y agentes futuros.

## Objetivo

Aumentar análisis, velocidad y cobertura sin delegar responsabilidad arquitectónica.

## Roles

### Jefferson

- product owner;
- arquitecto responsable;
- aprobador;
- valida comandos;
- decide merge.

### ChatGPT

- análisis;
- explicación;
- arquitectura;
- documentación;
- prompts;
- auditoría.

### Claude/Codex

- inspección;
- implementación acotada;
- tests;
- refactor;
- auditoría independiente.

## Flujo

```text
Contexto -> Auditoría -> Plan -> Implementación -> Diff -> Tests -> Auditoría -> Decisión
```

## Contexto mínimo

- objetivo;
- alcance;
- no alcance;
- arquitectura;
- archivos;
- criterios;
- tests;
- security;
- comandos;
- entrega.

## Prohibido

- secrets;
- datos sensibles reales;
- commits sin petición;
- cambios masivos sin diff;
- “arregla todo”;
- tests no ejecutados;
- IA como autoridad criptográfica;
- merge por confianza en resumen.

## Auditoría inicial

- estructura;
- packages;
- dependencies;
- behavior;
- risks;
- doc contradictions;
- archivos previstos.

## Implementación

- minimal changes;
- no style rewrite;
- no package update fuera de scope;
- no ceremonial abstraction;
- preservar tests;
- declarar assumptions.

## Verificación

Resultados exactos. El humano repite comandos críticos.

## Revisión cruzada

Para auth, pagos, migrations, privacidad, workers, encryption y authorization:

- un agente implementa;
- otro audita;
- Jefferson decide.

## Evidencia

Conservar:

- prompt/resumen;
- auditoría;
- plan;
- diff;
- commands;
- findings;
- accepted/rejected decisions.

## Aprendizaje

Cada entrega explica:

- concepto React;
- concepto .NET;
- por qué funciona;
- alternativas;
- riesgo;
- tests.

No aceptar código que no pueda explicarse razonablemente.

## Plantilla de prompt

```text
Rol:
Contexto:
Objetivo:
Alcance:
No alcance:
Arquitectura:
Reglas:
Seguridad:
Pruebas:
Comandos:
Criterios:
Entrega:
```
