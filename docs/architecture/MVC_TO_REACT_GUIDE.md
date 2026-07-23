# PersonalOS — Transición de ASP.NET Core MVC a React

**Versión:** 1.0  
**Estado:** Active

## 1. Cambio mental

MVC:

```text
Request -> Controller -> ViewModel -> Razor -> HTML
```

PersonalOS:

```text
Browser -> React -> HTTP JSON -> API -> Application -> Data
```

ASP.NET Core deja de renderizar la interfaz principal. Entrega datos y seguridad. React mantiene la UI en el navegador.

## 2. Mapa

| MVC | React/API | Diferencia |
|---|---|---|
| Razor View | Component | se ejecuta en cliente |
| Partial | Component | composición |
| `_Layout` | App Shell/Layout | sin reload completo |
| MVC Controller | API endpoint | JSON, no HTML |
| ViewModel | DTO + TS type | contrato entre procesos |
| Model binding | JSON deserialize | request tipado |
| ModelState | server validation + Zod | ambos lados |
| TempData | cache/toast/navigation state | no postback |
| ViewBag | props/context/query | evitar implícito |
| Tag Helpers | JSX/components | declarativo |
| Session auth | cookie + `/me` | React pregunta identidad |
| RedirectToAction | client navigation | navegación cliente |
| Form POST | handler + mutation | no reload |
| EF entity in view | DTO | no exponer persistencia |
| server lifecycle | render lifecycle | puede repetirse |
| DI | sigue en backend | React compone/hooks |
| Filters | middleware/filters | siguen en API |
| antiforgery form | token + header | SPA |

## 3. Componentes

Una función que describe UI según props y state.

```tsx
type Props = { displayName: string };

export function Greeting({ displayName }: Props) {
  return <h1>Hola, {displayName}</h1>;
}
```

Sin efectos durante render.

## 4. Props

Entradas explícitas e inmutables. Más cercanas a parámetros de partial tipada que a `ViewBag`.

## 5. State local

Para:

- modal;
- filtro visual;
- pestaña;
- valor temporal.

No duplicar server state completo.

## 6. Server state

TanStack Query maneja:

- loading;
- cache;
- stale;
- retry;
- invalidation;
- errors;
- dedup.

## 7. Render

Puede ocurrir muchas veces. Debe ser puro, rápido, sin escrituras ni peticiones manuales en el cuerpo.

## 8. Effects

`useEffect` sincroniza con sistemas externos. No usar por defecto para derivar datos, clicks o duplicar props.

Pregunta:

> ¿Qué sistema externo sincronizo?

## 9. Forms

React Hook Form administra interacción. Zod valida cliente. Servidor valida de nuevo.

```text
Usuario -> validación local -> request -> validación server -> resultado
```

## 10. Mutations

POST/PUT/PATCH/DELETE.

Después:

- actualizar cache solo si es seguro; o
- invalidar query;
- estado visible;
- conflicto;
- evitar doble submit.

## 11. Routes

React Router elige componentes sin nueva página.

ASP.NET Core conserva autorización real. `ProtectedRoute` no sustituye `[Authorize]`.

## 12. Auth

React no lee cookie HttpOnly.

1. API la emite.
2. browser la guarda.
3. fetch la envía.
4. React consulta `/me`.
5. API responde usuario o 401.

## 13. Antiforgery

1. React solicita token.
2. lo conserva en memoria.
3. lo envía en header mutables.
4. API valida token + cookie.

## 14. DTOs

C#:

```csharp
public sealed record CurrentUserResponse(
    Guid Id,
    string DisplayName,
    string Email);
```

TypeScript:

```ts
export type CurrentUser = {
  id: string;
  displayName: string;
  email: string;
};
```

Contratos equivalentes, no entidad compartida.

## 15. Errores

React transforma ProblemDetails en:

- errores de campo;
- error general;
- login;
- retry;
- pantalla segura.

## 16. Lo que no cambia

- HTTP;
- DI;
- middleware;
- autorización;
- server validation;
- EF Core;
- migraciones;
- SQL;
- logging;
- tests;
- separación.

## 17. Ruta de aprendizaje

1. Layout/componentes.
2. Props.
3. Forms.
4. State.
5. Routing.
6. Fetch.
7. Query.
8. Mutations.
9. Auth.
10. Hooks.
11. Testing.
12. PWA.

## 18. Preguntas de revisión

- ¿Quién posee el dato?
- ¿Local o remoto?
- ¿Qué provoca render?
- ¿Por qué existe cada effect?
- ¿Qué pasa si llega tarde?
- ¿Qué pasa si se duplica?
- ¿Qué ve mientras carga?
- ¿Funciona con teclado?
- ¿Qué valida servidor?
- ¿Qué prueba lo demuestra?
