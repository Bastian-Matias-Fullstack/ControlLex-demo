function configurarSidebarPorRoles(roles) {
    roles = Array.isArray(roles) ? roles : [];

    const isAdmin = roles.includes("Admin");
    const isAbogado = roles.includes("Abogado");
    const isSoporte = roles.includes("Soporte");

    // Admin ve todo
    if (isAdmin) return;

    // Dashboard nav: solo Admin o Abogado
    if (!isAbogado) {
        document.getElementById("nav-dashboard")?.remove();
    }

    // Roles nav: solo Admin
    document.getElementById("nav-roles")?.remove();

    // Usuarios nav: Admin o Soporte
    if (!isSoporte) {
        document.getElementById("nav-usuarios")?.remove();
    }

    // Casos nav: Admin o Abogado
    if (!isAbogado) {
        document.getElementById("nav-casos")?.remove();
    }
}

function CanAccess(moduloId, roles) {
    roles = Array.isArray(roles) ? roles : [];

    const isAdmin = roles.includes("Admin");
    const isAbogado = roles.includes("Abogado");
    const isSoporte = roles.includes("Soporte");

    // Admin ve todo
    if (isAdmin) return true;

    // Dashboard: Admin o Abogado
    if (moduloId === "mod-dashboard") return isAbogado;

    // Casos: Admin o Abogado
    if (moduloId === "mod-casos") return isAbogado;

    // Usuarios: Admin o Soporte
    if (moduloId === "mod-usuarios") return isSoporte;

    // Roles: solo Admin
    if (moduloId === "mod-roles") return false;

    // Por defecto, denegar
    return false;
}
function clearSessionAndRedirect(reason = "session-ended") {
    console.warn("Sesión finalizada:", reason);

    localStorage.removeItem("jwt_token");
    localStorage.removeItem("usuario_actual");
    sessionStorage.removeItem("demoContext");

    history.replaceState(null, "", "dashboard.html");
    window.location.replace("login.html");
}
function obtenerRolesDesdeJWT() {
        const token = localStorage.getItem("jwt_token");
        if (!token) return [];
        try {
            const payloadBase64 = token.split('.')[1];
            const payloadJson = atob(payloadBase64);
            const payload = JSON.parse(payloadJson);
            const roles = payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
          return Array.isArray(roles) ? roles.filter(Boolean) : (roles ? [roles] : []);
        } catch (e) {
            console.error("Error al decodificar el token:", e);
            return [];
        }
    }
            function obtenerRolEfectivo(roles) {
        if (roles.includes("Admin")) return "Admin";
        if (roles.includes("Abogado")) return "Abogado";
        if (roles.includes("Soporte")) return "Soporte";
        return null;
}
function normalizarRoles(roles) {
    if (!Array.isArray(roles)) return [];
    return roles.filter(Boolean);
}

function tieneRolValido(roles) {
    roles = normalizarRoles(roles);
    return roles.includes("Admin") || roles.includes("Abogado") || roles.includes("Soporte");
}

    function obtenerModuloDefaultPorRol(roles) {
        roles = normalizarRoles(roles);

        if (roles.includes("Admin")) return "mod-dashboard";
        if (roles.includes("Abogado")) return "mod-dashboard";
        if (roles.includes("Soporte")) return "mod-usuarios";

        return null;
    }

function resolverModuloInicial(roles, moduloSolicitado) {
    roles = normalizarRoles(roles);

    if (!tieneRolValido(roles)) {
        return {
            allowed: false,
            reason: "no-role",
            target: null
        };
    }

    const modulosValidos = ["mod-dashboard", "mod-casos", "mod-roles", "mod-usuarios"];

    if (moduloSolicitado && modulosValidos.includes(moduloSolicitado) && CanAccess(moduloSolicitado, roles)) {
        return {
            allowed: true,
            reason: "requested-allowed",
            target: moduloSolicitado
        };
    }

    const fallback = obtenerModuloDefaultPorRol(roles);

    if (!fallback) {
        return {
            allowed: false,
            reason: "no-available-module",
            target: null
        };
    }

    if (moduloSolicitado && modulosValidos.includes(moduloSolicitado) && !CanAccess(moduloSolicitado, roles)) {
        return {
            allowed: true,
            reason: "requested-denied-fallback",
            target: fallback
        };
    }

    return {
        allowed: true,
        reason: "default",
        target: fallback
    };
}
        function aplicarVisibilidadPorRol(roles) {
            roles = Array.isArray(roles) ? roles : [];

            const gestionRoles = document.getElementById("seccion-gestion-roles");
            const btnNuevoCaso = document.getElementById("btnNuevoCaso");
            const tablaCasos = document.getElementById("tablaCasosWrapper");
            const seccionUsuarios = document.getElementById("seccion-usuarios");
            const btnNuevoUsuario = document.getElementById("btnNuevoUsuario");

            const isAdmin = roles.includes("Admin");
            const isAbogado = roles.includes("Abogado");
            const isSoporte = roles.includes("Soporte");

            // Reset
            [gestionRoles, btnNuevoCaso, tablaCasos, seccionUsuarios, btnNuevoUsuario]
                .forEach(el => el?.classList.add("d-none"));

            // Admin ve todo
            if (isAdmin) {
                gestionRoles?.classList.remove("d-none");
                btnNuevoCaso?.classList.remove("d-none");
                tablaCasos?.classList.remove("d-none");
                seccionUsuarios?.classList.remove("d-none");
                btnNuevoUsuario?.classList.remove("d-none");
                return;
            }

            // Multi-rol: unión de permisos
            if (isAbogado) {
                btnNuevoCaso?.classList.remove("d-none");
                tablaCasos?.classList.remove("d-none");
            }

            if (isSoporte) {
                seccionUsuarios?.classList.remove("d-none");
                btnNuevoUsuario?.classList.remove("d-none");
            }
    }
    function navigate(moduloId) {
    const roles = obtenerRolesDesdeJWT();

        if (!CanAccess(moduloId, roles)) {
            const fallback = obtenerModuloDefaultPorRol(roles) || "mod-dashboard";

            Swal?.fire?.({
                icon: "warning",
                title: "Acceso denegado",
                text: "No tienes permisos para acceder a este módulo."
            });

            // Mostrar fallback
            mostrarModulo(fallback);
            history.replaceState(null, "", `#${fallback}`);

            // Marcar activo el item del menú del fallback
            actualizarNavegacionActiva(fallback);

            //  Ejecutar lógica de carga del módulo fallback
            onModuloCargado(fallback);

            //Reaplicar visibilidad por roles
            aplicarVisibilidadPorRol(roles);

            return;
        }

        mostrarModulo(moduloId);
        history.replaceState(null, "", `#${moduloId}`);
  
    // 2️ Estado activo del menú
    actualizarNavegacionActiva(moduloId);
    // AQUÍ SE ENGANCHA EL PASO 1
    onModuloCargado(moduloId);
        aplicarVisibilidadPorRol(roles);
        if (isMobileViewport()) {
            closeSidebarMobile();
        }
}

function actualizarNavegacionActiva(moduloId) {
    const navId = "nav-" + moduloId.replace("mod-", "");

    document.querySelectorAll(".sidebar-nav-button").forEach(button => {
        const isActive = button.id === navId;
        button.classList.toggle("active", isActive);

        if (isActive) {
            button.setAttribute("aria-current", "page");
        } else {
            button.removeAttribute("aria-current");
        }
    });
}
// Demo Context 
const demoContext = sessionStorage.getItem("demoContext"); 
function applyDemoContextVisibility(context) {
    const casos = document.getElementById("tablaCasosWrapper");
    const roles = document.getElementById("seccion-gestion-roles");
    const usuarios = document.getElementById("seccion-usuarios");
    // Ocultamos todo primero
    [casos, roles, usuarios].forEach(el => el?.classList.add("d-none"));

    switch (context) {
        case "casos":
            casos?.classList.remove("d-none");
            break;

        case "roles":
            roles?.classList.remove("d-none");
            break;

        case "usuarios":
            usuarios?.classList.remove("d-none");
            break;

        default:
            // sin demoContext → comportamiento normal
            [casos, roles, usuarios].forEach(el => el?.classList.remove("d-none"));
    }
}

function cargarModuloOpcional(src, inicializador, nombreModulo) {
    const script = document.createElement("script");
    script.src = src;
    script.onload = () => {
        if (typeof window[inicializador] === "function") {
            window[inicializador]();
        } else {
            console.warn(`⚠️ ${inicializador} no está definido.`);
        }
    };
    script.onerror = () => console.error(`❌ Error al cargar ${nombreModulo}`);
    document.body.appendChild(script);
}

function cargarModulosPorRol(roles) {
    if (roles.includes("Admin")) {
        cargarModuloOpcional("/js/roles.js?v=roles-action-lock-1", "initRolesModule", "roles.js");
    }

    if (roles.includes("Admin") || roles.includes("Soporte")) {
        cargarModuloOpcional("/js/Usuarios.js?v=usuarios-action-lock-1", "initUsuariosModule", "usuarios.js");
    }
}

