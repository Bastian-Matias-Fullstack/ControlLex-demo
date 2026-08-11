# Ejecución local de ControlLex

Esta guía describe el procedimiento validado para reconstruir y ejecutar ControlLex en un ambiente local de Development a partir del repositorio.

La ejecución local puede prepararse desde una base de datos nueva mediante EF Core migrations y un bootstrap explícito de datos demo, sin depender de una copia previa de la base de datos.

## Requisitos

- .NET SDK 8
- SQL Server
- Git
- PowerShell o terminal equivalente

## 1. Restaurar herramientas y dependencias

Desde la raíz del repositorio:

```powershell
dotnet tool restore
dotnet restore ".\SoftwareJuridicoEscalableRobusto.sln"
```

`dotnet-ef` está definido como herramienta local del repositorio mediante `dotnet-tools.json`.

## 2. Crear la configuración de Development

Copiar la plantilla:

```powershell
Copy-Item `
    ".\appsettings.Example.Development.json" `
    ".\appsettings.Development.json"
```

`appsettings.Development.json` está ignorado por Git.

Configurar la conexión a SQL Server según el ambiente local. Por ejemplo:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ControlLex_Dev;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

El nombre de la base de datos puede adaptarse al ambiente local del desarrollador.

## 3. Configurar secretos locales

Los valores sensibles de Development deben permanecer fuera del repositorio.

Configurar una clave JWT local:

```powershell
dotnet user-secrets set `
    "Jwt:Key" `
    "<CLAVE_LOCAL_SEGURA>" `
    --project ".\API.csproj"
```

Configurar la contraseña utilizada para crear los usuarios demo:

```powershell
dotnet user-secrets set `
    "DemoBootstrap:Password" `
    "<PASSWORD_DEMO_LOCAL>" `
    --project ".\API.csproj"
```

No reutilizar contraseñas personales ni secretos pertenecientes a otros ambientes.

## 4. Aplicar las migrations

```powershell
dotnet ef database update `
    --project ".\API.csproj" `
    --startup-project ".\API.csproj"
```

La base destino se obtiene desde:

```text
ConnectionStrings:DefaultConnection
```

Las migrations reconstruyen el esquema actual y los roles estáticos requeridos por la aplicación.

## 5. Inicializar los datos demo

Sobre una base recién migrada y sin datos operativos:

```powershell
dotnet run `
    --project ".\API.csproj" `
    -- `
    --seed-demo
```

El bootstrap inicializa actualmente:

- 10 clientes ficticios
- 3 usuarios demo
- 3 relaciones usuario/rol
- 15 casos del baseline demo

Usuarios demo:

| Usuario | Rol |
|---|---|
| admin@legal.cl | Admin |
| abogado@legal.cl | Abogado |
| soporte@legal.cl | Soporte |

Los usuarios utilizan la contraseña configurada mediante `DemoBootstrap:Password`.

El bootstrap está limitado a Development y requiere una base sin datos operativos. Si detecta datos existentes, aborta sin modificarlos.

## 6. Ejecutar ControlLex

Después de inicializar la base:

```powershell
dotnet run --project ".\API.csproj"
```

Con el perfil HTTP actual:

```text
http://localhost:5150
```

El arranque normal no ejecuta el bootstrap.

## 7. Verificar salud

Liveness:

```text
http://localhost:5150/health/live
```

Readiness de base de datos:

```text
http://localhost:5150/health/ready
```

En un ambiente correctamente configurado, ambos endpoints deben responder HTTP 200 con estado `Healthy`.

## Migrations, bootstrap y reset

Son responsabilidades diferentes:

### EF Core migrations

Reconstruyen y evolucionan el esquema de la base de datos.

### Demo bootstrap

Inicializa por primera vez una base Development/demo recién migrada con los datos necesarios para utilizar la aplicación.

### Demo reset

Restaura el baseline de una demo existente después de que haya sido utilizada o modificada.

El bootstrap no reemplaza al reset.

## Validación realizada

La reproducibilidad local fue comprobada sobre una base de datos nueva siguiendo esta secuencia:

```text
dotnet tool restore
        ↓
dotnet ef database update
        ↓
12 migrations aplicadas
        ↓
3 roles / 0 datos operativos
        ↓
dotnet run -- --seed-demo
        ↓
10 clientes
3 usuarios
3 relaciones usuario/rol
15 casos
        ↓
dotnet run
        ↓
health/live = Healthy
health/ready = Healthy
        ↓
autenticación válida
        ↓
dashboard y casos accesibles
```

También se comprobó que una segunda ejecución del bootstrap sobre una base ya poblada aborta sin modificar información.

Esta validación corresponde al ambiente local de Development. No constituye por sí sola una validación de despliegue productivo.