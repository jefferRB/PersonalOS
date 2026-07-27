# PersonalOS — Producto, alcance y experiencia

**Versión:** 1.0  
**Estado:** Baseline

## 1. Problema

La vida personal suele administrarse con herramientas separadas:

- calendario;
- notas;
- tareas;
- hábitos;
- contador de calorías;
- diario;
- recordatorios.

La fragmentación provoca duplicación, falta de contexto y dificultad para convertir datos en decisiones.

## 2. Propuesta

PersonalOS unifica:

```text
Captura -> Planificación -> Ejecución -> Registro -> Revisión -> Ajuste
```

El producto debe ser rápido, tranquilo y explicable. No debe convertirse en un ERP personal, un sistema punitivo ni una colección de gráficas sin acción.

## 3. Usuario inicial

Jefferson:

- estudia y trabaja;
- desarrolla software;
- quiere organizar prioridades;
- controla hábitos;
- necesita gestionar calorías;
- quiere registrar aprendizajes;
- busca mejorar semanalmente;
- utiliza el proyecto para aprender React.

## 4. Principios de producto

1. Acción antes que decoración.
2. Captura rápida.
3. Tres prioridades antes que veinte tareas.
4. Recuperación antes que culpa.
5. Datos históricos honestos.
6. Recomendaciones explicables.
7. Privacidad por defecto.
8. Accesibilidad.
9. Control del usuario.
10. Crecimiento incremental.

## 5. Módulos

### Identidad

- registro;
- login;
- logout;
- usuario actual;
- preferencias;
- zona horaria.

### Hoy

- fecha local;
- próximos eventos;
- tres prioridades;
- tareas;
- hábitos;
- calorías;
- captura rápida;
- recomendación;
- cierre diario.

### Planificación

- tareas;
- eventos;
- proyectos;
- metas;
- fechas;
- prioridades;
- reprogramación.

### Hábitos

- frecuencia;
- días;
- check;
- cantidad;
- duración;
- descanso;
- motivo de incumplimiento;
- consistencia;
- recuperación.

### Nutrición

- objetivos versionados;
- comidas;
- alimentos;
- porciones;
- calorías;
- macronutrientes;
- resumen diario y semanal.

### Diario

- entrada libre;
- reflexión matutina;
- cierre nocturno;
- victoria;
- problema;
- causa;
- aprendizaje;
- ajuste.

### Revisión semanal

- plan frente a ejecución;
- hábitos;
- nutrición;
- patrones;
- experimento para la siguiente semana.

### Recordatorios

- fecha y hora;
- repetición;
- posponer;
- horario silencioso;
- estado de entrega;
- canal.

## 6. Experiencia diaria

### Inicio

- revisar agenda;
- elegir tres prioridades;
- ver hábitos;
- revisar objetivo calórico;
- definir intención.

### Durante el día

- captura rápida;
- completar tareas;
- registrar hábitos;
- registrar comidas;
- añadir notas.

### Cierre

- revisar resultados;
- registrar lo bueno;
- registrar lo difícil;
- identificar causa;
- definir ajuste;
- preparar mañana.

### Revisión semanal

- comparar;
- reconocer patrones;
- elegir un experimento;
- ajustar objetivos.

## 7. UX

Navegación futura:

```text
Hoy
Planificar
Hábitos
Nutrición
Diario
Revisión
Configuración
```

Todo flujo debe contemplar:

- loading;
- empty;
- success;
- validation;
- conflict;
- unauthorized;
- rate limit;
- server error;
- offline cuando aplique.

Requisitos:

- teclado;
- labels;
- foco visible;
- HTML semántico;
- no depender solo del color;
- mobile-first para captura;
- desktop para análisis.

## 8. Gamificación

Permitido:

- progreso;
- consistencia;
- recuperación;
- niveles;
- récords;
- experimentos.

No permitido:

- humillación;
- castigo agresivo;
- perder todo por un fallo;
- premiar comer peligrosamente poco;
- dark patterns.

## 9. MVP

El MVP completo incluye:

- cuenta segura;
- Hoy;
- tareas;
- hábitos;
- nutrición;
- diario;
- revisión;
- tendencias;
- exportación;
- PWA.

## 10. Fuera del MVP

- gestor de contraseñas;
- consejo médico;
- red social;
- marketplace;
- microservicios;
- billing;
- multi-tenancy;
- aplicación nativa;
- IA que modifique datos sin aprobación.

## 11. Roadmap

### M1 — Walking skeleton

- Identity;
- cookies;
- antiforgery;
- React protegido;
- pruebas;
- CI.

### M2 — Perfil y tiempo

- zona horaria;
- fecha local;
- reloj abstraído.

### M3 — Planificación

- tareas;
- prioridades;
- Hoy mínimo.

### M4 — Hábitos

- definición;
- registro;
- consistencia.

### M5 — Nutrición

- objetivos;
- comidas;
- calorías.

### M6 — Diario

- entradas;
- cierre;
- privacidad.

### M7 — Revisión

- agregados;
- tendencias;
- recomendaciones explicables.

### M8 — PWA y recordatorios

- instalación;
- offline;
- push;
- scheduler.

### M9 — Hardening

- email confirmation;
- recovery;
- MFA/passkeys;
- CSP;
- backup;
- staging;
- observabilidad.

## 12. Métricas futuras

- días activos;
- cierres diarios;
- tareas completadas;
- hábitos registrados;
- revisiones semanales;
- tiempo de captura;
- recomendaciones aceptadas;
- uso continuo.

Las métricas no deben convertirse en castigo.
