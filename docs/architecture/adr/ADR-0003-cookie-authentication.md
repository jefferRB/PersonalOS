# ADR-0003 — Identity con cookies same-origin

- **Estado:** Accepted
- **Fecha:** 2026-07-23

## Contexto

App web de navegador. Se necesita sesión sin exponer token al JavaScript.

## Decisión

- ASP.NET Core Identity.
- AppUser `Guid`.
- cookie HttpOnly.
- Secure en producción.
- SameSite=Lax.
- mismo origen.
- antiforgery.
- `/api/auth/me`.

## Alternativas

- JWT en localStorage;
- BFF externo;
- IdP administrado desde inicio;
- auth propia.

## Razón

Cookie HttpOnly reduce exposición a JS. Identity evita hashing, lockout y stamps propios.

## Consecuencias

- tratar CSRF;
- CORS no permisivo;
- React usa `/me`;
- recovery/MFA pendientes.
