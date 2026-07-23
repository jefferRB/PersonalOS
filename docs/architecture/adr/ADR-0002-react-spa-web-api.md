# ADR-0002 — React SPA y ASP.NET Core Web API

- **Estado:** Accepted
- **Fecha:** 2026-07-23

## Contexto

Se domina ASP.NET Core MVC y se quiere aprender React manteniendo .NET.

## Decisión

- React + TypeScript + Vite.
- ASP.NET Core Web API.
- JSON.
- mismo origen lógico.
- proyectos separados en monorepo.

## Alternativas

- Razor MVC;
- Blazor;
- plantilla combinada;
- full-stack JavaScript.

## Razón

React cumple aprendizaje y empleabilidad. .NET conserva experiencia en seguridad, datos y operación.

## Consecuencias

- contratos;
- estado cliente/remoto;
- antiforgery;
- pipeline frontend;
- sin postback clásico.
