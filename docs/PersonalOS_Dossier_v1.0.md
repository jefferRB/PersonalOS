# PersonalOS - Dossier v1.0

# 1. Charter de producto

## 1.1 Visión

PersonalOS será un sistema operativo personal que conecta planificación, ejecución, registro y reflexión para ayudar al usuario a mejorar su sistema semanalmente. No pretende maximizar actividad ni castigar días imperfectos; pretende convertir datos cotidianos en decisiones pequeñas, sostenibles y explicables.

## 1.2 Problema

La información personal suele estar fragmentada entre agenda, notas, aplicaciones de hábitos, contadores de calorías, recordatorios y diarios. Esa fragmentación impide responder preguntas simples: qué debo hacer ahora, por qué estoy fallando, qué patrón se repite y qué ajuste concreto conviene probar.

## 1.3 Propuesta de valor

- Un centro de mando diario con agenda, prioridades, hábitos y objetivo nutricional.
- Captura rápida para reducir fricción durante el día.
- Cierre diario que convierte experiencias en aprendizaje.
- Revisión semanal con métricas, causas y un experimento de mejora.
- Privacidad por diseño y exportación completa de los datos.
- Evolución gradual desde reglas explicables hacia asistencia de IA autorizada.

## 1.4 Usuario inicial

El producto comienza como una aplicación de un solo usuario, diseñada para Jefferson. El modelo conservará `UserId` y límites claros para evitar bloquear una evolución multiusuario, pero no se implementarán multi-tenancy, facturación ni administración comercial durante el MVP.

## 1.5 Principios de producto

1. **Acción antes que decoración.** La pantalla principal indica qué hacer, no solo qué ocurrió.
2. **Menos compromisos, mejor cumplimiento.** El sistema no premia crear listas imposibles.
3. **Explicaciones antes que puntajes.** Toda alerta debe indicar datos y razonamiento.
4. **Recuperación antes que perfección.** Se mide la capacidad de volver al sistema después de fallar.
5. **Captura en segundos.** Registrar una comida, hábito o idea no debe interrumpir el día.
6. **Privacidad como requisito funcional.** Diario, exportaciones y futuro vault reciben controles adicionales.
7. **Datos portables.** El usuario puede exportar y borrar su información.
8. **IA opcional.** El producto funciona sin IA; la IA amplifica, no sustituye reglas ni criterio.
9. **Salud sin extremos.** Los objetivos nutricionales son definidos por el usuario y el sistema no incentiva déficits peligrosos.
10. **Construcción observable.** Cada release deja pruebas, ADRs, métricas y evidencia de operación.

## 1.6 Resultados esperados

- Utilizar la aplicación al menos cinco días por semana durante ocho semanas.
- Completar el cierre diario en menos de cinco minutos.
- Consultar la pantalla Hoy como primera fuente de prioridades.
- Reducir tareas vencidas sin fecha o sin siguiente acción.
- Poder explicar, mediante datos, al menos un patrón semanal útil.
- Mantener exportación y restauración verificadas desde la primera versión pública personal.

## 1.7 Métricas de producto

| Métrica | Definición | Señal saludable inicial |
|---|---|---|
| Activación personal | Día con agenda revisada y al menos un registro | 5/7 días |
| Cierre diario | Reflexión nocturna completada | >= 4/7 días |
| Cumplimiento planificado | Prioridades completadas / prioridades comprometidas | 60-85 %, evitando 100 % artificial |
| Recuperación | Días necesarios para volver tras un fallo | <= 2 días |
| Fricción de captura | Tiempo para guardar acción frecuente | < 10 segundos |
| Calidad de insights | Recomendaciones aceptadas o descartadas con razón | >= 1 útil por semana |
| Portabilidad | Exportación y restauración probadas | 100 % por release mayor |

## 1.8 No objetivos iniciales

- Competir comercialmente con gestores de contraseñas especializados.
- Ofrecer recomendaciones médicas o nutricionales clínicas.
- Crear una red social, sistema de comparación con otras personas o ranking público.
- Incorporar microservicios, event sourcing o una plataforma de plugins en el MVP.
- Automatizar decisiones importantes sin confirmación del usuario.


---

# 2. Requisitos y alcance

## 2.1 Alcance del MVP

El MVP debe ser suficientemente pequeño para completarse y suficientemente útil para reemplazar herramientas dispersas. Incluye autenticación, configuración personal, centro de mando Hoy, tareas, eventos manuales, hábitos, captura rápida, diario básico, objetivo nutricional diario, registro simple de comidas, cierre diario, revisión semanal inicial, exportación JSON y recordatorios persistentes.

El MVP excluye integración completa con calendarios externos, reconocimiento automático de alimentos, recomendaciones generativas, colaboración, pagos y vault de contraseñas.

## 2.2 Requisitos funcionales prioritarios

