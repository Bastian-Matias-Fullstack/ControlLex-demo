# QA y pruebas

## Suite automatizada

El baseline V1 validado contiene **121 pruebas exitosas**. No se declara cobertura total: la suite cubre comportamientos y fronteras relevantes, no una métrica porcentual de cobertura.

| Tipo | Evidencia en el repositorio |
| --- | --- |
| Unitarias de dominio | Reglas de `Caso` y `Usuario`. |
| Unitarias de Application | Creación, actualización, cierre, eliminación, listado, usuarios, roles y tokens de versión. |
| Arquitectura | `DependencyBoundaryTests` verifica que Domain/Application no dependan de EF Core o ASP.NET Core. |
| Integración HTTP | `HttpContractTests` verifica contratos de error y respuestas HTTP. |
| Persistencia SQL Server | `CasoSqlServerIntegrityTests` cubre restricciones de integridad de casos. |
| Operacionales | `ClientIpResolverTests` cubre resolución de IP para el comportamiento de proxy implementado. |

## Contratos HTTP

Los errores esperados se normalizan como `ProblemDetails`. La suite de integración cubre respuestas 400, 401, 403, 404, 409, 429, 500 y 503 en las rutas que las producen.

## Ejecución

```powershell
dotnet restore ".\SoftwareJuridicoEscalableRobusto.sln"
dotnet build ".\SoftwareJuridicoEscalableRobusto.sln" -c Release --no-restore
dotnet test ".\SoftwareJuridicoEscalableRobusto.sln" -c Release --no-build
```

Las pruebas SQL Server ejercen una conexión indicada mediante `CONTROLLEX_SQL_TEST_CONNECTION`; sin esa variable, sus escenarios se omiten sin apuntar a una base arbitraria. La validación clean-room de migrations y bootstrap se describe en [07_Production_Readiness.md](07_Production_Readiness.md).

## Criterio de calidad

El repositorio busca que los flujos esperados devuelvan contratos predecibles y que las reglas de dominio e integridad de datos no dependan únicamente del frontend. Las pruebas no sustituyen la revisión manual de UX ni validan por sí solas una operación productiva.
