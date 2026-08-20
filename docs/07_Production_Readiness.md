# Production readiness

## Baseline

- Tag: `v1.0-production-readiness`.
- Commit del baseline documentado: `31ba5fa55397b3e47a8029fbd172a361dbcb24fa`.
- Propósito: dejar evidencia técnica revisable de calidad, datos y artefacto de despliegue para la V1 de ControlLex.

El término *production readiness* en este documento se limita a las validaciones registradas a continuación. No implica disponibilidad garantizada, operación comercial ni infraestructura cloud administrada.

## Validaciones registradas

- Build Release exitoso.
- Suite de 121 pruebas automatizadas exitosa.
- Migrations EF Core ejecutadas desde una base SQL Server vacía hasta la última migration.
- Verificación de `__EFMigrationsHistory`, schema y tablas requeridas.
- Verificación del índice único filtrado `UX_Casos_ClienteId_Activo`.
- Verificación de `Casos.Version` como `rowversion` SQL Server, no nulo y de 8 bytes.
- Bootstrap demo en una base recién migrada: 10 clientes, 3 usuarios demo protegidos, 3 relaciones usuario/rol y 15 casos (5/5/5 por estado).
- Comprobación de cero clientes con más de un caso activo.
- Health checks `/health/live` y `/health/ready` con respuesta `Healthy` contra la base validada.
- Workflow CI con restore, build, test, publish, inspección del publish y Docker build.

La validación clean-room se realizó sin copiar datos de una base existente. Las migrations fueron la fuente de verdad del schema y el bootstrap fue el mecanismo versionado para el baseline demo.

## Incidente de consistencia EF Core en ambiente productivo

Durante la preparación final se detectó una desalineación entre el modelo EF Core desplegado y el historial de migrations aplicado en el ambiente productivo.

### Problema

La base registraba migrations solo hasta una versión anterior, mientras que el código desplegado requería objetos incorporados posteriormente.

### Diagnóstico

Antes de intervenir se revisaron:

- El historial `__EFMigrationsHistory`.
- Las tablas y columnas necesarias del schema.
- El índice filtrado de casos activos.
- La columna de concurrencia `Version`.

### Resolución

Se validó que los objetos físicos requeridos existían y se sincronizó el historial de EF Core con el estado efectivo del schema. No se recreó la base ni se eliminó información.

### Resultado

La aplicación volvió a operar con el modelo y el historial de migrations alineados. La prueba clean-room posterior confirmó que la cadena versionada puede crear el schema completo desde cero y que el bootstrap demo respeta la invariante de casos activos.

## Límites de la evidencia

- Las comprobaciones se realizaron contra ambientes y bases autorizadas para validación.
- No son una auditoría externa de seguridad ni una certificación.
- La configuración, secretos, red perimetral y operación del proveedor cloud requieren validación propia en cada despliegue.