| ID | Capacidad | Criterio verificable | Prioridad |
|---|---|---|---|
| RF-001 | Autenticación | El usuario inicia y cierra sesión; las rutas privadas no son accesibles sin sesión | Must |
| RF-002 | Configuración | Zona horaria, idioma, horarios silenciosos y objetivo calórico quedan versionados | Must |
| RF-003 | Pantalla Hoy | Muestra agenda, 3 prioridades, hábitos, progreso nutricional y siguiente acción | Must |
| RF-004 | Tareas | Crear, editar, completar, posponer y archivar tareas sin perder historial | Must |
| RF-005 | Eventos | Registrar eventos con inicio, fin, zona horaria y recordatorios | Must |
| RF-006 | Hábitos | Definir frecuencia, métrica, objetivo y días de descanso planificados | Must |
| RF-007 | Registro de hábitos | Marcar cumplimiento, valor parcial, omisión justificada o descanso | Must |
| RF-008 | Nutrición | Registrar comidas, calorías y macros opcionales por fecha local | Must |
| RF-009 | Diario | Crear entradas libres, matutinas y nocturnas con autosave seguro | Must |
| RF-010 | Cierre diario | Registrar lo bueno, lo malo, aprendizaje y cambio para mañana | Must |
| RF-011 | Revisión semanal | Comparar plan vs. realidad y definir un experimento siguiente | Should |
| RF-012 | Recordatorios | Programar, posponer, entregar, reintentar y auditar recordatorios | Must |
| RF-013 | Captura rápida | Crear tarea, nota, comida o hábito desde una entrada global | Should |
| RF-014 | Exportación | Descargar todos los datos en JSON legible y versionado | Must |
| RF-015 | Eliminación | Borrar datos seleccionados o la cuenta con confirmación reforzada | Should |
| RF-016 | Dashboard | Mostrar tendencias semanales sin convertir correlación en causalidad | Should |

## 2.3 Requisitos no funcionales

| ID | Atributo | Requisito inicial |
|---|---|---|
| RNF-001 | Seguridad | Cookies HttpOnly/Secure, antiforgery, validación de entrada, least privilege y secretos fuera del repositorio |
| RNF-002 | Privacidad | Clasificación de datos, minimización, exportación y borrado verificables |
| RNF-003 | Disponibilidad | Objetivo personal de 99 %, con degradación aceptable para funciones no críticas |
| RNF-004 | Rendimiento | Pantalla Hoy interactiva en < 2 s sobre red doméstica; acciones optimistas < 200 ms percibidos |
| RNF-005 | Accesibilidad | Navegación por teclado, contraste AA, labels, foco visible y reduced motion |
| RNF-006 | Mantenibilidad | Monolito modular, límites de módulos, ADRs y pruebas por caso de uso |
| RNF-007 | Observabilidad | Correlation ID, logs estructurados, métricas de worker y health checks |
| RNF-008 | Recuperación | Backup cifrado; restauración ensayada; migraciones con rollback operativo |
| RNF-009 | Portabilidad | PWA responsive en escritorio y móvil; datos exportables sin dependencia del proveedor |
| RNF-010 | Correctitud temporal | UTC para instantes, fecha local explícita para hábitos/comidas y zona horaria del usuario |

## 2.4 Historias de usuario clave

### US-001 - Preparar el día
Como usuario, quiero revisar agenda, energía y tres prioridades en una sola pantalla para comenzar con un plan realista.

**Aceptación:** no permite comprometer más de tres prioridades principales sin una advertencia explícita; muestra conflictos de horario y tareas vencidas.

### US-002 - Registrar un hábito sin fricción
Como usuario, quiero marcar o cuantificar un hábito en dos interacciones para mantener datos consistentes.

**Aceptación:** distingue cumplido, parcial, omitido, descanso y no registrado; permite corregir el mismo día con auditoría mínima.

### US-003 - Registrar alimentación
Como usuario, quiero guardar una comida frecuente rápidamente y ver el acumulado contra el objetivo vigente de ese día.

**Aceptación:** los históricos se comparan con el objetivo efectivo en la fecha, no con el objetivo actual.

### US-004 - Cerrar el día
Como usuario, quiero revisar resultados y registrar una mejora para mañana en menos de cinco minutos.

**Aceptación:** guarda borradores, evita perder texto y crea opcionalmente una tarea o ajuste a partir del aprendizaje.

### US-005 - Recibir un recordatorio confiable
Como usuario, quiero recibir recordatorios sin duplicados y poder posponerlos.

**Aceptación:** cada entrega tiene clave idempotente, estado, canal, intentos y respuesta del usuario.

## 2.5 Reglas de alcance

- Una funcionalidad no entra al MVP porque sea atractiva; entra si completa el ciclo diario.
- Todo dato nuevo debe justificar por qué se captura y qué decisión permite tomar.
- Ningún dashboard se implementa antes de definir su pregunta, fuente, fórmula y posible interpretación errónea.
- Las mejoras se prueban con uso real semanal antes de expandirlas.


---

# 3. Arquitectura de software

## 3.1 Estilo arquitectónico

Se adopta un **monolito modular con slices verticales**. Un único despliegue reduce complejidad operativa mientras los módulos mantienen límites de negocio. Los casos de uso agrupan endpoint, validación, autorización, lógica, persistencia y pruebas relacionadas. No se introduce un repositorio genérico sobre EF Core.

## 3.2 Stack objetivo

- Frontend: React 19.x, TypeScript y Vite.
- Navegación: React Router.
- Estado remoto: TanStack Query; estado local con hooks y Context; otra librería solo ante una necesidad demostrada.
- Formularios: React Hook Form y Zod.
- Backend: ASP.NET Core 10 Web API.
- Persistencia: EF Core 10 y SQL Server.
- Identidad: ASP.NET Core Identity con cookies de sesión.
- Contrato: REST JSON documentado con OpenAPI.
- Cliente: PWA responsive con offline parcial y push donde el navegador lo permita.
- Pruebas: xUnit, pruebas de integración, Vitest, React Testing Library y Playwright.

