# Frontend

## Implementación

El frontend se sirve desde `wwwroot` y utiliza HTML, CSS, Bootstrap y JavaScript sin framework. Consume la API mediante `fetch` y adjunta el JWT para las rutas protegidas.

Los módulos visibles se ajustan al rol efectivo para mejorar la navegación. Esa visibilidad no concede permisos: la autorización se mantiene en backend.

## Comportamiento de casos

La interfaz permite listar, ver, crear, editar, cerrar y eliminar casos de acuerdo con las capacidades expuestas por la API. También presenta filtros, ordenamiento, paginación y métricas de dashboard.

Los estados se representan como `Pendiente`, `EnProceso` y `Cerrado`. La métrica de casos activos usa la definición de dominio: `Estado != Cerrado`.

## Renderizado y seguridad

Los valores provenientes de API, persistencia o usuario se insertan mediante APIs DOM seguras como `textContent`, `dataset` y listeners registrados con `addEventListener`. El markup constante y controlado puede permanecer como plantilla; los datos no confiables no se interpolan en sinks que interpreten HTML o JavaScript.

Esta medida evita las rutas Stored XSS identificadas durante la revisión del frontend. La política CSP de Production aporta una defensa adicional, pero no reemplaza el renderizado seguro.

## Límites

El frontend no es una frontera de autorización ni un mecanismo de protección de secretos. La sesión actual utiliza JWT almacenado en el navegador y las reglas de negocio, permisos y validaciones decisivas se ejecutan en el backend.
