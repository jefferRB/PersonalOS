# ADR-0001 — Adoptar un monolito modular

- **Estado:** Accepted
- **Fecha:** 2026-07-23

## Contexto

Producto inicial de un usuario, varios dominios futuros, sin escala organizacional que justifique distribución.

## Decisión

Monolito modular con Domain, Application, Infrastructure y Api; React separado.

## Alternativas

- MVC único: familiar, pero no cumple aprendizaje React y favorece mezcla.
- Microservicios: costo operativo sin necesidad.
- Monolito modular: límites con operación simple.

## Consecuencias

Positivas:

- despliegue simple;
- transacciones locales;
- bajo costo;
- límites;
- extracción futura posible.

Negativas:

- disciplina;
- falla de proceso afecta todo;
- no escala componentes por separado.

## Reglas

- sin referencias inversas;
- sin acceso a internals;
- microservicios requieren ADR;
- revisión por milestone.