La selección prioriza aprendizaje de React sin abandonar el dominio operativo ya adquirido en .NET, EF Core y SQL Server. React 19.2 figura como versión vigente en la documentación oficial; .NET 10 es LTS activo y Microsoft publica soporte hasta noviembre de 2028. La versión exacta se fijará y actualizará mediante dependabot o proceso equivalente, nunca mediante rangos ambiguos en producción.

## 3.3 Contenedores y responsabilidades

1. **PWA React:** interacción, validación inmediata, estados de carga/error, caché de consultas, optimismo controlado y gráficos.
2. **API ASP.NET Core:** autenticación, autorización, reglas de negocio, contratos, auditoría y exportación.
3. **Worker:** recordatorios, reintentos, expiraciones, resúmenes y tareas programadas.
4. **SQL Server:** fuente de verdad transaccional.
5. **Almacenamiento de respaldo:** exportaciones y copias cifradas con retención.
6. **Observabilidad:** logs estructurados, métricas, traces selectivos y health checks.

## 3.4 Módulos

| Módulo | Responsabilidad | Dependencias permitidas |
|---|---|---|
| Identity & Profile | Usuario, preferencias, zona horaria, sesión y seguridad | Infraestructura común |
| Productivity | Tareas, eventos, proyectos y metas | Identity, Notifications |
| Habits | Definición, programación, registro y recuperación | Identity, Notifications |
| Nutrition | Objetivos versionados, comidas y agregados diarios | Identity |
| Journal | Entradas, reflexiones y búsquedas | Identity, Security services |
| Today | Proyección del día y comandos rápidos | Lee Productivity, Habits, Nutrition, Journal |
| Insights | Métricas, revisiones y recomendaciones explicables | Proyecciones de módulos; no modifica datos fuente directamente |
| Notifications | Programación, canales, intentos e idempotencia | Identity y adaptadores externos |
| Export & Backup | Portabilidad, respaldo y restauración | Todos mediante contratos de lectura |
| Vault futuro | Secretos cifrados del lado del cliente | Aislado; no depende de Insights ni IA |

## 3.5 Reglas de dependencia

- El módulo Today compone información; no se convierte en dueño de tareas, hábitos o comidas.
- Insights lee eventos y agregados, pero toda acción propuesta requiere un comando explícito.
- Los proveedores externos se consumen mediante adaptadores en Infrastructure.
- Las entidades no conocen HTTP, React, SQL ni proveedores.
- Las migraciones pertenecen al backend y se ensayan contra una copia antes de producción.

## 3.6 API

Convenciones propuestas:

- Rutas por módulo: `/api/tasks`, `/api/habits`, `/api/nutrition`, `/api/journal`, `/api/today`.
- Errores con Problem Details y códigos estables.
- `ETag` o versión de fila en ediciones sensibles para detectar conflictos.
- `Idempotency-Key` en comandos susceptibles a doble envío.
- Paginación cursor-based para diarios y actividad histórica.
- Filtros con fechas locales explícitas y zona horaria documentada.
- OpenAPI generado y validado en CI.

## 3.7 Consistencia y eventos internos

El MVP utiliza transacciones locales. Después de guardar una operación, puede registrar un evento interno persistible para notificaciones o proyecciones. No se necesita un broker distribuido. Para evitar pérdida entre transacción y envío externo se puede incorporar un patrón outbox cuando los recordatorios o integraciones lo requieran.

## 3.8 Decisiones que se posponen

- SSR o React Server Components: la PWA autenticada no los necesita inicialmente.
- GraphQL: REST cubre los casos y simplifica aprendizaje y observabilidad.
- Microservicios: no hay escala organizacional ni operativa que los justifique.
- Kubernetes: un servidor Linux o contenedores simples son suficientes.
- IA embebida en cada flujo: primero se validan reglas y calidad de datos.


---

# 4. Modelo de dominio y datos

## 4.1 Lenguaje ubicuo

- **Prioridad:** tarea seleccionada como compromiso principal del día.
- **Tarea:** acción completables; puede tener fecha límite pero no necesariamente una hora fija.
- **Evento:** compromiso con inicio y fin temporal.
- **Hábito:** comportamiento recurrente medido por estado, cantidad, duración o restricción.
- **Registro:** observación ocurrida en una fecha local.
- **Objetivo:** resultado medible de mayor plazo.
- **Proyecto:** esfuerzo finito compuesto por tareas.
- **Experimento semanal:** cambio pequeño y explícito que se evaluará en la siguiente revisión.
- **Insight:** explicación derivada de datos con evidencia, confianza y limitaciones.

## 4.2 Agregados iniciales

### UserProfile
Raíz de preferencias personales. Conserva zona horaria IANA, locale, unidades, horarios silenciosos y configuración de privacidad. Los cambios relevantes son versionados.

### TaskItem
Mantiene título, estado, prioridad, proyecto opcional, fecha límite, programación y versión de concurrencia. Completar una tarea registra `CompletedAt`; reabrirla conserva historial.

### Habit
Define tipo de métrica, programación y objetivo. Sus logs son hechos históricos; cambiar la frecuencia no reescribe el pasado.

### NutritionTarget
Es un rango efectivo. Nunca se sobrescribe el objetivo histórico. Cada día obtiene el objetivo que estaba vigente según fecha local.

