# Reglas de negocio y roles

## Roles

La autorización se aplica en backend con JWT y los roles `Admin`, `Abogado` y `Soporte`. El frontend ajusta la navegación para mejorar UX, pero no sustituye la autorización del servidor.

- **Admin:** administra casos, usuarios y roles según los endpoints autorizados.
- **Abogado:** opera los flujos de casos autorizados.
- **Soporte:** opera los flujos de usuarios autorizados.

## Ciclo de vida de un caso

Los estados persistidos son `Pendiente`, `EnProceso` y `Cerrado`.

- Un caso nuevo inicia en `Pendiente`.
- La actualización general no cambia el estado arbitrariamente.
- El cierre usa un flujo dedicado y un caso cerrado no puede cerrarse nuevamente.
- Los casos cerrados no admiten las operaciones de edición o eliminación que el dominio restringe.

Las violaciones de reglas de negocio se traducen a `409 Conflict`; recursos inexistentes a `404 Not Found` y contratos inválidos a `400 Bad Request`.

## Un caso activo por cliente

Un caso activo es aquel cuyo estado es distinto de `Cerrado`. Un cliente no puede tener más de uno.

La regla se protege en dos niveles:

1. **Application** valida el conflicto durante los casos de uso de creación y actualización.
2. **SQL Server** aplica el índice único filtrado `UX_Casos_ClienteId_Activo` sobre `Casos.ClienteId`, con filtro `Estado <> 'Cerrado'`.

La duplicación protege la integridad cuando hay operaciones concurrentes o rutas de acceso distintas a la aplicación.

## Concurrencia optimista

`Casos.Version` usa `rowversion` de SQL Server. La API entrega el token como Base64 y exige una versión esperada en las operaciones que modifican o cierran un caso. Si otra escritura ya modificó el registro, el conflicto se comunica como `409`, evitando sobrescrituras silenciosas.
