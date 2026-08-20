# Contexto general

## Propósito

ControlLex es un proyecto de portafolio orientado a demostrar prácticas de desarrollo de aplicaciones de negocio con .NET. El dominio representado es la gestión de casos jurídicos, clientes, usuarios y roles.

No pretende describir un servicio comercial ni afirmar operación productiva permanente. El valor del repositorio está en que las decisiones, reglas y validaciones pueden revisarse en código, migrations, pruebas y automatización de build.

## Alcance funcional

- Gestión de casos y clientes asociados.
- Gestión de usuarios y asignación de roles.
- Autenticación JWT y autorización backend para Admin, Abogado y Soporte.
- Listado de casos con filtros, ordenamiento y paginación.
- Dashboard y frontend estático servido desde `wwwroot`.

## Principios de implementación

- El backend es la autoridad para permisos y reglas; la visibilidad del frontend es solo UX.
- Los casos de uso separan las reglas de la capa HTTP y de EF Core.
- Los errores esperados se expresan como contratos HTTP consistentes.
- La configuración sensible se mantiene fuera de archivos versionados.

## Estado de V1

La V1 está congelada bajo el tag `v1.0-production-readiness`. Incluye la funcionalidad principal, validaciones de calidad y documentación técnica actualizada. Las capacidades no implementadas —por ejemplo arquitectura distribuida, operación comercial o infraestructura cloud avanzada— permanecen fuera de alcance.