document.addEventListener("DOMContentLoaded", () => {

    if (window.matchMedia?.("(prefers-reduced-motion: reduce)").matches) {
        document.getElementById("bg-video-dashboard")?.pause();
    }

    document.querySelector(".sidebar-toggle")?.addEventListener("click", toggleSidebar);
    document.getElementById("nav-dashboard")?.addEventListener("click", () => navigate("mod-dashboard"));
    document.getElementById("nav-casos")?.addEventListener("click", () => navigate("mod-casos"));
    document.getElementById("nav-usuarios")?.addEventListener("click", () => navigate("mod-usuarios"));
    document.getElementById("nav-roles")?.addEventListener("click", () => navigate("mod-roles"));
    document.getElementById("sidebarOverlay")?.addEventListener("click", closeSidebarMobile);

    const token = localStorage.getItem("jwt_token");
    if (!token) {
        clearSessionAndRedirect("missing-token");
        return;
    }


    const roles = normalizarRoles(obtenerRolesDesdeJWT());

    if (!tieneRolValido(roles)) {
        Swal?.fire?.({
            icon: "warning",
            title: "Acceso no disponible",
            text: "Tu cuenta no tiene un rol asignado. No es posible ingresar a esta demo."
        }).then(() => {
            clearSessionAndRedirect("no-valid-role");
        });

        return;
    }

    configurarSidebarPorRoles(roles);
    aplicarVisibilidadPorRol(roles);
    cargarModulosPorRol(roles);

    const hashModulo = (window.location.hash || "").replace("#", "").trim();
    const moduloSolicitado = hashModulo || null;

    const decision = resolverModuloInicial(roles, moduloSolicitado);

    if (!decision.allowed || !decision.target) {
        clearSessionAndRedirect("invalid-initial-module-decision");
        return;
    }
    document.body.classList.remove("auth-pending");
    document.body.classList.add("auth-ready");

    if (decision.reason === "requested-denied-fallback") {
        Swal?.fire?.({
            icon: "info",
            title: "Redirección segura",
            text: "Tu rol no tiene acceso al módulo solicitado. Te redirigimos a tu módulo disponible."
        });
    }

    navigate(decision.target);

    const apiUrl = "api/Casos";
    
// =========================================================
// CAPA DEFENSIVA CASOS - ANTI CLIC / ANTI REQUEST PARALELA
// Objetivo: no cambiar flujos; solo evitar cargas duplicadas
// cuando Render/Azure SQL estén lentos o el usuario haga clics rápidos.
// =========================================================
let cargarCasosController = null;
let cargarCasosSecuencia = 0;
let detalleCasoCargando = false;
let detalleCasoIdCargando = null;
let abriendoNuevoCaso = false;
let guardandoCaso = false;

const accionesCasoEnCurso = new Set();

function iniciarAccionCaso(clave) {
    if (accionesCasoEnCurso.has(clave)) return false;
    accionesCasoEnCurso.add(clave);
    return true;
}

function finalizarAccionCaso(clave) {
    accionesCasoEnCurso.delete(clave);
}

function limpiarBackdropsHuerfanos() {
    const hayModalAbierto = document.querySelector(".modal.show");
    if (hayModalAbierto) return;

    document.querySelectorAll(".modal-backdrop").forEach(backdrop => backdrop.remove());
    document.body.classList.remove("modal-open");
    document.body.style.removeProperty("overflow");
    document.body.style.removeProperty("padding-right");
}

function setBotonCargando(btn, cargando, htmlCargando = null) {
    if (!btn) return null;

    if (cargando) {
        const htmlOriginal = btn.innerHTML;
        btn.disabled = true;
        btn.classList.add("disabled");
        btn.setAttribute("aria-busy", "true");
        if (htmlCargando) btn.innerHTML = htmlCargando;
        return htmlOriginal;
    }

    btn.disabled = false;
    btn.classList.remove("disabled");
    btn.removeAttribute("aria-busy");
    return null;
}

    /* Define la URL base para la API de casos.*/
    /*Recupera el token JWT desde localStorage (para autorizar las peticiones).*/
    let usuario = JSON.parse(localStorage.getItem("usuario_actual"));
    if (!usuario) {
        usuario = {};
    }

    const saludoDesktop = document.getElementById("saludoUsuario");
    const saludoMobile = document.getElementById("saludoUsuarioMobile");

    function renderSaludoUsuario(usuario) {
        const nombreCompleto = (usuario?.nombre || "").trim();
        const primerNombre = nombreCompleto.split(" ")[0] || "";

        const textoDesktop = nombreCompleto ? `Hola, ${nombreCompleto}` : "Hola";
        const textoMobile = primerNombre ? `Hola, ${primerNombre}` : "Hola";

        if (saludoDesktop) {
            saludoDesktop.textContent = textoDesktop;
            saludoDesktop.setAttribute("title", textoDesktop);
        }

        if (saludoMobile) {
            saludoMobile.textContent = textoMobile;
            saludoMobile.setAttribute("title", textoMobile);
        }
    }

    console.log("Rol del usuario:", roles);

    // Siempre aplicar reglas de rol (base de la app)
    // Solo si existe demoContext, se aplica encima (modo demo)

    // Aplicamos Choise.Js
    const filtroEstado = document.getElementById("filtroEstado");
    const choicesEstado = new Choices(filtroEstado, {
        searchEnabled: false,
        itemSelectText: '',
        shouldSort: false
    });

    const filtros = {
        estado: null,
        pagina: 1,
        tamanio: 10
    };
    renderSaludoUsuario(usuario);

    window.addEventListener("resize", () => {
        renderSaludoUsuario(usuario);
    });
    recargarCasos = cargarCasosDesdeBackend;
    cargarCasosDesdeBackend();
    initPdfExport();

// PAGINACIÓN: listener único 
const paginacion = document.getElementById("paginacion");

paginacion?.addEventListener("click", (e) => {
    const link = e.target.closest(".page-link");
    if (!link) return;

    e.preventDefault();

    const nuevaPagina = parseInt(link.dataset.page, 10);
    if (!Number.isInteger(nuevaPagina)) return;

    if (nuevaPagina !== filtros.pagina) {
        filtros.pagina = nuevaPagina;
        cargarCasosDesdeBackend();
    }
});

    /* Ejecuta carga inicial.*/
    filtroEstado.addEventListener("change", () => {
        const estadoSeleccionado = filtroEstado.value?.trim();
        filtros.estado = estadoSeleccionado || null;
        filtros.pagina = 1;
        let casosFiltrados = [];

        cargarCasosDesdeBackend();
    });
    /* Actualiza el filtro de estado y recarga la tabla */
    function construirQueryString(filtros) {
        const params = new URLSearchParams();
        if (filtros.estado) params.append("estado", filtros.estado);
        if (filtros.pagina) params.append("pagina", filtros.pagina);
        if (filtros.tamanio) params.append("tamanio", filtros.tamanio);
        return "?" + params.toString();
    }
    /*Transforma tu objeto filtros en un query string para el fetch.*/
async function cargarCasosDesdeBackend() {
    const secuenciaActual = ++cargarCasosSecuencia;

    if (cargarCasosController) {
        cargarCasosController.abort();
    }

    const controller = new AbortController();
    cargarCasosController = controller;

    try {
        const query = construirQueryString(filtros);

        const response = await fetch(`${apiUrl}${query}`, {
            cache: "no-store",
            signal: controller.signal,
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });

        if (secuenciaActual !== cargarCasosSecuencia) return;

        if (!response.ok) {
            if (response.status === 401) {
                clearSessionAndRedirect("expired-session-401");
                return;
            }

            if (response.status === 429) {
                console.warn("Demasiadas solicitudes al cargar casos.");
                document.getElementById("contadorResultados").textContent =
                    "Demasiadas solicitudes. Espera unos segundos e intenta nuevamente.";
                return;
            }

            console.error("Error al obtener los casos:", response.status);
            document.getElementById("contadorResultados").textContent =
                "No se pudieron cargar los casos. Intenta nuevamente.";
            return;
        }

        const data = await response.json();

        if (secuenciaActual !== cargarCasosSecuencia) return;

        window.casosGlobal = data.items || [];

        // Validación mínima para evitar errores si backend falla
        if (!data.items || !data.resumen) {
            console.warn("⚠️ La respuesta del backend no tiene el formato esperado:", data);
            document.getElementById("contadorResultados").textContent =
                "⚠️ No se pudieron cargar los datos correctamente.";
            return;
        }

        renderizarTabla(data.items);
        renderizarCardsCasos(data.items);
        actualizarResumen(data.resumen);
        mostrarMensajeInformativo(data.items.length, data.totalRegistros);
        renderizarPaginacion(data.pagina, data.totalPaginas);

    } catch (error) {
        if (error?.name === "AbortError") return;

        console.error("Error al cargar casos:", error);
        document.getElementById("contadorResultados").textContent =
            "No se pudo conectar con el servidor. Si la demo estaba en reposo, espera unos segundos e intenta nuevamente.";

    } finally {
        if (secuenciaActual === cargarCasosSecuencia && cargarCasosController === controller) {
            cargarCasosController = null;
        }
    }
}

    function renderizarTabla(lista) {
        const tbody = document.getElementById("casosBody");
        tbody.classList.remove("opacity-100");
        tbody.classList.add("opacity-0");
        tbody.innerHTML = "";

        lista.forEach(caso => {
            const estado = normalizarEstado(caso.estado);
            const tr = document.createElement("tr");
            tr.classList.add(`tr-${["pendiente", "enproceso", "cerrado"].includes(estado) ? estado : "desconocido"}`);

            tr.append(
                crearCeldaCaso(caso.id),
                crearCeldaCaso(caso.titulo)
            );

            const estadoCell = crearCeldaCaso();
            estadoCell.appendChild(crearEstadoBadge(caso.estado));
            tr.appendChild(estadoCell);

            const tipoCell = crearCeldaCaso();
            tipoCell.append(crearIconoTipo(caso.tipoCaso), textoSeguro(caso.tipoCaso));
            tr.append(
                tipoCell,
                crearCeldaCaso(caso.nombreCliente || "No Client"),
                crearCeldaCaso(new Date(caso.fechaCreacion).toLocaleDateString())
            );

            const acciones = crearCeldaCaso(null, "text-nowrap");
            acciones.appendChild(crearBotonCaso("btn btn-sm btn-outline-light me-1 btn-ver", caso.id, caso.estado, caso.version, "Ver", "bi-eye-fill"));

            if (estado !== "cerrado") {
                acciones.append(
                    crearBotonCaso("btn btn-sm btn-outline-warning btn-editar-caso", caso.id, caso.estado, caso.version, "Editar", "bi-pencil-fill"),
                    crearBotonCaso("btn btn-sm btn-outline-danger btn-eliminar", caso.id, caso.estado, caso.version, "Eliminar", "bi-trash-fill"),
                    crearBotonCaso("btn btn-sm btn-outline-secondary btn-cerrar", caso.id, caso.estado, caso.version, "Cerrar", "bi-lock-fill")
                );
            }

            tr.appendChild(acciones);
            tbody.appendChild(tr);
        });

        /** Limpia y vuelve a renderizar la tabla de casos con animación suave (opacity).
 Inserta HTML dinámico fila por fila */

        setTimeout(() => {
            tbody.classList.remove("opacity-0");
            tbody.classList.add("opacity-100");
        }, 50);
    }
    function renderizarCardsCasos(lista) {
        const cardsContainer = document.getElementById("casosCards");
        if (!cardsContainer) return;

        cardsContainer.innerHTML = "";

        lista.forEach(caso => {
            const estado = normalizarEstado(caso.estado);
            const card = document.createElement("article");
            card.className = "caso-card";
            card.dataset.id = textoSeguro(caso.id);
            card.dataset.estado = textoSeguro(caso.estado);

            const header = document.createElement("div");
            header.className = "caso-card-header";
            const titleWrap = document.createElement("div");
            titleWrap.className = "caso-card-title-wrap";
            const id = document.createElement("span");
            id.className = "caso-card-id";
            id.textContent = `#${textoSeguro(caso.id)}`;
            const titulo = document.createElement("h6");
            titulo.className = "caso-card-title mb-1";
            titulo.textContent = textoSeguro(caso.titulo);
            titleWrap.append(id, titulo);

            const badgeWrap = document.createElement("div");
            badgeWrap.className = "caso-card-badge";
            badgeWrap.appendChild(crearEstadoBadge(caso.estado));
            header.append(titleWrap, badgeWrap);

            const body = document.createElement("div");
            body.className = "caso-card-body";
            const tipoValue = document.createElement("span");
            tipoValue.className = "caso-card-value";
            tipoValue.append(crearIconoTipo(caso.tipoCaso), textoSeguro(caso.tipoCaso));
            body.append(
                crearFilaCardCaso("Tipo", tipoValue),
                crearFilaCardCaso("Cliente", caso.nombreCliente || "No Client"),
                crearFilaCardCaso("Creación", new Date(caso.fechaCreacion).toLocaleDateString())
            );

            const acciones = document.createElement("div");
            acciones.className = "caso-card-actions";
            acciones.appendChild(crearBotonCaso("btn btn-sm btn-outline-light me-1 btn-ver", caso.id, caso.estado, caso.version, "Ver", "bi-eye-fill"));

            if (estado !== "cerrado") {
                acciones.append(
                    crearBotonCaso("btn btn-sm btn-outline-warning btn-editar-caso", caso.id, caso.estado, caso.version, "Editar", "bi-pencil-fill"),
                    crearBotonCaso("btn btn-sm btn-outline-danger btn-eliminar", caso.id, caso.estado, caso.version, "Eliminar", "bi-trash-fill"),
                    crearBotonCaso("btn btn-sm btn-outline-secondary btn-cerrar", caso.id, caso.estado, caso.version, "Cerrar", "bi-lock-fill")
                );
            }

            card.append(header, body, acciones);
            cardsContainer.appendChild(card);
        });
    }
    function renderizarPaginacion(paginaActual, totalPaginas) {
        const paginacion = document.getElementById("paginacion");
        paginacion.innerHTML = "";

        if (totalPaginas <= 1) return;

        const crearItem = (label, page, disabled = false, active = false) => {
            const li = document.createElement("li");
            li.className = `page-item ${disabled ? "disabled" : ""} ${active ? "active" : ""}`;
            const link = document.createElement("a");
            link.className = "page-link";
            link.href = "#";
            link.dataset.page = textoSeguro(page);
            link.textContent = textoSeguro(label);
            li.appendChild(link);
            return li;
        };

        paginacion.appendChild(crearItem("Anterior", paginaActual - 1, paginaActual === 1));

        for (let i = 1; i <= totalPaginas; i++) {
            paginacion.appendChild(crearItem(i, i, false, i === paginaActual));
        }
        paginacion.appendChild(crearItem("Siguiente", paginaActual + 1, paginaActual === totalPaginas));
    }
    window.legalAppMetricas = {
        casos: {
            total: 0,
            pendientes: 0,
            enProceso: 0,
            cerrados: 0
        },
        usuarios: {
            total: 0,
            protegidos: 0,
            perfiles: 0
        },
        roles: {
            usuariosConPerfil: 0,
            asignaciones: 0,
            disponibles: 0
        }
    };

    window.recalcularMetricasUI = function () {
        const metricas = window.legalAppMetricas;

        // Dashboard principal
        const totalCasosEl = document.getElementById("totalCasos");
        const casosPendientesEl = document.getElementById("casosPendientes");
        const casosResueltosEl = document.getElementById("casosResueltos");
        const tasaCierreEl = document.getElementById("tasaCierre");

        const overviewTotalEl = document.getElementById("overviewTotal");
        const overviewPendientesEl = document.getElementById("overviewPendientes");
        const overviewCerradosEl = document.getElementById("overviewCerrados");

        const total = Number(metricas.casos.total ?? 0);
        const pendientes = Number(metricas.casos.pendientes ?? 0);
        const resueltos = Number(metricas.casos.cerrados ?? 0);
        const enProceso = Number(metricas.casos.enProceso ?? 0);
        const casosActivos = total - resueltos;

        const tasaCierre = total > 0
            ? `${Math.round((resueltos / total) * 100)}%`
            : "0%";

        if (totalCasosEl) totalCasosEl.textContent = casosActivos;
        if (casosPendientesEl) casosPendientesEl.textContent = pendientes;
        if (casosResueltosEl) casosResueltosEl.textContent = resueltos;
        if (tasaCierreEl) tasaCierreEl.textContent = tasaCierre;

        if (overviewTotalEl) overviewTotalEl.textContent = total;
        if (overviewPendientesEl) overviewPendientesEl.textContent = pendientes;
        if (overviewCerradosEl) overviewCerradosEl.textContent = resueltos;

        // Resumen módulo Casos
        const casosResumenTotalEl = document.getElementById("casosResumenTotal");
        const casosResumenPendientesEl = document.getElementById("casosResumenPendientes");
        const casosResumenCerradosEl = document.getElementById("casosResumenCerrados");

        if (casosResumenTotalEl) casosResumenTotalEl.textContent = total;
        if (casosResumenPendientesEl) casosResumenPendientesEl.textContent = pendientes;
        if (casosResumenCerradosEl) casosResumenCerradosEl.textContent = resueltos;

        // Gráfico resumen principal
        const chartPendientesValueEl = document.getElementById("chartPendientesValue");
        const chartEnProcesoValueEl = document.getElementById("chartEnProcesoValue");
        const chartCerradosValueEl = document.getElementById("chartCerradosValue");

        const chartPendientesBarEl = document.getElementById("chartPendientesBar");
        const chartEnProcesoBarEl = document.getElementById("chartEnProcesoBar");
        const chartCerradosBarEl = document.getElementById("chartCerradosBar");

        const totalGrafico = total > 0 ? total : 1;

        const pendientesPct = (pendientes / totalGrafico) * 100;
        const enProcesoPct = (enProceso / totalGrafico) * 100;
        const cerradosPct = (resueltos / totalGrafico) * 100;
        const insightEl = document.getElementById("insightText");
        const insightIcon = document.getElementById("insightIcon");

        if (insightEl) {
            const tasa = Math.round(cerradosPct);
            const esMovil = window.matchMedia("(max-width: 575.98px)").matches;

            let mensaje = "";

            if (cerradosPct < 40) {
                mensaje = esMovil
                    ? `<strong>Bajo umbral · Cierre ${tasa}%</strong><br><span>Priorizar resolución de pendientes.</span>`
                    : `La tasa de resolución actual (${tasa}%) se encuentra por debajo del umbral operativo esperado.
        Se observa acumulación de casos en seguimiento, lo que podría impactar la eficiencia del sistema.
        → Recomendación: priorizar la resolución de casos pendientes para optimizar el rendimiento operativo.`;
            }
            else if (cerradosPct >= 40 && cerradosPct < 70) {
                mensaje = esMovil
                    ? `<strong>Operación estable · Cierre ${tasa}%</strong><br><span>Optimizar casos en proceso.</span>`
                    : `El sistema mantiene una operación estable con una tasa de resolución del ${tasa}%.
        → Existe margen de mejora en la gestión de casos en proceso para aumentar la eficiencia.`;
            }
            else {
                mensaje = esMovil
                    ? `<strong>Operación óptima · Cierre ${tasa}%</strong><br><span>Dentro de parámetros esperados.</span>`
                    : `El sistema presenta un nivel óptimo de eficiencia con una tasa de resolución del ${tasa}%.
        → La operación se encuentra dentro de parámetros esperados.`;
            }

            insightEl.innerHTML = mensaje;
        }

        const formatPct = (value) => Math.round(value);

        if (chartPendientesValueEl) {
            chartPendientesValueEl.textContent = `${pendientes} (${formatPct(pendientesPct)}%)`;
        }

        if (chartEnProcesoValueEl) {
            chartEnProcesoValueEl.textContent = `${enProceso} (${formatPct(enProcesoPct)}%)`;
        }

        if (chartCerradosValueEl) {
            chartCerradosValueEl.textContent = `${resueltos} (${formatPct(cerradosPct)}%)`;
        }

        if (chartPendientesBarEl) chartPendientesBarEl.style.width = `${pendientesPct}%`;
        if (chartEnProcesoBarEl) chartEnProcesoBarEl.style.width = `${enProcesoPct}%`;
        if (chartCerradosBarEl) chartCerradosBarEl.style.width = `${cerradosPct}%`;

      
        // Resumen módulo Usuarios
        const usuariosResumenTotalEl = document.getElementById("usuariosResumenTotal");
        const usuariosResumenProtegidosEl = document.getElementById("usuariosResumenProtegidos");
        const usuariosResumenPerfilesEl = document.getElementById("usuariosResumenPerfiles");

        if (usuariosResumenTotalEl) usuariosResumenTotalEl.textContent = metricas.usuarios.total ?? 0;
        if (usuariosResumenProtegidosEl) usuariosResumenProtegidosEl.textContent = metricas.usuarios.protegidos ?? 0;
        if (usuariosResumenPerfilesEl) usuariosResumenPerfilesEl.textContent = metricas.usuarios.perfiles ?? 0;

        // Resumen módulo Roles
        const rolesResumenUsuariosEl = document.getElementById("rolesResumenUsuarios");
        const rolesResumenAsignacionesEl = document.getElementById("rolesResumenAsignaciones");
        const rolesResumenDisponiblesEl = document.getElementById("rolesResumenDisponibles");

        if (rolesResumenUsuariosEl) rolesResumenUsuariosEl.textContent = metricas.roles.usuariosConPerfil ?? 0;
        if (rolesResumenAsignacionesEl) rolesResumenAsignacionesEl.textContent = metricas.roles.asignaciones ?? 0;
        if (rolesResumenDisponiblesEl) rolesResumenDisponiblesEl.textContent = metricas.roles.disponibles ?? 0;
    };
    function actualizarResumen(resumen) {
        const total = Number(resumen?.total ?? 0);
        const pendientes = Number(resumen?.pendientes ?? 0);
        const resueltos = Number(resumen?.resueltos ?? 0);
        const enProceso = Math.max(total - pendientes - resueltos, 0);

        window.legalAppMetricas.casos.total = total;
        window.legalAppMetricas.casos.pendientes = pendientes;
        window.legalAppMetricas.casos.enProceso = enProceso;
        window.legalAppMetricas.casos.cerrados = resueltos;

        window.recalcularMetricasUI?.();
    }
    /** Muestra totales y contadores en la parte superior del dashboard. */
    function mostrarMensajeInformativo(mostrados, total) {
        const estadoTabla = document.getElementById("contadorResultados");
        if (!estadoTabla) return;
        estadoTabla.textContent = `Mostrando ${mostrados} de ${total} casos`;
    }

    /** Mensaje inferior: “Mostrando X de Y casos”*/
async function mostrarDetalleCaso(id, btnOrigen = null) {
    if (detalleCasoCargando) {
        console.warn("Detalle de caso ya está cargando. Clic ignorado.", {
            idActual: detalleCasoIdCargando,
            idSolicitado: id
        });
        return;
    }

    detalleCasoCargando = true;
    detalleCasoIdCargando = id;

    const btn = btnOrigen;
    const htmlOriginal = setBotonCargando(
        btn,
        true,
        `<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>`
    );

    try {
        const res = await fetch(`${apiUrl}/${id}?ts=${Date.now()}`, {
            cache: "no-store",
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });

        if (!res.ok) {
            if (res.status === 401) {
                clearSessionAndRedirect("expired-session-401");
                return;
            }

            throw new Error(`Error al obtener detalle. HTTP ${res.status}`);
        }

        const data = await res.json();

        document.getElementById("detalle-titulo").innerText = data.titulo ?? "—";
        document.getElementById("detalle-descripcion").innerText = data.descripcion ?? "—";
        document.getElementById("detalle-estado").innerText = data.estado ?? "—";
        document.getElementById("detalle-tipo").innerText = data.tipoCaso ?? "—";
        document.getElementById("detalle-cliente").innerText = data.nombreCliente ?? "—";
        document.getElementById("detalle-fecha").innerText = data.fechaCreacion
            ? new Date(data.fechaCreacion).toLocaleDateString()
            : "—";

        const grupoMotivo = document.getElementById("grupo-motivo-cierre");
        const motivoEl = document.getElementById("detalle-motivo-cierre");

        if ((data.estado ?? "").toLowerCase() === "cerrado" && data.motivoCierre) {
            motivoEl.innerText = data.motivoCierre;
            grupoMotivo.classList.remove("d-none");
        } else {
            motivoEl.innerText = "";
            grupoMotivo.classList.add("d-none");
        }

        const modalEl = document.getElementById("modalDetalle");
        if (!modalEl) throw new Error("No existe #modalDetalle en el DOM.");

        const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        modal.show();

    } catch (error) {
        console.error("Error al cargar detalle:", error);
        limpiarBackdropsHuerfanos();

        if (window.Swal) {
            Swal.fire({
                icon: "error",
                title: "No se pudo cargar el detalle",
                text: "La información del caso no respondió a tiempo. Intenta nuevamente.",
                confirmButtonText: "Entendido"
            });
        } else {
            alert("No se pudo cargar el detalle del caso.");
        }

    } finally {
        if (btn) {
            setBotonCargando(btn, false);
            if (htmlOriginal !== null) btn.innerHTML = htmlOriginal;
        }

        detalleCasoCargando = false;
        detalleCasoIdCargando = null;
    }
}

    /* Trae los datos de un caso por ID.
Rellena un modal Bootstrap para mostrar info detallada */

    function textoSeguro(valor, fallback = "") {
        return valor === null || valor === undefined ? fallback : String(valor);
    }

    function normalizarEstado(estado) {
        return textoSeguro(estado).toLowerCase();
    }

    function crearCeldaCaso(valor = null, clase = "") {
        const celda = document.createElement("td");
        if (clase) celda.className = clase;
        if (valor !== null) celda.textContent = textoSeguro(valor);
        return celda;
    }

    function crearEstadoBadge(estado) {
        const badge = document.createElement("span");
        const estadoNormalizado = normalizarEstado(estado);

        badge.classList.add("badge");
        switch (estadoNormalizado) {
            case "pendiente":
                badge.classList.add("estado-pendiente");
                badge.textContent = "Pendiente";
                break;
            case "enproceso":
                badge.classList.add("estado-enproceso");
                badge.textContent = "En Proceso";
                break;
            case "cerrado":
                badge.classList.add("estado-cerrado");
                badge.textContent = "Cerrado";
                break;
            default:
                badge.classList.add("bg-secondary", "badge-estado");
                badge.textContent = textoSeguro(estado);
                break;
        }

        return badge;
    }

    function crearIconoTipo(tipo) {
        const icono = document.createElement("i");
        const tipoNormalizado = textoSeguro(tipo).toLowerCase();
        const clasesPorTipo = {
            laboral: "bi-people text-primary me-2",
            familia: "bi-house-heart text-success me-2",
            civil: "bi-bank text-info me-2",
            penal: "bi-shield-exclamation text-danger me-2"
        };

        icono.className = `bi ${clasesPorTipo[tipoNormalizado] || "bi-folder2-open text-secondary me-2"}`;
        return icono;
    }

    function crearBotonCaso(clases, id, estado, version, titulo, iconoClase) {
        const boton = document.createElement("button");
        boton.type = "button";
        boton.className = clases;
        boton.dataset.id = textoSeguro(id);
        boton.dataset.estado = textoSeguro(estado);
        boton.dataset.version = textoSeguro(version);
        boton.title = titulo;

        const icono = document.createElement("i");
        icono.className = `bi ${iconoClase}`;
        boton.appendChild(icono);
        return boton;
    }

    function crearFilaCardCaso(etiqueta, valor) {
        const fila = document.createElement("div");
        fila.className = "caso-card-row";
        const label = document.createElement("span");
        label.className = "caso-card-label";
        label.textContent = etiqueta;

        const contenido = valor instanceof Element
            ? valor
            : (() => {
                const span = document.createElement("span");
                span.className = "caso-card-value";
                span.textContent = textoSeguro(valor);
                return span;
            })();

        fila.append(label, contenido);
        return fila;
    }

    // Función reutilizable para mostrar errores claros al usuario
    async function mostrarErrorDesdeResponse(respuesta, mensajePorDefecto) {
        try {
            const errorJson = await respuesta.json();
            const mensaje = errorJson.detail || mensajePorDefecto;

            Swal.fire({
                icon: 'warning',
                title: 'Error',
                text: mensaje,
            });
            return mensaje;
        } catch {
            Swal.fire({
                icon: 'error',
                title: 'Error inesperado',
                text: mensajePorDefecto,
            });
            return mensajePorDefecto;
        }
    }
//  VALIDACIÓN INLINE (helpers)
function clearInvalid(form) {
  if (!form) return;
  form.querySelectorAll(".is-invalid").forEach(el => el.classList.remove("is-invalid"));
  form.querySelectorAll(".invalid-feedback").forEach(fb => (fb.textContent = ""));
}

function setInvalid(inputEl, message) {
  if (!inputEl) return;
  inputEl.classList.add("is-invalid");
  const feedback = inputEl.closest(".mb-3")?.querySelector(".invalid-feedback");
  if (feedback) feedback.textContent = message;
}

function validateCasoForm() {
        const form = document.getElementById("formGestionCaso");
        const tituloEl = document.getElementById("form-titulo");
        const descEl = document.getElementById("form-descripcion");
        const tipoEl = document.getElementById("form-tipo");
        const clienteEl = document.getElementById("form-cliente");

        clearInvalid(form);

        const titulo = tituloEl.value.trim();
        const descripcion = descEl.value.trim();
        const tipoCaso = tipoEl.value;
        const clienteId = parseInt(clienteEl.value, 10);

        let ok = true;

        if (!titulo) {
            setInvalid(tituloEl, "Debes ingresar un título.");
            ok = false;
        } else if (titulo.length < 5 || titulo.length > 150) {
            setInvalid(tituloEl, "Debe tener entre 5 y 150 caracteres.");
            ok = false;
        }

        if (!descripcion) {
            setInvalid(descEl, "Debes ingresar una descripción.");
            ok = false;
        } else if (descripcion.length < 10) {
            setInvalid(descEl, "La descripción debe tener al menos 10 caracteres.");
            ok = false;
        } else if (descripcion.length > 5000) {
            setInvalid(descEl, "No puede superar 5000 caracteres.");
            ok = false;
        }

        if (!tipoCaso) {
            setInvalid(tipoEl, "Debes seleccionar un tipo de caso.");
            ok = false;
        }

        if (!Number.isInteger(clienteId) || clienteId < 1) {
            setInvalid(clienteEl, "Debes seleccionar un cliente válido.");
            ok = false;
        }

        return ok;
    }


    document.getElementById("logoutBtn")?.addEventListener("click", () => {
        localStorage.removeItem("jwt_token");
        localStorage.removeItem("usuario_actual");
        sessionStorage.removeItem("demoContext");
        window.location.hash = "";
        window.location.href = "login.html";
    });

    // Limpia el token y usuario del almacenamiento, redirige al login. **/
    document.addEventListener("click", async (e) => {
        // Ver detalle

if (e.target.closest(".btn-ver")) {
    e.preventDefault();
    e.stopPropagation();

    const btn = e.target.closest(".btn-ver");
    const id = btn.dataset.id;

    if (!id) return;
    if (btn.disabled || btn.getAttribute("aria-busy") === "true") return;

    mostrarDetalleCaso(id, btn);
    return;
}

    // EDITAR
if (e.target.closest(".btn-editar-caso, .btn-outline-warning")) {
    e.preventDefault();
    e.stopPropagation();

    const btn = e.target.closest(".btn-editar-caso, .btn-outline-warning");
    const row = btn.closest("tr");
    const card = btn.closest(".caso-card");

    const id = btn.dataset.id || row?.children[0]?.textContent?.trim();
    const estado = (
        btn.dataset.estado ||
        card?.dataset.estado ||
        row?.children[2]?.innerText ||
        ""
    ).trim().toLowerCase();

    if (!id) return;

    const claveAccion = `editar:${id}`;
    if (!iniciarAccionCaso(claveAccion)) return;

    const htmlOriginal = setBotonCargando(
        btn,
        true,
        `<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>`
    );

    try {
        if (estado === "cerrado") {
            Swal.fire({
                icon: "info",
                title: "No editable",
                text: "Este caso está cerrado y no puede ser modificado.",
            });
            return;
        }

        const res = await fetch(`${apiUrl}/${id}?ts=${Date.now()}`, {
            cache: "no-store",
            headers: {
                "Authorization": `Bearer ${token}`
            }
        });

        if (!res.ok) {
            if (res.status === 401) {
                clearSessionAndRedirect("expired-session-401");
                return;
            }

            if (res.status === 429) {
                throw new Error("Demasiadas solicitudes. Espera unos segundos e intenta nuevamente.");
            }

            throw new Error("No se pudo obtener el caso");
        }

        const data = await res.json();

        const form = document.getElementById("formGestionCaso");
        clearInvalid(form);

        const tituloInput = document.getElementById("form-titulo");
        const tipoSelect = document.getElementById("form-tipo");
        const clienteSelect = document.getElementById("form-cliente");
        const descripcionInput = document.getElementById("form-descripcion");

        const submitBtn = document.querySelector('button[form="formGestionCaso"][type="submit"]');
        if (submitBtn) {
            submitBtn.style.display = "";
            submitBtn.disabled = false;
            submitBtn.innerHTML = '<i class="bi bi-save me-1"></i> Guardar Cambios';
        }

        [tituloInput, tipoSelect, clienteSelect, descripcionInput].forEach(el => {
            if (el) el.disabled = false;
        });

        const rol = obtenerRolEfectivo(obtenerRolesDesdeJWT());

        if (rol === "Abogado") {
            clienteSelect.disabled = true;

            if (data.estado === "EnProceso") {
                tipoSelect.disabled = true;
                tituloInput.disabled = true;
            }

            if (data.estado === "Cerrado") {
                tituloInput.disabled = true;
                tipoSelect.disabled = true;
                clienteSelect.disabled = true;
                descripcionInput.disabled = true;

                if (submitBtn) {
                    submitBtn.style.display = "none";
                }
            }
        }

        document.getElementById("form-id").value = data.id;
        document.getElementById("form-version").value = data.version ?? "";
        document.getElementById("form-titulo").value = data.titulo ?? "";
        document.getElementById("form-descripcion").value = data.descripcion ?? "";

        await cargarClientes();

        clienteSelect.value = data.clienteId;
        document.getElementById("grupo-cliente").style.display = "block";

        const estadoSpan = document.getElementById("form-estado");

        const estadoVisual =
            data.estado === "Pendiente"
                ? "EnProceso"
                : data.estado;

        estadoSpan.textContent = estadoVisual;
        estadoSpan.className = "badge";

        if (estadoVisual === "Pendiente") {
            estadoSpan.classList.add("estado-pendiente");
        } else if (estadoVisual === "EnProceso") {
            estadoSpan.classList.add("estado-enproceso");
        } else if (estadoVisual === "Cerrado") {
            estadoSpan.classList.add("estado-cerrado");
        }

        document.getElementById("form-tipo").value = data.tipoCaso;
        document.getElementById("modalGestionCasoLabel").textContent = "✏️ Editar Caso";

        const modalEl = document.getElementById("modalGestionCaso");
        bootstrap.Modal.getOrCreateInstance(modalEl).show();

    } catch (err) {
        console.error("❌ Error al cargar caso:", err);

        Swal.fire({
            icon: "error",
            title: "No se pudo cargar el caso",
            text: err.message || "Intenta nuevamente en unos segundos."
        });

    } finally {
        setBotonCargando(btn, false);
        if (htmlOriginal !== null) btn.innerHTML = htmlOriginal;

        finalizarAccionCaso(claveAccion);
    }

    return;
}

        // Eliminar con SweetAlert (esto va *fuera* del bloque de editar)
        if (e.target.closest(".btn-eliminar")) {
            const btn = e.target.closest(".btn-eliminar");
            const id = btn.dataset.id;

            const fila = btn.closest("tr");
            const card = btn.closest(".caso-card");

            const estado = (
                btn.dataset.estado ||
                card?.dataset.estado ||
                fila?.children[2]?.innerText ||
                ""
            ).trim().toLowerCase();

            if (estado === "cerrado") {
                Swal.fire({
                    icon: "warning",
                    title: "No se puede eliminar",
                    text: "Este caso está cerrado y no puede ser eliminado.",
                });
                return; // No seguimos con el fetch
            }

            // Confirmación visual
            Swal.fire({
                title: '¿Estás seguro?',
                text: "Esta acción eliminará el caso permanentemente.",
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#d33',
                cancelButtonColor: '#6c757d',
                confirmButtonText: 'Sí, eliminar',
                cancelButtonText: 'Cancelar'
            }).then(async (result) => {
                if (result.isConfirmed) {
                    try {
                        Swal.fire({
                            title: 'Eliminando...',
                            allowOutsideClick: false,
                            didOpen: () => {
                                Swal.showLoading();
                            }
                        });

                        const res = await fetch(`${apiUrl}/${id}`, {
                            method: "DELETE",
                            headers: { "Authorization": `Bearer ${token}` }
                        });

                        if (!res.ok) {
                            const errorJson = await res.json();
                            const mensajeError = errorJson.detail || "Error inesperado";
                            // Mostrar mensaje personalizado según tipo de error
                            if (res.status === 400 || res.status === 404 || res.status === 409) {
                                Swal.fire({
                                    icon: 'warning',
                                    title: 'No se pudo eliminar',
                                    text: mensajeError,
                                });
                            } else {
                                Swal.fire({
                                    icon: 'error',
                                    title: 'Error inesperado',
                                    text: 'Ocurrió un problema al intentar eliminar el caso.',
                                });
                            }

                            return; // detenemos el flujo
                        }
                        Swal.fire({
                            toast: true,
                            position: 'top-end',
                            icon: 'success',
                            title: 'Caso eliminado exitosamente',
                            showConfirmButton: false,
                            timer: 2000,
                            timerProgressBar: true
                        });

                        await cargarCasosDesdeBackend();
                    } catch (error) {
                        Swal.fire('Error', error.message, 'error');
                    }
                }
            });
        }
// CERRAR CASO
if (e.target.closest(".btn-cerrar")) {
    e.preventDefault();
    e.stopPropagation();

    const btn = e.target.closest(".btn-cerrar");
    const id = btn.dataset.id;
    const version = btn.dataset.version;

    const fila = btn.closest("tr");
    const card = btn.closest(".caso-card");

    const estado = (
        btn.dataset.estado ||
        card?.dataset.estado ||
        fila?.children[2]?.innerText ||
        ""
    ).trim().toLowerCase();

    if (!id) return;

    if (!version) {
        await cargarCasosDesdeBackend();
        Swal.fire({
            icon: "info",
            title: "Datos actualizados",
            text: "Recarga el caso e intenta cerrarlo nuevamente."
        });
        return;
    }

    const claveAccion = `cerrar:${id}`;
    if (!iniciarAccionCaso(claveAccion)) return;

    const htmlOriginal = setBotonCargando(
        btn,
        true,
        `<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>`
    );

    try {
        if (estado === "cerrado") {
            Swal.fire({
                icon: "info",
                title: "Caso ya cerrado",
                text: "Este caso ya se encuentra cerrado.",
            });
            return;
        }

        const result = await Swal.fire({
            title: "Cerrar caso",
            input: "textarea",
            inputLabel: "Motivo del cierre (opcional)",
            inputPlaceholder: "Escribe el motivo si corresponde...",
            inputAttributes: {
                "aria-label": "Motivo de cierre"
            },
            showCancelButton: true,
            confirmButtonText: "Cerrar caso",
            cancelButtonText: "Cancelar"
        });

        if (!result.isConfirmed) return;

        const motivo = result.value ?? "";

        Swal.fire({
            title: "Cerrando caso...",
            allowOutsideClick: false,
            didOpen: () => Swal.showLoading()
        });

        const res = await fetch(`${apiUrl}/${id}/cerrar`, {
            method: "PUT",
            headers: {
                "Content-Type": "application/json",
                "Authorization": `Bearer ${token}`
            },
            body: JSON.stringify({ motivoCierre: motivo, version })
        });

        if (!res.ok) {
            if (res.status === 401) {
                clearSessionAndRedirect("expired-session-401");
                return;
            }

            let mensajeError = "Error inesperado";

            try {
                const errorJson = await res.json();
                mensajeError = errorJson.detail || mensajeError;
            } catch {
                // Si no viene JSON, usamos mensaje genérico.
            }

            if (res.status === 429) {
                throw new Error("Demasiadas solicitudes. Espera unos segundos e intenta nuevamente.");
            }

            throw new Error(mensajeError);
        }

        await cargarCasosDesdeBackend();

        Swal.fire({
            toast: true,
            position: "top-end",
            icon: "success",
            title: "Caso cerrado con éxito",
            showConfirmButton: false,
            timer: 2000,
            timerProgressBar: true
        });

    } catch (error) {
        console.error("Error al cerrar caso:", error);

        Swal.fire({
            icon: "error",
            title: "Error al cerrar",
            text: error.message || "No se pudo cerrar el caso."
        });

    } finally {
        setBotonCargando(btn, false);
        if (htmlOriginal !== null) btn.innerHTML = htmlOriginal;

        finalizarAccionCaso(claveAccion);
    }

    return;
}
    });
      
document.getElementById("btnNuevoCaso")?.addEventListener("click", async () => {
    if (abriendoNuevoCaso) return;

    abriendoNuevoCaso = true;

    const btnNuevo = document.getElementById("btnNuevoCaso");
    const htmlOriginal = btnNuevo?.innerHTML;

    try {
        if (btnNuevo) {
            btnNuevo.disabled = true;
            btnNuevo.classList.add("disabled");
            btnNuevo.setAttribute("aria-busy", "true");
            btnNuevo.innerHTML = `
                <span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>
                Cargando...
            `;
        }

        const form = document.getElementById("formGestionCaso");
        form.reset();

        clearInvalid(form);

        // Asegurar que el botón Guardar siempre vuelva.
        const submitBtn = document.querySelector('button[form="formGestionCaso"][type="submit"]');
        if (submitBtn) {
            submitBtn.style.display = "";
            submitBtn.disabled = false;
            submitBtn.innerHTML = '<i class="bi bi-save me-1"></i> Guardar Cambios';
        }

        // Nuevo caso = todos los campos editables.
        ["form-titulo", "form-descripcion", "form-tipo", "form-cliente"].forEach(id => {
            const el = document.getElementById(id);
            if (el) el.disabled = false;
        });

        document.getElementById("form-id").value = "";
        document.getElementById("form-version").value = "";

        const clienteSelect = document.getElementById("form-cliente");
        clienteSelect.disabled = false;

        await cargarClientes();

        document.getElementById("grupo-cliente").style.display = "block";

        const estadoSpan = document.getElementById("form-estado");
        estadoSpan.textContent = "Pendiente";
        estadoSpan.className = "badge estado-pendiente";

        document.getElementById("modalGestionCasoLabel").textContent = "📝 Nuevo Caso";

        const modalEl = document.getElementById("modalGestionCaso");
        const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        modal.show();

    } catch (error) {
        console.error("❌ Error al preparar nuevo caso:", error);

        Swal.fire({
            icon: "error",
            title: "No se pudo preparar el formulario",
            text: error.message || "No se pudieron cargar los datos necesarios. Intenta nuevamente.",
            confirmButtonText: "Entendido"
        });

    } finally {
        if (btnNuevo) {
            btnNuevo.disabled = false;
            btnNuevo.classList.remove("disabled");
            btnNuevo.removeAttribute("aria-busy");
            btnNuevo.innerHTML = htmlOriginal;
        }

        abriendoNuevoCaso = false;
    }
});

    document.getElementById("formGestionCaso")?.addEventListener("submit", async (e) => {
        e.preventDefault();
        //  Loading state 
        const submitBtn = document.querySelector(
          'button[form="formGestionCaso"][type="submit"]'
        );

      if (submitBtn) {
    submitBtn.disabled = true;
    submitBtn.innerHTML = "Guardando...";
}
        // VALIDACIÓN INLINE
    if (!validateCasoForm()) {
        submitBtn.disabled = false;
        submitBtn.innerHTML = '<i class="bi bi-save me-1"></i> Guardar Cambios';
        return;
    }



        const id = document.getElementById("form-id").value.trim();
        const version = document.getElementById("form-version").value.trim();
        const titulo = document.getElementById("form-titulo").value.trim();
        const descripcion = document.getElementById("form-descripcion").value.trim();
        const tipoCaso = document.getElementById("form-tipo").value;

        const clienteId = parseInt(
            document.getElementById("form-cliente").value,
            10
        );  

       
        const esNuevo = id === "";

        const caso = {
            Titulo: titulo,
            Descripcion: descripcion,
            TipoCaso: tipoCaso,
            ClienteId: clienteId
        };

        if (!esNuevo) {
            caso.Version = version;
        }

        const url = esNuevo ? apiUrl : `${apiUrl}/${id}`;
        const metodo = esNuevo ? "POST" : "PUT";

            let response; 
            try {
    const response = await fetch(url, {
        method: metodo,
        headers: {
            "Content-Type": "application/json",
            "Authorization": `Bearer ${token}`
        },
        body: JSON.stringify(caso)
    });

    if (!response.ok) {
        throw response; // ⬅️ NO lo parses aquí
    }

    //  ÉXITO
    bootstrap.Modal.getInstance(document.getElementById("modalGestionCaso")).hide();

    // Recarga la tabla
    await cargarCasosDesdeBackend();

    Swal.fire({
        toast: true,
        position: 'top-end',
        icon: 'success',
        title: esNuevo ? "Caso creado con éxito" : "Caso actualizado con éxito",
        showConfirmButton: false,
        timer: 2000,
        timerProgressBar: true
    });

} catch (error) {
    console.error("❌ Error al guardar caso:", error);

    //  Error backend (viene como Response)
    if (error instanceof Response) {
        const msg = await mostrarErrorDesdeResponse(
            error,
            esNuevo
                ? "No se pudo crear el caso."
                : "No se pudo actualizar el caso."
        );

        //  REGLA DE NEGOCIO ESPECÍFICA (si el backend lo dice)
        if (msg && msg.toLowerCase().includes("cliente ya tiene")) {
            Swal.fire({
                icon: 'warning',
                title: 'No se puede cambiar el cliente',
                text: 'Este cliente ya tiene un caso activo. Para modificar el cliente debes ingresar como Administrador.',
            });
        }

    } else {
        // Error de conexión / JS
        Swal.fire({
            icon: 'error',
            title: 'Error de conexión',
            text: error.message || 'No se pudo conectar con el servidor.',
        });
    }

} finally {
    // 🔚 SIEMPRE restaurar botón (éxito o error)
    submitBtn.disabled = false;
    submitBtn.innerHTML = '<i class="bi bi-save me-1"></i> Guardar Cambios';
}
        
    });
});

