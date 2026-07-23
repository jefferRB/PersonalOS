# ADR-0004 — SQL Server y EF Core

- **Estado:** Accepted
- **Fecha:** 2026-07-23

## Contexto

El aprendizaje nuevo es React. Ya existe experiencia con SQL Server y EF Core.

## Decisión

- SQL Server.
- EF Core.
- migrations en Infrastructure.
- LocalDB local.
- SQL Server real para migración/producción.

## Alternativas

- PostgreSQL;
- SQLite;
- NoSQL;
- Dapper primario.

## Razón

Reduce variables y concentra esfuerzo en React.

## Consecuencias

- SQLite solo acelera tests iniciales;
- migraciones requieren SQL Server real;
- no `EnsureCreated`;
- no repositorio genérico.