### JournalEntry
Distingue libre, matutina, nocturna, victoria, error, decisión e idea. Puede incorporar cifrado de aplicación en una fase posterior; siempre se excluye de logs y telemetría de contenido.

### Reminder
Conserva due time, zona, canal preferido, estado, intentos, deduplicación y vínculo al objeto de origen. Una entrega es un registro independiente.

### WeeklyReview
Congela métricas de la semana, interpretaciones aceptadas/descartadas y el experimento elegido. No depende de recalcular eternamente con fórmulas futuras.

## 4.3 Invariantes esenciales

1. Una fecha local se calcula con la zona horaria efectiva, no restando horas manualmente.
2. Un hábito no puede generar dos registros activos para la misma instancia programada.
3. Un objetivo nutricional no puede tener rangos efectivos superpuestos.
4. Completar una tarea es idempotente.
5. Un recordatorio no se entrega dos veces con la misma clave de intento.
6. Los cambios de zona horaria no alteran instantes históricos.
7. El cierre diario puede existir una vez por fecha local, con versiones de borrador.
8. Un insight no modifica datos fuente.
9. El contenido del diario jamás aparece en logs de aplicación.
10. La eliminación de cuenta invalida sesiones antes de iniciar el proceso destructivo.

## 4.4 Estrategia temporal

- `DateTimeOffset`/UTC para eventos e instantes.
- `LocalDate` conceptual para comidas, hábitos, cierres y revisiones.
- Zona horaria IANA en perfil y snapshot cuando una regla dependa de ella.
- Tolerancia explícita a cambios de horario de verano, aunque Costa Rica no lo utilice.
- Los recordatorios recurrentes almacenan la regla y calculan próximas ocurrencias de forma determinista.

## 4.5 Datos derivados

Los dashboards consultan vistas/proyecciones, no duplican toda la lógica en React. Los cálculos deben tener:

- nombre estable;
- fórmula documentada;
- período y zona horaria;
- datos incluidos/excluidos;
- tratamiento de faltantes;
- advertencia contra interpretaciones causales.

## 4.6 Retención y exportación

- Datos operativos: retención mientras exista la cuenta.
- Logs técnicos: contenido mínimo y retención corta.
- Entregas de notificación: historial suficiente para diagnóstico, sin contenido sensible innecesario.
- Exportación: esquema versionado, manifest, checksums y zona horaria.
- Restauración: valida versión, integridad y conflictos antes de escribir.


---

# 5. Experiencia de usuario y flujos

## 5.1 Arquitectura de información

Navegación principal propuesta:

1. **Hoy** - centro de mando.
2. **Plan** - tareas, agenda, proyectos y metas.
3. **Hábitos** - cumplimiento y configuración.
4. **Nutrición** - comidas, objetivos y tendencias.
5. **Diario** - entradas y reflexiones.
6. **Revisión** - cierre diario y semanal.
7. **Ajustes** - perfil, privacidad, datos y notificaciones.

La captura rápida permanece disponible desde cualquier pantalla mediante teclado y botón flotante móvil.

## 5.2 Journey de mañana

1. Abrir Hoy.
2. Registrar energía y sueño opcional.
3. Revisar compromisos fijos.
4. Resolver conflictos o tareas vencidas.
5. Seleccionar hasta tres prioridades.
6. Confirmar hábitos relevantes.
7. Ver recomendación breve basada en reglas.

El sistema evita convertir la mañana en una sesión administrativa. Valores frecuentes se recuerdan y el flujo puede omitirse.

## 5.3 Journey durante el día

- Marcar hábitos desde la tarjeta de Hoy.
- Agregar comida frecuente en pocos toques.
- Capturar una tarea sin clasificar y organizarla después.
- Iniciar foco desde una prioridad.
- Posponer recordatorios con opciones concretas.
- Registrar una nota de aprendizaje vinculada al día.

## 5.4 Journey de cierre

1. Revisar prioridades, tareas y hábitos.
2. Resolver pendientes: mover, cancelar o descomponer.
3. Registrar lo mejor, lo peor y la causa probable.
4. Elegir una acción concreta para mañana.
5. Preparar una prioridad tentativa.
6. Cerrar sin mensajes culpabilizantes.

## 5.5 Revisión semanal

La revisión presenta primero hechos, luego interpretación y finalmente decisión:

- plan vs. realidad;
- promedio de calorías contra objetivos diarios vigentes;
- cumplimiento y recuperación de hábitos;
- tareas creadas/completadas/pospuestas;
- días con mayor y menor energía;
- patrones sugeridos con nivel de confianza;
- experimento de la siguiente semana.

## 5.6 Sistema visual

- Diseño sobrio, compacto y motivador; no infantil.
- Colores semánticos consistentes para completado, riesgo, información y descanso.
- Tipografía legible; densidad adaptable entre móvil y escritorio.
- Gráficos con texto alternativo y tabla de datos accesible.
- Animaciones opcionales y compatibles con `prefers-reduced-motion`.
- Estados vacíos que enseñan la siguiente acción.

## 5.7 Estados obligatorios por pantalla

Cada feature debe diseñar:

- carga inicial;
- carga incremental;
- vacío nuevo;
- vacío por filtros;
- error recuperable;
- error sin conexión;
- conflicto de concurrencia;
- éxito y deshacer;
- permisos insuficientes;
- dato parcial o atrasado.