// CONTROLADOR DE MÓDULOS (NUEVO)
const MODULOS = [
    'mod-dashboard',
    'mod-casos',
    'mod-usuarios',
    'mod-roles'
];
function mostrarModulo(id) {
    MODULOS.forEach(m => {
        const el = document.getElementById(m);
        if (el) {
            el.classList.add('d-none');
            el.classList.remove('modulo-entrando');
        }
    });

    const activo = document.getElementById(id);
    if (activo) {
        activo.classList.remove('d-none');

        // Reinicia animación de entrada del módulo
        void activo.offsetWidth;
        activo.classList.add('modulo-entrando');

        setTimeout(() => {
            activo.classList.remove('modulo-entrando');
        }, 700);
    }
}
// LOADER POR MÓDULO (PASO 1)
let recargarCasos = null;

function onModuloCargado(moduloId) {
  switch (moduloId) {
    case "mod-casos":
      recargarCasos?.();
      break;

    case "mod-usuarios":
      window.initUsuariosModule?.();
      break;

    case "mod-roles":
      window.refrescarUsuariosEnRoles?.();
      break;

  }
}
// SIDEBAR (UX / NAVEGACIÓN)
function isMobileViewport() {
    return window.innerWidth <= 1199.98;
}

function toggleSidebar() {
    const sidebar = document.getElementById("sidebar");
    const overlay = document.getElementById("sidebarOverlay");

    if (!sidebar) return;

    if (isMobileViewport()) {
        const willOpen = !sidebar.classList.contains("mobile-open");
        sidebar.classList.toggle("mobile-open", willOpen);
        overlay?.classList.toggle("show", willOpen);
        return;
    }

    sidebar.classList.toggle("collapsed");
}

