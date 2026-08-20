# Ejecución local de ControlLex

Esta guía prepara una base de Development nueva mediante migrations EF Core y el bootstrap demo versionado. No requiere copiar una base existente.

## Requisitos

- .NET SDK 8.
- SQL Server accesible desde el equipo local.
- Git y PowerShell o una terminal equivalente.

## 1. Restaurar herramientas y dependencias

```powershell
dotnet tool restore
dotnet restore ".\SoftwareJuridicoEscalableRobusto.sln"
```

`dotnet-ef` está definido como herramienta local del repositorio.

## 2. Configurar Development

Copie la plantilla, que está diseñada para no contener secretos:

```powershell
Copy-Item ".\appsettings.Example.Development.json" ".\appsettings.Development.json"
```

`appsettings.Development.json` está ignorado por Git. Configure una base local dedicada, por ejemplo:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ControlLex_Dev;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

Configure los valores sensibles mediante user-secrets o variables de entorno. Por ejemplo:

```powershell
dotnet user-secrets set "Jwt:Key" "<CLAVE_LOCAL_SEGURA>" --project ".\API.csproj"
dotnet user-secrets set "DemoBootstrap:Password" "<PASSWORD_DEMO_LOCAL>" --project ".\API.csproj"
```

No use credenciales personales ni copie secretos de otros ambientes.

## 3. Aplicar migrations

```powershell
dotnet ef database update --project ".\API.csproj" --startup-project ".\API.csproj"
```

La cadena de migrations actual contiene 14 entradas, incluida la restricción de un caso activo por cliente y `Casos.Version` como `rowversion`.

## 4. Crear el baseline demo

Ejecute solo sobre una base recién migrada sin datos operativos:

```powershell
dotnet run --project ".\API.csproj" -- --seed-demo
```

El bootstrap está limitado a Development y aborta sin modificar datos si detecta información operativa. Crea 10 clientes ficticios, 3 usuarios demo protegidos, 3 asignaciones de rol y 15 casos (5 Pendiente, 5 EnProceso y 5 Cerrado).

## 5. Ejecutar y verificar

```powershell
dotnet run --project ".\API.csproj" --launch-profile http
```

El perfil HTTP versionado usa `http://localhost:5150`.

```text
http://localhost:5150/health/live
http://localhost:5150/health/ready
```

El endpoint `live` verifica que el proceso responde; `ready` incorpora el health check de `AppDbContext`.

## Migrations, bootstrap y reset

- **Migrations:** evolucionan el schema y los datos estáticos de roles.
- **Bootstrap demo:** inicializa una base nueva sin datos operativos.
- **Demo reset:** restaura el baseline de una demo existente según su flujo específico.

Son operaciones distintas. El bootstrap no es un reset ni debe usarse sobre datos de negocio.
