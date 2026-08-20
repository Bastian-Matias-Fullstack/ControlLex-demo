# ControlLex

ControlLex es una aplicación web de gestión de casos jurídicos construida como proyecto de portafolio técnico. Representa un escenario habitual de software de negocio: gestión de casos, clientes, usuarios y roles, con reglas de dominio, persistencia relacional y una API HTTP protegida.

Su objetivo es demostrar prácticas de desarrollo mantenible con .NET; no se presenta como un producto comercial ni como una operación cloud administrada.

## Estado

**V1 estable congelada** (`v1.0-production-readiness`). Incluye la funcionalidad principal, validaciones técnicas, pruebas automatizadas, artefacto Docker y documentación de operación local.

Fuera de alcance de esta V1:

- Arquitectura distribuida o multi-servicio.
- Operación comercial, SLA o alta disponibilidad.
- Infraestructura cloud avanzada y observabilidad administrada.

## Stack

- .NET 8 y ASP.NET Core.
- Entity Framework Core 8 y SQL Server.
- JWT Bearer, autorización por roles y FluentValidation.
- HTML, CSS, Bootstrap y JavaScript sin framework para el frontend servido por la aplicación.
- Docker.
- GitHub Actions.
- xUnit, Moq y pruebas de integración HTTP/SQL Server.

## Arquitectura

El repositorio aplica una separación pragmática de responsabilidades:

```text
API → Application → Domain
          ↑
  Infrastructure
```

- **API** recibe HTTP, aplica autenticación/autorización y traduce resultados a contratos HTTP.
- **Application** contiene casos de uso, validaciones y contratos de persistencia.
- **Domain** concentra entidades y reglas sin depender de EF Core ni ASP.NET Core.
- **Infrastructure** implementa persistencia EF Core, repositorios, hashing, JWT y servicios técnicos.

La dirección de dependencias evita que Application y Domain dependan de los detalles de HTTP o EF Core. Las pruebas de arquitectura verifican esas fronteras.

## Capacidades demostradas

- Gestión de casos, clientes, usuarios y roles con autorización backend.
- Estados de caso `Pendiente`, `EnProceso` y `Cerrado`; cierre mediante un flujo dedicado.
- Integridad de datos para impedir más de un caso activo por cliente.
- Concurrencia optimista mediante SQL Server `rowversion`.
- Migraciones EF Core y bootstrap explícito de datos demo para Development.
- Contratos de error HTTP basados en `ProblemDetails`.
- Prevención de Stored XSS en renderizado frontend y CSP en Production.
- Health checks de liveness y readiness de base de datos.
- Validación automatizada de restore, build, tests, publish y Docker build en GitHub Actions.

## Ejecución local

Los pasos completos, incluida una base Development nueva y el bootstrap demo, están en [docs/Ejecucion_Local_LegalApp.md](docs/Ejecucion_Local_LegalApp.md).

Resumen:

```powershell
dotnet tool restore
dotnet restore ".\SoftwareJuridicoEscalableRobusto.sln"
dotnet ef database update --project ".\API.csproj" --startup-project ".\API.csproj"
dotnet run --project ".\API.csproj" -- --seed-demo
dotnet run --project ".\API.csproj" --launch-profile http
```

La configuración Development y los secretos locales no se versionan. Configure `ConnectionStrings:DefaultConnection`, `Jwt:Key` y `DemoBootstrap:Password` mediante el mecanismo local descrito en la guía.

Con el perfil HTTP versionado, la aplicación escucha en `http://localhost:5150`. Los health checks están disponibles en `/health/live` y `/health/ready`.

## Calidad y despliegue reproducible

El baseline V1 validado registra **121 pruebas exitosas**. La suite incluye pruebas unitarias, de arquitectura, de contratos HTTP, de persistencia SQL Server y operacionales.

El workflow [ci.yml](.github/workflows/ci.yml) ejecuta, en Ubuntu:

```text
checkout → restore → build Release → test → publish → inspección del publish → docker build
```

El Dockerfile usa build multi-stage y ejecuta el contenedor con el usuario no-root expuesto por la imagen base. Consulte [docs/06_Seguridad.md](docs/06_Seguridad.md) y [docs/07_Production_Readiness.md](docs/07_Production_Readiness.md) para los límites y la evidencia de estas validaciones.

## Documentación

- [Contexto](docs/01_Contexto_General.md)
- [Arquitectura](docs/02_Arquitectura.md)
- [Reglas de negocio y roles](docs/03_Reglas_de_Negocio_y_Roles.md)
- [QA y pruebas](docs/04_QA_y_Pruebas.md)
- [Frontend](docs/05_Frontend.md)
- [Seguridad y despliegue](docs/06_Seguridad.md)
- [Production readiness](docs/07_Production_Readiness.md)