## 5.8 Accesibilidad

Objetivo WCAG 2.2 AA pragmático: navegación completa por teclado, foco visible, estructura semántica, labels programáticos, contraste, targets táctiles adecuados, anuncios para cambios dinámicos y ausencia de información comunicada solo por color.


---

# 6. Seguridad, privacidad y threat model

## 6.1 Clasificación de datos

| Clase | Ejemplos | Controles mínimos |
|---|---|---|
| Pública | assets y documentación pública | integridad y CSP |
| Interna | preferencias visuales, estados no sensibles | autorización y backups |
| Sensible | diario, hábitos, peso, nutrición, exportaciones | cifrado en tránsito/reposo, redacción de logs, reautenticación contextual |
| Crítica | contraseñas guardadas, clave maestra | cifrado cliente, aislamiento, no acceso del servidor al texto claro |

## 6.2 Activos y amenazas principales

- Cuenta y sesión del usuario.
- Contenido íntimo del diario.
- Información nutricional y de hábitos.
- Backups y exportaciones.
- Canales de notificación que pueden revelar contexto.
- Futuro vault de credenciales.

Amenazas prioritarias: robo de sesión, CSRF, XSS, acceso indebido a exportaciones, filtración por logs, backup sin cifrar, duplicación de recordatorios, abuso de endpoints, dependencia comprometida, pérdida de dispositivo y exposición accidental en notificaciones.

## 6.3 Controles de autenticación

- ASP.NET Core Identity con cookies HttpOnly y Secure en producción.
- SameSite según topología real y protección antiforgery para comandos.
- Rotación de sesión tras login y cambios sensibles.
- Security stamp y revocación de dispositivos.
- Rate limiting para login, recuperación y exportación.
- MFA opcional antes de publicar fuera del uso personal.
- Reautenticación para exportar, borrar cuenta o cambiar seguridad.

## 6.4 Controles de aplicación

- Validación en frontera y reglas repetidas en dominio cuando sean invariantes.
- Content Security Policy sin `unsafe-inline` salvo excepción documentada.
- Encoding de salida y prohibición de HTML de diario sin sanitización estricta.
- Consultas parametrizadas mediante EF Core.
- Principio de mínimo privilegio para usuario de base de datos.
- Secretos mediante variables/secret store, nunca en repositorio ni prompts.
- Uploads futuros con allowlist, magic bytes, tamaño y almacenamiento separado.

## 6.5 Privacidad por diseño

- Capturar solo datos que produzcan una decisión o experiencia útil.
- Desactivar telemetría de contenido; usar IDs y categorías no sensibles.
- Notificaciones con vista previa configurable.
- Exportación legible y borrado verificable.
- Consentimiento específico antes de enviar datos a un proveedor de IA.
- Posibilidad de excluir entradas o categorías del análisis.

## 6.6 Vault de contraseñas

El vault no forma parte del MVP. Para considerarlo apto se requiere un threat model específico, cifrado del lado del cliente, derivación de clave robusta, bloqueo automático, no recuperación de la clave maestra por el servidor, revisión criptográfica independiente y pruebas de pérdida/recuperación. Hasta entonces no se usarán credenciales críticas reales.

## 6.7 Abuso de gamificación

La seguridad también incluye evitar daño por diseño. El sistema no recompensa déficit calórico extremo, privación de sueño, sobreentrenamiento ni cadenas de hábitos imposibles. Los mensajes deben evitar culpa, comparación pública y patrones compulsivos.

## 6.8 Checklist de release de seguridad

- Dependencias auditadas y actualizadas.
- Secret scanning sin hallazgos.
- Pruebas de autorización por endpoint.
- Pruebas CSRF/XSS básicas.
- Exportaciones no indexables, temporales y revocables.
- Logs inspeccionados para PII/contenido.
- Backup cifrado y restauración probada.
- Cabeceras de seguridad verificadas.
- Threat model actualizado si cambió superficie de ataque.


---

# 7. Calidad y estrategia de pruebas

## 7.1 Pirámide orientada a riesgos

- **Unitarias:** reglas puras, calendarios, objetivos versionados, rachas y cálculos.
- **Integración:** API + base de datos real en contenedor; autenticación, autorización, transacciones y migraciones.
- **Componentes React:** interacción, accesibilidad, estados y validación.
- **Contrato:** OpenAPI y compatibilidad de cliente generado o tipado.
- **E2E:** journeys críticos de mañana, captura, cierre, exportación y recordatorios.
- **Operación:** smoke tests, restauración y pruebas de worker.

## 7.2 Casos de alto riesgo

1. Fechas alrededor de medianoche y cambio de zona horaria.
2. Doble clic o reintento de red que crea duplicados.
3. Cambio de objetivo nutricional con histórico existente.
4. Edición simultánea del diario desde dos pestañas.
5. Recordatorio entregado pero respuesta del proveedor perdida.
6. Migración que modifica datos históricos.
7. Exportación interrumpida o restauración parcial.
8. Acceso horizontal a datos de otro `UserId` en una evolución multiusuario.

## 7.3 Quality gates de CI

| Gate | Criterio |
|---|---|
| Compilación | cero errores y warnings nuevos tratados según baseline |
| Lint/format | frontend y backend sin desviaciones |
| Unitarias | 100 % verdes; cobertura usada como señal, no objetivo aislado |
| Integración | migración desde base anterior y casos críticos verdes |
| Frontend | pruebas de componentes y accesibilidad automática |
| Contrato | OpenAPI válido; breaking changes detectados |
| Seguridad | dependency scan, secret scan y análisis estático |
| E2E smoke | login, Hoy, registro y cierre diario |

