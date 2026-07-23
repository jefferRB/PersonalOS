# PersonalOS — App Flow

**Versión:** 1.0  
**Estado:** Proposed  
**Última actualización:** 2026-07-23

## Ciclo general

```mermaid
flowchart LR
    A[Capturar] --> B[Planificar]
    B --> C[Ejecutar]
    C --> D[Registrar]
    D --> E[Revisar]
    E --> F[Ajustar]
    F --> B
```

## Autenticación

```mermaid
flowchart TD
    A[Abrir app] --> B{GET /api/auth/me}
    B -->|Usuario| C[Dashboard]
    B -->|401| D[Login]
    D --> E[Obtener antiforgery]
    E --> F[Enviar credenciales]
    F -->|Éxito| G[Invalidar auth cache]
    G --> B
    F -->|Error| D
    C --> H[Logout]
    H --> I[POST con antiforgery]
    I --> J[Eliminar sesión]
    J --> D
```

## Registro

```mermaid
flowchart TD
    A[Formulario] --> B[Validación local]
    B -->|Inválido| A
    B -->|Válido| C[Token antiforgery]
    C --> D[POST register]
    D -->|Éxito| E[Sesión o login]
    D -->|409/422| F[Error comprensible]
    D -->|429| G[Indicar espera]
```

## Ciclo diario futuro

```mermaid
flowchart TD
    A[Inicio] --> B[Agenda]
    B --> C[Tres prioridades]
    C --> D[Hábitos]
    D --> E[Intención]
    E --> F[Ejecución]
    F --> G[Capturas]
    G --> H[Cierre]
    H --> I[Resumen]
    I --> J[Preparar mañana]
```

## Tarea futura

```mermaid
flowchart TD
    A[Capturar] --> B{¿Fecha?}
    B -->|Sí| C[Fecha local]
    B -->|No| D[Bandeja]
    C --> E[Guardar]
    D --> E
    E --> F[Hoy o Planificar]
    F --> G{Completar}
    G -->|Sí| H[Instante UTC]
    G -->|No| I[Reprogramar]
```

## Hábito futuro

```mermaid
flowchart TD
    A[Configurar] --> B[Tipo y frecuencia]
    B --> C[Expectativa por fecha local]
    C --> D{Registro}
    D -->|Cumplido| E[Valor]
    D -->|Descanso| F[Descanso]
    D -->|No| G[Motivo]
    E --> H[Consistencia]
    F --> H
    G --> H
```

## Nutrición futura

```mermaid
flowchart TD
    A[Comida] --> B[Alimento]
    B --> C[Porción]
    C --> D[Nutrientes]
    D --> E[Guardar fecha local]
    E --> F[Objetivo vigente]
    F --> G[Resumen]
```

## Cierre futuro

```mermaid
flowchart TD
    A[Revisar plan] --> B[Lo mejor]
    B --> C[Lo difícil]
    C --> D[Causa]
    D --> E[Aprendizaje]
    E --> F[Ajuste]
    F --> G[Guardar]
    G --> H[Resumen]
```

## Revisión semanal

```mermaid
flowchart TD
    A[Semana] --> B[Tareas]
    A --> C[Hábitos]
    A --> D[Nutrición]
    A --> E[Reflexiones]
    B --> F[Patrones]
    C --> F
    D --> F
    E --> F
    F --> G[Experimento]
    G --> H[Próxima semana]
```

## Errores

```mermaid
flowchart LR
    A[Request] --> B{Resultado}
    B -->|401| C[Login]
    B -->|403| D[Denegado]
    B -->|409| E[Conflicto]
    B -->|422| F[Validación]
    B -->|429| G[Espera]
    B -->|5xx| H[Mensaje + trace ID]
    B -->|Offline| I[Conservar si es seguro]
```

## Regla

Cada feature debe documentar entrada, decisiones, errores, salida, datos y autorización antes de implementación.
