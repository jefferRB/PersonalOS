# PersonalOS — Especificación UI/UX

**Versión:** 1.0  
**Estado:** Draft  
**Última actualización:** 2026-07-23

## 1. Objetivo

Debe sentirse como un centro de mando personal tranquilo, rápido y explicable. No como un ERP, una lista de métricas, un sistema punitivo o un formulario administrativo permanente.

## 2. Principios

1. Una siguiente acción clara.
2. Captura en segundos.
3. Progressive disclosure.
4. Datos con contexto.
5. Lenguaje neutral.
6. Mobile-first para captura.
7. Desktop para análisis.
8. Accesibilidad.
9. Estados explícitos.
10. Privacidad visible.
11. Consistencia.

## 3. Navegación futura

- Hoy
- Planificar
- Hábitos
- Nutrición
- Diario
- Revisión
- Configuración

## 4. Pantalla “Hoy”

Orden:

1. saludo y fecha local;
2. siguiente compromiso;
3. tres prioridades;
4. captura rápida;
5. hábitos;
6. progreso nutricional;
7. tareas;
8. recomendación;
9. cierre diario.

No iniciar con gráficas históricas.

## 5. Autenticación inicial

### Registro

- nombre;
- correo;
- contraseña;
- requisitos visibles;
- errores junto al campo;
- estado de envío;
- enlace a login.

### Login

- correo;
- contraseña;
- recordarme;
- error seguro;
- recuperación futura claramente marcada.

### Dashboard provisional

Demuestra sesión y navegación, no diseño final.

## 6. Captura rápida futura

Tipos:

- tarea;
- evento;
- comida;
- nota;
- hábito;
- idea.

La primera versión puede limitarse a tarea.

## 7. Estados

Todo dato remoto:

- idle;
- loading;
- success;
- empty;
- validation error;
- authorization error;
- conflict;
- rate limit;
- server error;
- offline;
- stale.

No mostrar excepciones técnicas.

## 8. Formularios

- label visible;
- ayuda solo si aporta;
- error con `aria-describedby`;
- foco en primer error;
- conservar datos;
- evitar doble submit;
- teclado;
- confirmación destructiva;
- autosave con estado visible.

## 9. Accesibilidad

- teclado;
- orden de foco;
- foco visible;
- contraste;
- texto escalable;
- iconos accesibles;
- no solo color;
- headings;
- landmarks;
- mensajes dinámicos;
- resumen textual para gráficas.

## 10. Responsive

### Móvil

- navegación compacta;
- acciones al alcance;
- una columna;
- captura prominente;
- gráficas simplificadas.

### Escritorio

- sidebar;
- paneles;
- revisión comparativa;
- atajos;
- tablas cuando sean mejores.

## 11. Sistema visual

Walking skeleton sin branding final.

Tokens mínimos:

- spacing;
- radius;
- typography;
- surface;
- foreground;
- border;
- primary;
- success;
- warning;
- danger;
- focus.

## 12. Gamificación

Permitido:

- consistencia;
- recuperación;
- progreso;
- nivel;
- récords;
- experimentos.

Prohibido:

- humillación;
- perder todo por un fallo;
- premiar hábitos triviales masivos;
- competir por comer menos;
- dark patterns.

## 13. Privacidad

- ocultar diario en previews;
- push sin contenido sensible;
- reautenticación futura;
- consentimiento para insights sensibles;
- excluir entradas.

## 14. Aceptación del walking skeleton

- teclado;
- loading;
- errores seguros;
- sin flash de contenido privado;
- logout accesible;
- responsive básico;
- HTML semántico;
- no solo color.