## 7.4 Definition of Done de una feature

- Problema y alcance documentados.
- ADR cuando cambia una decisión transversal.
- Criterios de aceptación verificables.
- Estados UX completos.
- Autorización y validación implementadas.
- Pruebas unitarias/integración/componentes según riesgo.
- Logs y métricas sin datos sensibles.
- Documentación de API actualizada.
- Migración ensayada si aplica.
- Evidencia visual o video corto.
- Revisión independiente de Claude/Codex sin hallazgos críticos abiertos.

## 7.5 Datos de prueba

Se utilizarán builders/factories y escenarios deterministas. Los datos sensibles reales no entran al repositorio. Las fechas se controlan mediante una abstracción de reloj. Los E2E generan su propio usuario y limpian el entorno.

## 7.6 Pruebas de usabilidad personal

Cada semana se registrará:

- acciones lentas o repetitivas;
- datos que nunca se consultan;
- campos que se omiten;
- alertas ignoradas;
- errores de interpretación;
- feature que sustituyó o no una herramienta anterior.

El resultado se transforma en backlog, no en cambios impulsivos sin evidencia.


---

# 8. DevOps, despliegue y observabilidad

## 8.1 Entornos

- **Local:** frontend y backend con base aislada; datos sintéticos.
- **CI:** servicios efímeros, migración desde cero y desde snapshot anterior.
- **Staging:** topología equivalente a producción, sin datos personales reales.
- **Producción:** dominio HTTPS, backups, monitoring y acceso administrativo limitado.

## 8.2 Pipeline

1. Restaurar dependencias con lockfiles.
2. Compilar backend y frontend.
3. Ejecutar formato, lint y análisis estático.
4. Ejecutar pruebas unitarias, integración y componentes.
5. Generar OpenAPI y verificar cambios.
6. Construir artefacto inmutable/versionado.
7. Desplegar a staging.
8. Ejecutar migración ensayada y smoke tests.
9. Promover el mismo artefacto a producción.
10. Verificar health, métricas y logs post-deploy.

## 8.3 Migraciones

- Una migración por cambio coherente.
- Scripts revisables y ejecutables con transacción cuando sea seguro.
- Backup verificado antes de cambios destructivos.
- Expand/contract para cambios incompatibles.
- No editar una migración ya aplicada en un entorno compartido.
- Prueba de actualización desde la versión productiva anterior.
- Runbook de recuperación documentado.

## 8.4 Observabilidad

Métricas iniciales:

- latencia y tasa de errores por endpoint;
- tiempo de carga de Today;
- recordatorios pendientes, entregados, fallidos y atrasados;
- jobs ejecutados y duración;
- conexiones y errores de base de datos;
- tamaño y edad del último backup;
- tasa de cierres diarios y capturas, de forma privada y local cuando sea posible.

Los logs usan correlation ID, categorías estables y eventos estructurados. No registran cuerpo del diario, contraseñas, tokens, alimentos detallados ni exportaciones.

## 8.5 SLOs iniciales

- 99 % de disponibilidad mensual personal.
- 95 % de requests de Today bajo 500 ms en servidor.
- 99 % de recordatorios procesados dentro de cinco minutos de su vencimiento cuando el proveedor está disponible.
- RPO de 24 horas y RTO de cuatro horas durante la etapa personal.

## 8.6 Runbooks mínimos

- Deploy y rollback.
- Migración fallida.
- Worker detenido o recordatorios atrasados.
- Base sin espacio o degradada.
- Restauración desde backup.
- Revocación de sesiones.
- Rotación de secretos.
- Incidente de privacidad.


---

# 9. Plan de aprendizaje de React mediante el producto

## 9.1 Principio

Cada concepto de React se aprende al resolver una necesidad visible. No se aceptan grandes bloques de código generados sin poder explicar flujo de datos, renderizado, efectos, caché y estados de error.

## 9.2 Ruta progresiva

| Etapa | Feature | Conceptos de React | Evidencia |
|---|---|---|---|
| 1 | Layout y navegación | JSX, componentes, props, composición | mapa de componentes y Storybook opcional |
| 2 | Lista de tareas | estado local, eventos, render condicional, keys | pruebas de componente |
| 3 | Formularios | controlled/uncontrolled, validación, schemas | formulario accesible con errores |
| 4 | API | fetch, async, loading/error, TanStack Query | cache y refetch explicados |
| 5 | Today | composición, selectors, datos derivados | profiler y límites de render |
| 6 | Hábitos | custom hooks, optimismo y rollback | test de fallo de red |
| 7 | Rutas privadas | router, loaders, auth state | pruebas de autorización UI |
| 8 | Diario | autosave, debounce, conflictos | prueba de dos pestañas |
| 9 | Dashboards | memoización necesaria, gráficos accesibles | tabla alternativa |
| 10 | PWA | service worker, offline, update lifecycle | instalación y actualización probadas |
| 11 | Testing | RTL, Vitest, MSW, Playwright | suite en CI |
| 12 | Performance | profiler, code splitting, virtualización | presupuesto medido |

## 9.3 Preguntas obligatorias por feature

