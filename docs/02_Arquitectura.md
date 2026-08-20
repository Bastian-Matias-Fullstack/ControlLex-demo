# Arquitectura

## Organización por capas

ControlLex aplica principios de Clean Architecture mediante cuatro proyectos con responsabilidades explícitas:

```text
API → Application → Domain
          ↑
  Infrastructure
```

| Capa | Responsabilidad |
| --- | --- |
| `Dominio` | Entidades, enums y reglas que no dependen de frameworks. |
| `Aplicacion` | Casos de uso, DTOs, validaciones y contratos de persistencia/servicios. |
| `Infraestructura` | EF Core, SQL Server, repositorios, hashing, JWT y adaptadores técnicos. |
| `API` | Controllers, configuración HTTP, middleware, autenticación, autorización y health checks. |

## Dirección de dependencias

`Dominio` no referencia EF Core ni ASP.NET Core. `Aplicacion` depende del dominio y define las abstracciones que necesita para ejecutar casos de uso. `Infraestructura` implementa esas abstracciones; `API` compone las dependencias y actúa como punto de entrada.

Este diseño mantiene el código de negocio separado de HTTP y persistencia, permite sustituir o simular repositorios en pruebas unitarias y reduce la lógica de los controllers. Las pruebas `DependencyBoundaryTests` comprueban que Application y Domain no adquieran dependencias prohibidas.

## Flujo representativo

1. Un controller recibe y valida un contrato HTTP.
2. El caso de uso de Application aplica reglas y utiliza interfaces de repositorio.
3. Infrastructure persiste mediante `AppDbContext` y EF Core.
4. La API traduce resultados y excepciones a `ProblemDetails` y códigos HTTP.

No se afirma un modelo DDD avanzado: la estructura se limita a las responsabilidades y dependencias que el repositorio implementa.
