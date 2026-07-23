# PersonalOS — Product Requirements Document

**Versión:** 1.0  
**Estado:** Proposed  
**Propietario:** Jefferson Rojas  
**Última actualización:** 2026-07-23

## 1. Resumen

PersonalOS es un sistema operativo personal para planificar, ejecutar, registrar, revisar y mejorar el día a día. Integra agenda, tareas, hábitos, nutrición, diario y análisis semanal.

No busca castigar ni acumular métricas. Debe responder:

- ¿Qué debo hacer ahora?
- ¿Qué es importante hoy?
- ¿Qué comportamiento repito?
- ¿Por qué fallo?
- ¿Qué ajuste concreto pruebo la próxima semana?

## 2. Problema

La información está fragmentada entre calendario, notas, hábitos, calorías y diario. Esto provoca:

- captura duplicada;
- falta de contexto;
- dashboards sin acción;
- metas desconectadas;
- dificultad para detectar patrones;
- abandono por fricción.

## 3. Usuario inicial

Jefferson:

- trabaja y estudia;
- desarrolla software;
- necesita controlar prioridades;
- busca mejorar hábitos y alimentación;
- quiere aprender React con un producto real;
- valora métricas y explicaciones.

No se asume SaaS desde el primer día.

## 4. Job to be done

> Cuando inicio, ejecuto o cierro mi día, quiero una vista unificada de compromisos, hábitos, alimentación y reflexiones para tomar una siguiente acción realista y mejorar semanalmente.

## 5. Visión

> Convertir datos diarios voluntariamente registrados en decisiones pequeñas, explicables y sostenibles.

## 6. Principios

1. Acción antes que decoración.
2. Captura rápida.
3. Historia honesta.
4. Explicabilidad.
5. Recuperación antes que culpa.
6. Privacidad por defecto.
7. Progresión gradual.
8. Control del usuario.
9. Accesibilidad.
10. Código comprensible.

## 7. Objetivos del MVP completo

- cuenta y sesión segura;
- centro “Hoy”;
- tareas;
- hábitos;
- calorías y objetivos históricos;
- diario y cierre;
- revisión semanal;
- tendencias básicas;
- exportación;
- PWA responsive.

## 8. No objetivos del MVP

- gestor de contraseñas;
- consejo médico;
- déficit extremo;
- red social;
- marketplace;
- multi-tenancy;
- pagos;
- app nativa;
- IA que modifique datos sin aprobación;
- microservicios.

## 9. Módulos

### Identidad

- registro;
- login/logout;
- usuario actual;
- zona horaria;
- preferencias.

### Hoy

- compromisos;
- tres prioridades;
- tareas;
- hábitos;
- calorías;
- captura rápida;
- recomendación.

### Planificación

- tareas;
- eventos;
- proyectos;
- metas;
- prioridades.

### Hábitos

- frecuencia;
- días;
- check/cantidad/duración;
- descanso;
- motivo;
- consistencia.

### Nutrición

- objetivos versionados;
- comidas;
- alimentos;
- porciones;
- calorías;
- macros;
- tendencia.

### Diario

- entrada libre;
- reflexión;
- victoria;
- problema;
- causa;
- aprendizaje;
- ajuste.

### Revisión

- plan vs ejecución;
- hábitos;
- nutrición;
- patrones;
- experimento semanal.

### Recordatorios

- hora;
- repetición;
- posponer;
- entrega;
- silencio;
- canal.

## 10. Primera entrega

Solo walking skeleton:

```text
React -> API -> Identity -> EF Core -> SQL Server
```

Debe incluir registro, login, `/me`, logout, cookie, antiforgery, ruta protegida, health, tests y CI.

## 11. Historias iniciales

### US-AUTH-001 Registro

Crear cuenta con nombre, correo y contraseña.

Aceptación:

- valida;
- no duplica correo;
- no filtra secretos;
- errores seguros;
- pruebas.

### US-AUTH-002 Login

Iniciar sesión con `rememberMe`.

Aceptación:

- Identity;
- lockout;
- cookie HttpOnly;
- sin token en storage;
- error genérico seguro.

### US-AUTH-003 Ruta protegida

El dashboard solo aparece con sesión válida.

### US-AUTH-004 Logout

Cerrar sesión e invalidarla.

## 12. Métricas futuras

- activación;
- días activos;
- cierres diarios;
- tiempo de captura;
- consistencia;
- revisiones semanales;
- recomendaciones aceptadas.

No usar métricas como castigo.

## 13. Restricciones

- usuario inicial único;
- presupuesto reducido;
- React es objetivo de aprendizaje;
- .NET/SQL Server reducen incertidumbre;
- responsive;
- diario sensible;
- despliegue separado de LuxuryCloud.

## 14. Riesgos

| Riesgo | Impacto | Mitigación |
|---|---:|---|
| construir todo a la vez | Alto | roadmap |
| fricción | Alto | quick capture |
| gamificación punitiva | Alto | recuperación |
| insights con pocos datos | Medio | reglas simples |
| sobrearquitectura | Medio | monolito modular |
| abandono | Alto | “Hoy” temprano |
| nutrición mal interpretada | Alto | límites no médicos |
| exposición del diario | Alto | seguridad reforzada |

## 15. Preguntas abiertas

- nombre final;
- cifrado del diario;
- canal de recordatorios;
- Google Calendar;
- catálogo de alimentos;
- XP;
- offline.

No bloquean el walking skeleton.
