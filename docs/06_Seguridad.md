# Seguridad y despliegue

## Alcance

ControlLex implementa controles concretos en aplicación, frontend y artefacto de despliegue. Esta documentación describe esos controles; no afirma certificación, seguridad completa ni garantías operacionales.

## Frontend y Stored XSS

El renderizado de datos controlables por usuario o provenientes de API evita insertarlos en sinks que interpreten HTML o JavaScript. Las rutas corregidas usan APIs DOM como `textContent`, `dataset` y `addEventListener`; no construyen handlers inline con datos dinámicos.

El markup constante puede seguir usando plantillas controladas. Esta distinción evita que una cadena persistida se ejecute como HTML al volver a mostrarse.

## Headers y CSP

En Production, el middleware de la API emite una Content Security Policy que limita el origen por defecto a `self`, impide objetos y frames, restringe scripts a `self` y `cdn.jsdelivr.net`, y define fuentes explícitas para estilos, imágenes y fuentes.

También añade los headers implementados:

- `X-Content-Type-Options: nosniff`
- `Referrer-Policy: no-referrer`
- `Permissions-Policy`
- `Cross-Origin-Opener-Policy: same-origin`
- `Cross-Origin-Resource-Policy: same-origin`
- `X-Frame-Options: DENY`

`UseHsts()` se activa en Production y HSTS se envía cuando ASP.NET Core observa una request HTTPS. `UseHttpsRedirection()` se mantiene excepto en el flujo Render configurado por la aplicación para evitar redirecciones incorrectas detrás del proxy.

## Autenticación, autorización y validación

- La API usa JWT Bearer y autorización por roles.
- Los controllers y casos de uso validan contratos y reglas en backend; la UI no concede permisos.
- FluentValidation valida contratos de Application.
- El middleware de errores normaliza errores esperados como `ProblemDetails`.
- El rate limiter definido en la API limita rutas de login y escritura por IP efectiva.

El frontend actual almacena JWT en `localStorage`. Es una decisión existente del proyecto y una limitación residual: CSP y renderizado seguro reducen vectores XSS, pero no convierten el navegador en una frontera de secretos.

## Configuración sensible

Los ejemplos versionados no contienen claves JWT, passwords ni cadenas de producción. `appsettings.*.json` de ambiente se excluye del publish y `.dockerignore` excluye archivos `.env`, `*.secret.json`, launch settings y configuración de usuario.

Para Development, la guía local usa `appsettings.Development.json` ignorado por Git y user-secrets o variables de entorno para `Jwt:Key` y `DemoBootstrap:Password`.

## Docker y CI

El Dockerfile realiza restore y publish en una etapa SDK y copia únicamente el resultado publish a una imagen runtime ASP.NET Core. El contenedor ejecuta con `USER $APP_UID`, el usuario no-root proporcionado por la imagen base.

GitHub Actions ejecuta en Ubuntu el siguiente flujo:

```text
checkout → setup-dotnet → restore → build Release → test → publish → inspección del artefacto → docker build
```

La inspección de publish exige `API.dll`, `API.runtimeconfig.json` y `wwwroot`, y falla si detecta configuración específica de ambiente o archivos secretos definidos en el workflow. Es una validación automatizada del repositorio; no reemplaza un proceso de despliegue ni una revisión de secretos del proveedor.
