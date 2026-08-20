# Ejecución local con configuración Production

Esta guía permite observar el comportamiento de configuración Production en una máquina local. No equivale a validar un proveedor cloud ni sus controles perimetrales.

## Configuración requerida

No versionar secretos. Defina los valores para la sesión de terminal o mediante el proveedor de secretos apropiado para el ambiente:

```powershell
$env:ConnectionStrings__DefaultConnection = "<CONEXION_SQL_LOCAL>"
$env:Jwt__Key = "<CLAVE_SEGURA_DE_DESARROLLO_LOCAL>"
$env:ASPNETCORE_ENVIRONMENT = "Production"
```

La cadena debe apuntar a una base local que el operador haya preparado y autorizado. No use esta guía para modificar una base compartida.

## Ejecución

```powershell
dotnet run --project ".\API.csproj" --no-launch-profile
```

La URL se controla mediante `ASPNETCORE_URLS` o los valores por defecto del host si no se especifica.

## Comportamiento verificable

En Production, la aplicación:

- Emite headers de seguridad, incluida Content Security Policy.
- Usa HSTS cuando la request se observa como HTTPS.
- Aplica HTTPS redirection salvo en el flujo configurado para Render.
- Mantiene autenticación y autorización JWT en backend.
- Mantiene health checks en `/health/live` y `/health/ready`.

La configuración `Swagger:Enabled` controla la disponibilidad de Swagger; fuera de Development, las rutas Swagger se protegen con autenticación y rol Admin cuando están habilitadas.

No se incluyen credenciales demo en esta guía. Las credenciales son específicas del ambiente y deben administrarse fuera del repositorio.
