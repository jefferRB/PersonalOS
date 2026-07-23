# PersonalOS — Estándares de ingeniería

**Versión:** 1.0  
**Estado:** Active  
**Última actualización:** 2026-07-23

## Principios

- claridad;
- cambios pequeños;
- seguridad por defecto;
- evidencia;
- reversibilidad;
- documentación;
- no sobrearquitectura.

## Branches

- `main`;
- `feat/<nombre>`;
- `fix/<nombre>`;
- `docs/<nombre>`;
- `chore/<nombre>`.

No desarrollar directamente en `main`.

## Commits

```text
feat: add cookie authentication
fix: reject missing antiforgery token
test: cover user lockout
docs: record identity decision
chore: update dependency
```

Cada commit representa una intención.

## Pull requests

Incluir:

- problema;
- alcance;
- no alcance;
- decisiones;
- screenshots;
- tests;
- comandos;
- migrations;
- riesgos;
- checklist;
- docs.

## C#

- nullable;
- async correcto;
- CancellationToken;
- reloj abstraído cuando importa;
- records para DTO;
- no excepciones para flujo esperado;
- no `catch (Exception)` vacío;
- no service locator;
- DI explícita;
- nombres de dominio;
- evitar `Manager` genérico.

## EF Core

- configurations separadas;
- projection;
- no entity en API;
- no lazy loading;
- migrations revisadas;
- índices por query;
- constraints;
- no repository genérico;
- transacción con propósito.

## API

- rutas consistentes;
- JSON;
- ProblemDetails;
- códigos correctos;
- authorization server-side;
- rate limit;
- no información interna.

## React

- TypeScript strict;
- componentes por responsabilidad;
- props explícitas;
- no effects en render;
- TanStack Query para remoto;
- React Hook Form;
- Zod;
- no duplicar query data;
- no `any` sin razón;
- no `dangerouslySetInnerHTML`;
- accesibilidad.

## CSS

- tokens;
- mobile-first;
- no inline repetido;
- focus;
- contrast;
- clases por intención;
- no solo color.

## Paquetes

Antes de agregar:

- problema;
- alternativas;
- mantenimiento;
- licencia;
- vulnerabilities;
- tamaño;
- versión;
- necesidad.

No agregar una librería para una función trivial.

## Seguridad

- secrets fuera de Git;
- input no confiable;
- least privilege;
- logs seguros;
- audits;
- negative auth tests;
- CSRF;
- XSS;
- ownership.

## Documentación

- ADR para decisiones;
- feature spec;
- README;
- diagrams as code;
- docs con PR;
- marcar superseded.

## IA

Todo código generado:

- se revisa;
- se entiende;
- se prueba;
- se compara con scope;
- se escanea;
- se documenta.

La responsabilidad final es humana.