- ¿Qué estado es local, remoto, derivado o parte de la URL?
- ¿Qué provoca un render?
- ¿Por qué existe este `useEffect` y podría eliminarse?
- ¿Quién es dueño del dato?
- ¿Qué ocurre sin conexión, con latencia o con respuesta fuera de orden?
- ¿Cómo se cancela o ignora una petición obsoleta?
- ¿Qué prueba demuestra el comportamiento?

## 9.4 Bitácora de aprendizaje

Cada PR tendrá una sección `Lo que aprendí` con:

- concepto nuevo;
- error cometido;
- explicación con palabras propias;
- alternativa descartada;
- enlace a documentación oficial;
- ejemplo pequeño independiente si el concepto fue complejo.

## 9.5 Criterio de dominio

Un tema se considera aprendido cuando Jefferson puede modificarlo sin copiar un patrón, predecir un error común, escribir una prueba y explicar el intercambio técnico en una revisión.


---

# 10. Flujo profesional con Claude y Codex

## 10.1 Roles

- **Jefferson:** product owner, arquitecto responsable y aprobador final.
- **ChatGPT:** análisis, diseño, explicación, documentación y preparación de prompts/revisiones.
- **Claude:** implementación acotada o análisis profundo de una tarea bien especificada.
- **Codex:** cambios de código, pruebas, diagnóstico y revisión independiente, según la tarea.

Ningún asistente es dueño de la arquitectura ni reemplaza la comprensión del cambio.

## 10.2 Paquete de trabajo por feature

1. Contexto y problema.
2. Comportamiento actual.
3. Resultado esperado.
4. Reglas e invariantes.
5. Contrato de API y modelo de datos.
6. Estados UX.
7. Seguridad y privacidad.
8. Criterios de aceptación.
9. Pruebas obligatorias.
10. Archivos fuera de alcance.
11. Comandos de verificación.
12. Formato del reporte final.

## 10.3 Secuencia recomendada

1. Diseñar y aprobar specification.
2. Pedir a una IA inspección antes de modificar.
3. Implementar un slice pequeño.
4. Ejecutar build/tests reales.
5. Revisar diff manualmente.
6. Pedir auditoría independiente a otro asistente.
7. Corregir hallazgos.
8. Explicar el código y actualizar documentación.
9. Commit atómico con evidencia.

## 10.4 Reglas de seguridad para prompts

- No copiar secretos, tokens, connection strings ni datos reales del diario.
- Redactar logs y payloads.
- No permitir comandos destructivos sin backup y alcance explícito.
- Pedir que la IA liste supuestos y archivos modificados.
- Prohibir refactors fuera de alcance.
- Exigir pruebas y salida de comandos, no afirmaciones vagas.
- Verificar librerías y APIs actuales en documentación oficial.

## 10.5 Plantilla de auditoría independiente

La segunda IA debe buscar: regresiones, violaciones de límites modulares, fallos de autorización, condiciones de carrera, problemas temporales, duplicados, pérdida de datos, exposición sensible, ausencia de pruebas y complejidad innecesaria. Debe devolver hallazgos ordenados por severidad, evidencia por archivo/línea y corrección propuesta.

## 10.6 Evidencia de autoría

Para que el proyecto sea válido en portafolio se conservarán ADRs, bitácoras, commits pequeños, discusiones de tradeoffs, videos de explicación y decisiones donde Jefferson rechazó o corrigió propuestas de IA. El valor no es ocultar el uso de herramientas, sino demostrar gobierno técnico sobre ellas.


---

# 11. Roadmap, backlog y entregas

## Fase 0 - Foundation documental

**Salida:** dossier aprobado, ADRs iniciales, backlog, repositorio y convenciones.  
**Gate:** no iniciar código sin alcance y arquitectura suficientemente definidos.

## Fase 1 - Walking skeleton

- Solución .NET y React.
- Login, perfil y shell de navegación.
- Base de datos y primera migración.
- Endpoint health y pipeline CI.
- Deploy mínimo a staging.

**Demo:** iniciar sesión y ver Today vacío en una PWA desplegada.

## Fase 2 - Planificación diaria

- Tareas, eventos y prioridades.
- Captura rápida.
- Vista Today.
- Manejo temporal correcto.

**Demo:** preparar un día real y completar tareas desde móvil.

## Fase 3 - Hábitos

- Tipos de hábito, programación, registros y descansos.
- Rachas y recuperación.
- Recordatorios básicos.

**Demo:** utilizar una semana sin duplicados ni confusión de fechas.

## Fase 4 - Nutrición

- Objetivos versionados.
- Comidas frecuentes y macros opcionales.
- Totales diarios y promedio semanal.

**Demo:** cambiar el objetivo sin alterar comparaciones históricas.

## Fase 5 - Diario y cierre

- Entradas, autosave y cierre diario.
- Privacidad reforzada.
- Preparación del día siguiente.

**Demo:** cerrar el día en menos de cinco minutos y recuperar un borrador.

## Fase 6 - Revisión e insights

- Dashboard semanal.
- Reglas explicables.
- Experimentos semanales.
- Gamificación responsable.

**Demo:** identificar un patrón con evidencia y convertirlo en una acción.

## Fase 7 - PWA y notificaciones confiables

- Instalación, offline parcial, push y fallback.
- Worker robusto, reintentos e idempotencia.

**Demo:** recibir, posponer y completar recordatorios sin duplicados.