function closeSidebarMobile() {
    const sidebar = document.getElementById("sidebar");
    const overlay = document.getElementById("sidebarOverlay");

    if (!sidebar) return;

    sidebar?.classList.remove("mobile-open");
    overlay?.classList.remove("show");
}

window.addEventListener("resize", () => {
    const sidebar = document.getElementById("sidebar");
    if (!sidebar) return;

    if (window.innerWidth >= 1200) {
        closeSidebarMobile();
        sidebar.classList.remove("collapsed");
    } else {
        sidebar.classList.add("collapsed");
        sidebar.classList.remove("mobile-open");
    }
});

async function cargarClientes() {
    const select = document.getElementById("form-cliente");
    if (!select) return;

    const token = localStorage.getItem("jwt_token");
    if (!token) {
        clearSessionAndRedirect("missing-token-clientes");
        return;
    }

    // La función se vuelve autosuficiente:
    // siempre deja el select limpio antes de cargar opciones nuevas.
    select.innerHTML = `<option value="">-- Seleccione cliente --</option>`;

    const response = await fetch(`/api/Clientes?ts=${Date.now()}`, {
        cache: "no-store",
        headers: {
            "Authorization": `Bearer ${token}`
        }
    });

    if (!response.ok) {
        if (response.status === 401) {
            clearSessionAndRedirect("expired-session-401");
            return;
        }

        if (response.status === 429) {
            throw new Error("Demasiadas solicitudes cargando clientes. Espera unos segundos.");
        }

        throw new Error("No se pudieron cargar los clientes.");
    }

    const clientes = await response.json();

    clientes.forEach(c => {
        const option = document.createElement("option");
        option.value = c.id;
        option.textContent = c.nombre;
        select.appendChild(option);
    });
}

window.addEventListener("hashchange", () => {
    const hashModulo = (window.location.hash || "").replace("#", "").trim();

    if (!hashModulo) return;
    navigate(hashModulo);

});


function obtenerMetricasParaPDF() {
    const metricas = window.legalAppMetricas || {};
    const roles = obtenerRolesDesdeJWT();
    const rol = obtenerRolEfectivo(roles);
    const incluirGobernanza = rol === "Admin";

    const total = Number(metricas.casos?.total ?? 0);
    const pendientes = Number(metricas.casos?.pendientes ?? 0);
    const cerrados = Number(metricas.casos?.cerrados ?? 0);
    const enProceso = Number(metricas.casos?.enProceso ?? Math.max(total - pendientes - cerrados, 0));

    const usuariosTotal = Number(metricas.usuarios?.total ?? 0);
    const usuariosProtegidos = Number(metricas.usuarios?.protegidos ?? 0);
    const perfilesDistintos = Number(metricas.usuarios?.perfiles ?? 0);

    const usuariosConPerfil = Number(metricas.roles?.usuariosConPerfil ?? 0);
    const rolesAsignados = Number(metricas.roles?.asignaciones ?? 0);
    const rolesDisponibles = Number(metricas.roles?.disponibles ?? 0);

    const pct = (valor) => total > 0 ? Math.round((valor / total) * 100) : 0;
    const tasaCierre = pct(cerrados);
    const pctPendientes = pct(pendientes);
    const pctProceso = pct(enProceso);
    const pctCerrados = pct(cerrados);

    let estadoGeneral = "Sin operación registrada";
    if (total > 0 && tasaCierre >= 65) estadoGeneral = "Operación saludable";
    else if (total > 0 && tasaCierre >= 40) estadoGeneral = "Operación estable con foco";
    else if (total > 0) estadoGeneral = "Mantener monitoreo periódico";

    const fechaGeneracion = new Date().toLocaleString("es-CL", {
        dateStyle: "long",
        timeStyle: "short"
    });

    const resumen = total === 0
        ? "Actualmente no existen casos registrados en el sistema, por lo que el tablero aún no refleja carga operativa activa. El entorno se encuentra disponible para poblamiento y validación funcional."
        : `Control Lex registra ${total} casos en total. De ellos, ${pendientes} permanecen pendientes, ${enProceso} se encuentran en proceso y ${cerrados} ya fueron cerrados. La tasa de cierre actual alcanza ${tasaCierre}%, permitiendo evaluar el nivel de resolución operativa y la presión sobre la carga activa del entorno.`;

    let hallazgos = [];

    if (incluirGobernanza) {
        hallazgos = [
            total === 0
                ? "Aún no existe carga de casos suficiente para evaluar desempeño operativo."
                : `${pctPendientes}% del volumen total se mantiene pendiente, indicador clave para seguimiento y priorización operativa.`,
            usuariosTotal > 0
                ? `La base administrativa considera ${usuariosTotal} usuarios, con ${usuariosProtegidos} cuentas protegidas y ${perfilesDistintos} perfiles distintos registrados.`
                : "No se observan métricas de usuarios suficientemente pobladas en este momento.",
            rolesAsignados > 0
                ? `Existen ${rolesAsignados} asignaciones de rol sobre ${rolesDisponibles} roles disponibles, con ${usuariosConPerfil} usuarios vinculados a perfiles.`
                : "Las asignaciones de roles aún no muestran volumen suficiente para una lectura más profunda de gobernanza."
        ];
    } else {
        hallazgos = [
            total === 0
                ? "Aún no existe carga de casos suficiente para evaluar desempeño operativo."
                : `${pctPendientes}% del volumen total se mantiene pendiente, indicador clave para seguimiento y priorización operativa.`,
            total > 0
                ? `El porcentaje de cierre actual alcanza ${tasaCierre}%, permitiendo medir capacidad de resolución y estabilidad operativa.`
                : "Todavía no existe suficiente actividad para medir una tasa de cierre representativa.",
            enProceso > 0
                ? `${enProceso} casos permanecen en proceso, lo que exige seguimiento para evitar acumulación operativa.`
                : "La carga en proceso se mantiene contenida en este momento."
        ];
    }

    let recomendacion = "Mantener monitoreo periódico del dashboard y continuar poblamiento controlado para fortalecer trazabilidad, evidencia visual y lectura ejecutiva del entorno.";
    if (total > 0 && pctPendientes >= 45) {
        recomendacion = "Priorizar la reducción del backlog pendiente mediante seguimiento activo, reasignación operativa y revisión de tiempos de resolución para mejorar la percepción de control del servicio.";
    } else if (total > 0 && tasaCierre >= 65) {
        recomendacion = "El comportamiento operativo es favorable. Conviene consolidar este desempeño con evidencia periódica, continuidad del dato y refuerzo visual de la propuesta de valor frente a clientes.";
    }
    let mensajeClave = "La operación presenta visibilidad suficiente para toma de decisión ejecutiva.";
    let fortalezaClave = "La estructura de métricas permite monitorear carga, avance y capacidad de resolución.";
    let prioridadClave = "Mantener seguimiento periódico para sostener trazabilidad y lectura de desempeño.";

    if (total > 0 && tasaCierre < 40) {
        mensajeClave = "La operación requiere foco inmediato sobre backlog y capacidad de cierre.";
        fortalezaClave = "Existe visibilidad clara del volumen activo y del comportamiento por estado.";
        prioridadClave = "Reducir pendientes y acelerar cierres para mejorar percepción de control del servicio.";
    } else if (total > 0 && tasaCierre >= 65) {
        mensajeClave = "La operación muestra un comportamiento estable y una resolución saludable.";
        fortalezaClave = "La tasa de cierre confirma una dinámica de gestión favorable.";
        prioridadClave = "Consolidar este desempeño con monitoreo preventivo y continuidad del dato.";
    }
    return {
        total,
        pendientes,
        enProceso,
        cerrados,
        tasaCierre,
        pctPendientes,
        pctProceso,
        pctCerrados,
        usuariosTotal,
        usuariosProtegidos,
        perfilesDistintos,
        usuariosConPerfil,
        rolesAsignados,
        rolesDisponibles,
        fechaGeneracion,
        estadoGeneral,
        resumen,
        hallazgos,
        recomendacion,
        mensajeClave,
        fortalezaClave,
        prioridadClave,
        rol,
        incluirGobernanza
    };
}

    function crearNodoPDF(data) {
        const template = document.getElementById("pdf-report-template");

        if (!template) {
            throw new Error("No existe #pdf-report-template");
        }

        const reportNode = template.content.firstElementChild.cloneNode(true);
        reportNode.id = "pdf-report-root";

        const setText = (selector, value) => {
            const el = reportNode.querySelector(selector);
            if (el) el.textContent = value;
        };

        const setWidth = (selector, value) => {
            const el = reportNode.querySelector(selector);
            if (el) el.style.width = `${Math.max(0, Math.min(100, value))}%`;
        };

        setText('[data-pdf="fecha"]', data.fechaGeneracion);
        setText('[data-pdf="estadoGeneral"]', data.estadoGeneral);
        setText('[data-pdf="summary"]', data.resumen);

        setText('[data-pdf="total"]', data.total);
        setText('[data-pdf="pendientes"]', data.pendientes);
        setText('[data-pdf="proceso"]', data.enProceso);
        setText('[data-pdf="tasa"]', `${data.tasaCierre}%`);

        setText('[data-pdf="pendientesTexto"]', `${data.pendientes} (${data.pctPendientes}%)`);
        setText('[data-pdf="procesoTexto"]', `${data.enProceso} (${data.pctProceso}%)`);
        setText('[data-pdf="cerradosTexto"]', `${data.cerrados} (${data.pctCerrados}%)`);

        setWidth('[data-pdf="pendientesBar"]', data.pctPendientes);
        setWidth('[data-pdf="procesoBar"]', data.pctProceso);
        setWidth('[data-pdf="cerradosBar"]', data.pctCerrados);

        setText('[data-pdf="usuariosTotal"]', data.usuariosTotal);
        setText('[data-pdf="usuariosProtegidos"]', data.usuariosProtegidos);
        setText('[data-pdf="perfilesDistintos"]', data.perfilesDistintos);
        setText('[data-pdf="usuariosConPerfil"]', data.usuariosConPerfil);
        setText('[data-pdf="rolesAsignados"]', data.rolesAsignados);
        setText('[data-pdf="rolesDisponibles"]', data.rolesDisponibles);

        setText('[data-pdf="hallazgo1"]', data.hallazgos[0] || "");
        setText('[data-pdf="hallazgo2"]', data.hallazgos[1] || "");
        setText('[data-pdf="hallazgo3"]', data.hallazgos[2] || "");
        setText('[data-pdf="recomendacion"]', data.recomendacion);
        setText('[data-pdf="mensajeClave"]', data.mensajeClave);
        setText('[data-pdf="estadoGeneral2"]', data.estadoGeneral);
        setText('[data-pdf="fortalezaClave"]', data.fortalezaClave);
        setText('[data-pdf="prioridadClave"]', data.prioridadClave);

        if (!data.incluirGobernanza) {
            reportNode.querySelector("#pdf-section-gobernanza")?.remove();
        }

        reportNode.style.display = "block";
        reportNode.style.width = "720px";
        reportNode.style.minWidth = "720px";
        reportNode.style.maxWidth = "720px";
        reportNode.style.margin = "0";
        reportNode.style.height = "auto";
        reportNode.style.overflow = "visible";
        reportNode.style.background = "#ffffff";
        reportNode.style.position = "static";
        reportNode.style.left = "auto";
        reportNode.style.top = "auto";
        reportNode.style.zIndex = "auto";
        reportNode.style.transform = "none";
        reportNode.style.filter = "none";
        reportNode.style.opacity = "1";
        reportNode.style.visibility = "visible";
        reportNode.style.pointerEvents = "none";
        reportNode.style.boxSizing = "border-box";

        reportNode.querySelectorAll("*").forEach(el => {
            el.style.transition = "none";
            el.style.animation = "none";
            el.style.transform = "none";
        });

        return reportNode;
    }