## Fase 8 - Hardening y portafolio

- Exportación/restauración.
- Accesibilidad y performance.
- Threat model actualizado.
- Video, case study y arquitectura pública.

## Backlog posterior

Integración Calendar, foco/Pomodoro, metas/proyectos avanzados, reconocimiento de alimentos, wearable imports, IA opt-in y vault aislado tras auditoría específica.

## Definition of Ready

Una historia entra a desarrollo cuando tiene usuario, problema, resultado, criterios de aceptación, datos, estados UX, riesgos, dependencias y pruebas esperadas. Si cambia arquitectura, incluye ADR.


---

# 12. Estrategia de portafolio y evidencia profesional

## 12.1 Narrativa

El proyecto debe presentarse como un caso de estudio de arquitectura evolutiva y producto personal, no como una colección de pantallas. La historia central: convertir una necesidad diaria en un sistema mantenible, seguro, observable y medible, mientras se aprende React con gobierno técnico de IA.

## 12.2 Evidencias por competencia

| Competencia | Evidencia pública |
|---|---|
| Product thinking | Charter, métricas, no objetivos y resultados de uso real |
| Arquitectura | C4, límites modulares, ADRs y tradeoffs |
| Backend | API, dominio, EF Core, idempotencia y fechas |
| Frontend | componentes, estado remoto, accesibilidad y performance |
| Seguridad | threat model, controles, pruebas y exclusión consciente del vault |
| Calidad | estrategia, test matrix, CI y defectos prevenidos |
| DevOps | pipeline, staging, migraciones, observabilidad y runbooks |
| Datos | objetivos versionados, exportación y métricas reproducibles |
| Liderazgo técnico | revisiones de IA, decisiones rechazadas y documentación actualizada |

## 12.3 Entregables para CV/GitHub

- README ejecutivo con GIF/video corto.
- Diagrama C4 y arquitectura modular.
- Dossier PDF descargable.
- ADRs seleccionados.
- Capturas de CI y cobertura de journeys.
- Video de 5-8 minutos explicando un tradeoff real.
- Case study con problema, restricciones, decisiones, resultados y aprendizajes.
- Changelog y releases versionadas.

## 12.4 Frases de CV basadas en evidencia

No se usarán hasta que exista prueba real. Ejemplos futuros:

- Diseñé y construí una PWA con React y ASP.NET Core bajo arquitectura de monolito modular, documentando decisiones mediante ADRs y diagramas C4.
- Implementé recordatorios persistentes e idempotentes con worker, reintentos y observabilidad, validados mediante pruebas de integración y E2E.
- Definí un modelo temporal con objetivos versionados y fechas locales para preservar correctitud histórica.
- Establecí CI/CD con quality gates, migraciones ensayadas, análisis de seguridad y despliegue reproducible.

## 12.5 Honestidad profesional

La documentación distingue intención de resultado. No se afirmará alta disponibilidad, seguridad del vault, impacto en hábitos ni mejora de productividad sin pruebas. Se explicará el uso de IA como acelerador supervisado, incluyendo controles y revisiones humanas.


---

# 13. Glosario

- **ADR:** registro breve de una decisión arquitectónica, su contexto y consecuencias.
- **C4:** modelo de diagramación por contexto, contenedores, componentes y código.
- **Idempotencia:** propiedad por la cual repetir una operación produce el mismo resultado observable.
- **Invariante:** regla que siempre debe mantenerse válida en el dominio.
- **Local date:** fecha de calendario del usuario, separada de un instante UTC.
- **Monolito modular:** aplicación desplegable como unidad con límites internos explícitos.
- **Outbox:** patrón para guardar el evento y el cambio de datos en la misma transacción antes de procesarlo.
- **PWA:** aplicación web instalable con capacidades como caché, offline parcial y notificaciones según plataforma.
- **RPO/RTO:** pérdida máxima de datos tolerada y tiempo objetivo de recuperación.
- **Slice vertical:** implementación de un caso de uso de extremo a extremo, evitando capas desconectadas por tipo técnico.
- **SLO:** objetivo medible de confiabilidad.
- **Threat model:** análisis estructurado de activos, amenazas, superficies y controles.


---

# 14. Referencias técnicas base

- React, `React Versions`: https://react.dev/versions
- React, `Thinking in React`: https://react.dev/learn/thinking-in-react
- Vite, `Getting Started`: https://vite.dev/guide/
- Microsoft, `.NET Support Policy`: https://dotnet.microsoft.com/en-us/platform/support/policy
- Microsoft Learn, `ASP.NET Core antiforgery`: https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0
- Microsoft Learn, `ASP.NET Core security`: https://learn.microsoft.com/en-us/aspnet/core/security/
- OWASP, `Application Security Verification Standard`: https://owasp.org/www-project-application-security-verification-standard/
- OWASP Cheat Sheet Series, `Cryptographic Storage`: https://cheatsheetseries.owasp.org/cheatsheets/Cryptographic_Storage_Cheat_Sheet.html
- OWASP Cheat Sheet Series, `Logging`: https://cheatsheetseries.owasp.org/cheatsheets/Logging_Cheat_Sheet.html
- W3C, `Web Content Accessibility Guidelines 2.2`: https://www.w3.org/TR/WCAG22/

Las referencias son punto de partida. Cada feature debe enlazar documentación oficial de las APIs concretas que utilice y registrar la versión fijada en el repositorio.