function setEstadoBotonPDF(btn, exportando) {
    if (!btn) return;

    if (exportando) {
        btn.disabled = true;
        btn.dataset.exporting = "true";
        btn.innerHTML = `<i class="bi bi-hourglass-split"></i> Generando PDF...`;
        return;
    }

    btn.disabled = false;
    btn.dataset.exporting = "false";
    btn.innerHTML = `<i class="bi bi-file-earmark-pdf"></i> Exportar Reporte`;
}
function exportarReportePDF() {
    const btn = document.getElementById("btnExportPDF");
    if (!btn || btn.dataset.exporting === "true") return;

    const rol = obtenerRolEfectivo(obtenerRolesDesdeJWT());

    if (rol !== "Admin" && rol !== "Abogado") {
        throw new Error("No tienes permisos para exportar este reporte.");
    }

    try {
        const jsPDFCtor = window.jspdf?.jsPDF || window.jsPDF;

        if (!jsPDFCtor) {
            throw new Error("jsPDF no está cargado. Revisa que el script de jsPDF esté antes de dashboard.js.");
        }

        setEstadoBotonPDF(btn, true);

        const data = obtenerMetricasParaPDF();

        const doc = new jsPDFCtor({
            orientation: "portrait",
            unit: "mm",
            format: "a4",
            compress: true
        });

        const pageW = 210;
        const pageH = 297;
        const margin = 14;
        const contentW = pageW - margin * 2;

        const colors = {
            navy: [15, 23, 42],
            blue: [30, 58, 138],
            lightBlue: [239, 246, 255],
            sky: [56, 189, 248],
            yellow: [250, 204, 21],
            green: [34, 197, 94],
            orange: [249, 115, 22],
            text: [15, 23, 42],
            muted: [100, 116, 139],
            border: [203, 213, 225],
            soft: [248, 250, 252],
            white: [255, 255, 255]
        };

        const setColor = (rgb, type = "text") => {
            if (type === "fill") doc.setFillColor(rgb[0], rgb[1], rgb[2]);
            else if (type === "draw") doc.setDrawColor(rgb[0], rgb[1], rgb[2]);
            else doc.setTextColor(rgb[0], rgb[1], rgb[2]);
        };

        const text = (value, x, y, options = {}) => {
            const size = options.size || 10;
            const style = options.style || "normal";
            const color = options.color || colors.text;
            const maxWidth = options.maxWidth;
            const lineHeight = options.lineHeight || size * 0.42;

            doc.setFont("helvetica", style);
            doc.setFontSize(size);
            setColor(color, "text");

            const safeValue = String(value ?? "");

            if (maxWidth) {
                const lines = doc.splitTextToSize(safeValue, maxWidth);
                doc.text(lines, x, y);
                return y + Math.max(lines.length, 1) * lineHeight;
            }

            doc.text(safeValue, x, y, options.align ? { align: options.align } : undefined);
            return y + lineHeight;
        };

            const sectionTitle = (label, y) => {
                text(label.toUpperCase(), margin, y, {
                    size: 8,
                    style: "bold",
                    color: colors.muted
                });

                setColor([226, 232, 240], "draw");
                doc.line(margin, y + 2.8, pageW - margin, y + 2.8);

                return y + 7;
            };

        const roundedFill = (x, y, w, h, radius, fillColor, drawColor = null) => {
            setColor(fillColor, "fill");
            if (drawColor) {
                setColor(drawColor, "draw");
                doc.roundedRect(x, y, w, h, radius, radius, "FD");
            } else {
                doc.roundedRect(x, y, w, h, radius, radius, "F");
            }
        };

        const drawHeader = () => {
            roundedFill(margin, 14, contentW, 42, 4, colors.navy);

            text("LEGALAPP", margin + 7, 25, {
                size: 11,
                style: "bold",
                color: colors.white
            });

            text("Reporte ejecutivo", margin + 7, 34, {
                size: 18,
                style: "bold",
                color: colors.white
            });

            text("operacional", margin + 7, 43, {
                size: 18,
                style: "bold",
                color: colors.white
            });

            text("Gestión legal · control operativo · monitoreo del entorno demo", margin + 7, 51, {
                size: 8.5,
                color: [226, 232, 240]
            });

            text("Fecha de generación", pageW - margin - 7, 25, {                size: 8,
                color: [203, 213, 225],
                align: "right"
            });

            text(data.fechaGeneracion, pageW - margin - 7, 33, {
                size: 10,
                style: "bold",
                color: colors.white,
                align: "right"
            });

            text("Estado general", pageW - margin - 7, 41, {
                size: 8,
                color: [203, 213, 225],
                align: "right"
            });

            roundedFill(pageW - margin - 64, 44, 57, 9.5, 4.5, colors.blue, [90, 110, 180]);
            text(data.estadoGeneral, pageW - margin - 35.5, 50.5, {
                size: 7.6,
                style: "bold",
                color: colors.white,
                align: "center"
            });

            };

            const drawKpiCard = (x, y, w, h, title, value, fillColor, valueColor = colors.text) => {
                roundedFill(x, y, w, h, 4, fillColor);
                text(title.toUpperCase(), x + 5, y + 9, {
                    size: 8,
                    color: fillColor === colors.navy ? [226, 232, 240] : colors.text
                });
                text(value, x + 5, y + 24, {
                    size: 22,
                    style: "bold",
                    color: valueColor
                });
            };


            const drawSignalCard = (x, y, w, title, body, accentColor, bgColor, textColor) => {
                const h = 36;

                roundedFill(x, y, w, h, 3.5, bgColor, [220, 230, 240]);
                setColor(accentColor, "fill");
                doc.roundedRect(x, y, 1.8, h, 1, 1, "F");

                text(title.toUpperCase(), x + 5, y + 8, {
                    size: 7.2,
                    style: "bold",
                    color: accentColor
                });

                text(body, x + 5, y + 16, {
                    size: 8.4,
                    style: "bold",
                    color: textColor,
                    maxWidth: w - 10,
                    lineHeight: 4.1
                });
            };

        const drawProgress = (label, valueText, pct, y, color) => {
            text(label, margin, y, {
                size: 10,
                color: colors.text
            });

            text(valueText, pageW - margin, y, {
                size: 10,
                style: "bold",
                color: colors.text,
                align: "right"
            });

            setColor([226, 232, 240], "fill");
            doc.roundedRect(margin, y + 4, contentW, 4, 2, 2, "F");

            setColor(color, "fill");
            doc.roundedRect(margin, y + 4, Math.max(4, contentW * Math.max(0, Math.min(100, pct)) / 100), 4, 2, 2, "F");

            return y + 15;
        };

            const drawFooter = () => {
                setColor(colors.border, "draw");
                doc.line(margin, pageH - 22, pageW - margin, pageH - 22);

                text("Documento generado automáticamente desde el dashboard de LegalApp.", pageW / 2, pageH - 15, {
                    size: 7.8,
                    color: colors.muted,
                    align: "center"
                });

                text("Entorno demo orientado a presentación ejecutiva y validación comercial.", pageW / 2, pageH - 10, {
                    size: 7.8,
                    color: colors.muted,
                    align: "center"
                });
            };

        const drawPage1 = () => {
            doc.setFillColor(255, 255, 255);
            doc.rect(0, 0, pageW, pageH, "F");

            drawHeader();

            let y = 68;

            y = sectionTitle("Lectura ejecutiva rápida", y);
            roundedFill(margin, y, contentW, 26, 3.5, colors.soft, colors.border);
            text(data.mensajeClave, margin + 5, y + 11, {
                size: 12,
                style: "bold",
                color: colors.text,
                maxWidth: contentW - 10,
                lineHeight: 5.5
            });

            y += 40;

            y = sectionTitle("Resumen ejecutivo", y);
            roundedFill(margin, y, contentW, 30, 3.5, colors.soft);
            setColor(colors.navy, "fill");
            doc.roundedRect(margin, y, 1.7, 30, 0.8, 0.8, "F");

            text(data.resumen, margin + 6, y + 9, {
                size: 9.5,
                color: [30, 41, 59],
                maxWidth: contentW - 12,
                lineHeight: 5
            });

            y += 44;

            y = sectionTitle("Indicadores clave", y);

            const gap = 6;
            const cardW = (contentW - gap) / 2;
            const cardH = 32;

            drawKpiCard(margin, y, cardW, cardH, "Total de casos", data.total, colors.navy, colors.white);
            drawKpiCard(margin + cardW + gap, y, cardW, cardH, "Pendientes", data.pendientes, colors.yellow, colors.text);

            y += cardH + 6;

            drawKpiCard(margin, y, cardW, cardH, "En proceso", data.enProceso, colors.sky, colors.text);
            drawKpiCard(margin + cardW + gap, y, cardW, cardH, "Tasa de cierre", `${data.tasaCierre}%`, colors.green, colors.text);

            y += cardH + 10;

            y = sectionTitle("Señales ejecutivas", y);
            const signalGap = 5;
            const signalW = (contentW - signalGap * 2) / 3;

            drawSignalCard(
                margin,
                y,
                signalW,
                "Estado",
                data.estadoGeneral,
                colors.orange,
                [255, 247, 237],
                [124, 45, 18]
            );

            drawSignalCard(
                margin + signalW + signalGap,
                y,
                signalW,
                "Fortaleza",
                data.fortalezaClave,
                [37, 99, 235],
                [239, 246, 255],
                [30, 58, 138]
            );

                drawSignalCard(
                    margin + (signalW + signalGap) * 2,
                    y,
                    signalW,
                    "Prioridad",
                    data.prioridadClave,
                    [22, 163, 74],
                    [240, 253, 244],
                    [22, 101, 52]
                );

                };

        const drawGovernanceTable = (y) => {
            const rowH = 10;
            const col1 = contentW * 0.72;
            const col2 = contentW - col1;

            setColor([226, 232, 240], "fill");
            setColor(colors.border, "draw");
            doc.roundedRect(margin, y, contentW, rowH, 2.5, 2.5, "FD");

            text("Indicador", margin + 3, y + 6.7, {
                size: 8.5,
                style: "bold",
                color: colors.text
            });

            text("Valor", margin + col1 + 3, y + 6.7, {
                size: 8.5,
                style: "bold",
                color: colors.text
            });

            y += rowH;

            const rows = [
                ["Usuarios totales", data.usuariosTotal],
                ["Usuarios protegidos", data.usuariosProtegidos],
                ["Perfiles distintos", data.perfilesDistintos],
                ["Usuarios con perfil", data.usuariosConPerfil],
                ["Asignaciones de rol", data.rolesAsignados],
                ["Roles disponibles", data.rolesDisponibles]
            ];

            rows.forEach(([label, value]) => {
                setColor(colors.white, "fill");
                setColor(colors.border, "draw");
                doc.rect(margin, y, col1, rowH, "FD");
                doc.rect(margin + col1, y, col2, rowH, "FD");

                text(label, margin + 3, y + 6.7, {
                    size: 8.8,
                    color: colors.text
                });

                text(value, margin + col1 + 3, y + 6.7, {
                    size: 8.8,
                    color: colors.text
                });

                y += rowH;
            });

            return y + 8;
        };

        const drawPage2 = () => {
            doc.addPage();
            doc.setFillColor(255, 255, 255);
            doc.rect(0, 0, pageW, pageH, "F");

            let y = 18;

            y = sectionTitle("Distribución operativa", y);
            y = drawProgress("Pendientes", `${data.pendientes} (${data.pctPendientes}%)`, data.pctPendientes, y, [245, 158, 11]);
            y = drawProgress("En proceso", `${data.enProceso} (${data.pctProceso}%)`, data.pctProceso, y, [14, 165, 233]);
            y = drawProgress("Cerrados", `${data.cerrados} (${data.pctCerrados}%)`, data.pctCerrados, y, [22, 163, 74]);

            y += 2;

            if (data.incluirGobernanza) {
                y = sectionTitle("Gobernanza y administración", y);
                y = drawGovernanceTable(y);
            }

            y = sectionTitle("Hallazgos principales", y);

            const hallazgos = Array.isArray(data.hallazgos) ? data.hallazgos : [];
            hallazgos.slice(0, 3).forEach((item) => {
                text(`• ${item}`, margin + 3, y, {
                    size: 10,
                    color: colors.text,
                    maxWidth: contentW - 6,
                    lineHeight: 5.5
                });
                const lines = doc.splitTextToSize(`• ${item}`, contentW - 6);
                y += Math.max(lines.length, 1) * 5.5 + 1;
            });

            y += 8;

            roundedFill(margin, y, contentW, 34, 4, colors.lightBlue, [191, 219, 254]);
            text("RECOMENDACIÓN EJECUTIVA", margin + 5, y + 8, {
                size: 8.2,
                style: "bold",
                color: [37, 99, 235]
            });

            text(data.recomendacion, margin + 5, y + 17, {
                size: 11.2,
                style: "bold",
                color: [30, 58, 138],
                maxWidth: contentW - 10,
                lineHeight: 5.7
            });

            drawFooter();
        };

        drawPage1();
        drawPage2();

        doc.save(`legalapp_reporte_operativo_${new Date().toISOString().slice(0, 10)}.pdf`);

    } catch (error) {
        console.error("❌ Error exportando PDF:", error);

        if (window.Swal?.fire) {
            window.Swal.fire({
                icon: "error",
                title: "No fue posible generar el PDF",
                text: error?.message || "Ocurrió un problema durante la exportación."
            });
        } else {
            alert(error?.message || "Ocurrió un problema durante la exportación.");
        }
    } finally {
        setEstadoBotonPDF(btn, false);
    }
}

function initPdfExport() {
    const btn = document.getElementById("btnExportPDF");

    if (!btn) {
        console.warn("⚠️ Botón PDF no encontrado");
        return;
    }

    if (btn.dataset.pdfReady === "true") return;

    btn.dataset.pdfReady = "true";
    btn.addEventListener("click", exportarReportePDF);
}

      
   



