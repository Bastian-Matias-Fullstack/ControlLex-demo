function setFieldError(inputId, message) {
    const input = document.getElementById(inputId);
    if (!input) return;

    input.classList.add("is-invalid");
    input.classList.remove("is-valid");

    const feedback = input.parentElement?.querySelector(".invalid-feedback");
    if (feedback) feedback.textContent = message ?? "Campo inválido";
}

function clearFieldError(inputId) {
    const input = document.getElementById(inputId);
    if (!input) return;

    input.classList.remove("is-invalid");
    const feedback = input.parentElement?.querySelector(".invalid-feedback");
    if (feedback) feedback.textContent = "";
}


function clearUserFormErrors() {
    ["nombreUsuario", "emailUsuario", "password"].forEach(clearFieldError);
}

function actualizarMetricasUsuarios(usuarios) {
    const lista = Array.isArray(usuarios) ? usuarios : [];

    const total = lista.length;

    const protegidos = lista.filter(usuario => {
        const nombre = (usuario?.nombre || "").toLowerCase();
        const email = (usuario?.email || "").toLowerCase();

        return nombre === "admin"
            || nombre === "soporte"
            || nombre === "abogado"
            || email === "admin@legal.cl"
            || email === "soporte@legal.cl"
            || email === "abogado@legal.cl";
    }).length;

    const perfilesSet = new Set();

    lista.forEach(usuario => {
        const roles = usuario?.roles;

        if (Array.isArray(roles)) {
            roles.forEach(r => {
                const nombreRol = typeof r === "string" ? r : r?.nombre;
                if (nombreRol) perfilesSet.add(String(nombreRol).trim());
            });
        } else if (typeof roles === "string" && roles.trim()) {
            perfilesSet.add(roles.trim());
        }
    });

    window.legalAppMetricas.usuarios.total = total;
    window.legalAppMetricas.usuarios.protegidos = protegidos;
    window.legalAppMetricas.usuarios.perfiles = perfilesSet.size;

    window.recalcularMetricasUI?.();
}
// usuarios.js
// Este módulo carga, crea, edita y elimina usuarios desde el backend usando JWT
// Punto de entrada del módulo, se llama desde dashboard.html
function initUsuariosModule() {
        document.getElementById("seccion-usuarios")?.classList.remove("d-none");
    if (!window.__usuariosInitialized) {
    window.__usuariosInitialized = true;
    configurarEventosUsuarios();
  }
    cargarUsuarios();
}
window.initUsuariosModule = initUsuariosModule;
// =========================================================
// CAPA DEFENSIVA USUARIOS - ANTI CLIC / ANTI REQUEST PARALELA
// Objetivo: no cambiar el flujo actual; solo evitar submits/deletes
// duplicados cuando la API/BD esté lenta o haya clics rápidos.
// =========================================================
let guardandoUsuario = false;
const accionesUsuarioEnCurso = new Set();

function iniciarAccionUsuario(clave) {
    if (accionesUsuarioEnCurso.has(clave)) return false;
    accionesUsuarioEnCurso.add(clave);
    return true;
}

function finalizarAccionUsuario(clave) {
    accionesUsuarioEnCurso.delete(clave);
}

function setBotonUsuarioCargando(btn, cargando, htmlCargando = null) {
    if (!btn) return null;

    if (cargando) {
        const htmlOriginal = btn.innerHTML;
        btn.disabled = true;
        btn.classList.add("disabled");
        btn.setAttribute("aria-busy", "true");

        if (htmlCargando) {
            btn.innerHTML = htmlCargando;
        }

        return htmlOriginal;
    }

    btn.disabled = false;
    btn.classList.remove("disabled");
    btn.removeAttribute("aria-busy");
    return null;
}
// 🧠 Configura los listeners para botones y formularios
function configurarEventosUsuarios() {
    document.getElementById("btnNuevoUsuario").addEventListener("click", () => {
        abrirModalUsuario();   // Abre el modal vacío para crear usuario
    });
    document.getElementById("formUsuario").addEventListener("submit", async (e) => {
        e.preventDefault();
        await guardarUsuario(); // Enviar datos al backend (POST o PUT)
    });
     // ✅ UX pro: limpiar error al escribir
    ["nombreUsuario", "emailUsuario", "password"].forEach(id => {
    const input = document.getElementById(id);
        if (!input) return;

        input.addEventListener("input", () => {
            clearFieldError(id);
        });
    });
}
// 🔄 Fetch: obtener todos los usuarios
async function cargarUsuarios() {
    try {
        const response = await fetch("/api/usuarios", {
            headers: authHeader()
        });
        if (!response.ok) {
            console.error("Error HTTP:", response.status);
            return;
        }
        const text = await response.text();

        if (!text) {
            console.warn("Respuesta vacía del servidor");
            return;
        }
        const usuarios = JSON.parse(text);
        renderizarUsuarios(usuarios);
        renderizarCardsUsuarios(usuarios);
        actualizarMetricasUsuarios(usuarios);
    } catch (error) {
        console.error("Error al cargar usuarios:", error);
    }
}
// 🧱 Renderiza usuarios en la tabla HTML
function renderizarUsuarios(usuarios) {
    const tbody = document.getElementById("usuariosBody");
    tbody.innerHTML = "";

    usuarios.forEach(usuario => {
        const tr = document.createElement("tr");

        tr.append(
            crearCeldaUsuario(usuario.id),
            crearCeldaNombreUsuario(usuario.nombre, usuario.esDemoProtegido),
            crearCeldaUsuario(usuario.email),
            crearCeldaRolesTabla(usuario.roles)
        );

        const acciones = crearCeldaUsuario();
        agregarAccionesUsuario(acciones, usuario, false);
        tr.appendChild(acciones);

        tbody.appendChild(tr);
    });
}
function renderizarCardsUsuarios(usuarios) {
    const cardsContainer = document.getElementById("usuariosCards");
    if (!cardsContainer) return;

    cardsContainer.innerHTML = "";

    usuarios.forEach(usuario => {
        const card = document.createElement("article");
        card.className = "usuario-card";
        card.dataset.id = textoSeguroUsuario(usuario.id);

        const header = document.createElement("div");
        header.className = "usuario-card-header";
        const titleWrap = document.createElement("div");
        titleWrap.className = "usuario-card-title-wrap";
        const id = document.createElement("span");
        id.className = "usuario-card-id";
        id.textContent = `#${textoSeguroUsuario(usuario.id)}`;
        const nombre = document.createElement("h6");
        nombre.className = "usuario-card-title mb-1";
        nombre.textContent = textoSeguroUsuario(usuario.nombre);
        if (usuario.esDemoProtegido) nombre.appendChild(crearBadgeUsuario("badge text-bg-warning ms-2", "Protegido"));
        const email = document.createElement("div");
        email.className = "usuario-card-email";
        email.textContent = textoSeguroUsuario(usuario.email);
        titleWrap.append(id, nombre, email);
        header.appendChild(titleWrap);

        const body = document.createElement("div");
        body.className = "usuario-card-body";
        const filaRoles = document.createElement("div");
        filaRoles.className = "usuario-card-row";
        const labelRoles = document.createElement("span");
        labelRoles.className = "usuario-card-label";
        labelRoles.textContent = "Roles";
        const roles = document.createElement("div");
        roles.className = "usuario-card-value";
        agregarRolesUsuario(roles, usuario.roles, "badge bg-info-subtle text-info-emphasis me-1 mb-1", "badge bg-secondary-subtle text-secondary-emphasis", "Sin rol");
        filaRoles.append(labelRoles, roles);
        body.appendChild(filaRoles);

        const acciones = document.createElement("div");
        acciones.className = "usuario-card-actions";
        agregarAccionesUsuario(acciones, usuario, true);

        card.append(header, body, acciones);
        cardsContainer.appendChild(card);
    });
}

function textoSeguroUsuario(valor, fallback = "") {
    return valor === null || valor === undefined ? fallback : String(valor);
}

function crearCeldaUsuario(valor = null) {
    const celda = document.createElement("td");
    if (valor !== null) celda.textContent = textoSeguroUsuario(valor);
    return celda;
}

function crearBadgeUsuario(clases, texto) {
    const badge = document.createElement("span");
    badge.className = clases;
    badge.textContent = texto;
    return badge;
}

function crearCeldaNombreUsuario(nombre, esDemoProtegido) {
    const celda = crearCeldaUsuario(nombre);
    if (esDemoProtegido) celda.appendChild(crearBadgeUsuario("badge text-bg-warning ms-2", "Protegido"));
    return celda;
}

function agregarRolesUsuario(contenedor, roles, clasesBadge, clasesVacio, textoVacio) {
    const rolesSeguros = Array.isArray(roles) ? roles : [];
    if (rolesSeguros.length === 0) {
        if (clasesVacio) contenedor.appendChild(crearBadgeUsuario(clasesVacio, textoVacio));
        else contenedor.textContent = textoVacio;
        return;
    }

    rolesSeguros.forEach(rol => contenedor.appendChild(crearBadgeUsuario(clasesBadge, textoSeguroUsuario(rol))));
}

function crearCeldaRolesTabla(roles) {
    const celda = crearCeldaUsuario();
    agregarRolesUsuario(celda, roles, "badge text-bg-primary me-1", "", "-");
    return celda;
}

function crearBotonUsuario(clases, titulo, icono, deshabilitado, esCard) {
    const boton = document.createElement("button");
    boton.type = "button";
    boton.className = clases;
    boton.disabled = deshabilitado;
    if (titulo) boton.title = titulo;

    if (esCard) {
        const iconoElemento = document.createElement("i");
        iconoElemento.className = `bi ${icono}`;
        boton.appendChild(iconoElemento);
    } else {
        boton.textContent = icono;
    }

    return boton;
}

function agregarAccionesUsuario(contenedor, usuario, esCard) {
    const protegido = !!usuario.esDemoProtegido;
    const editar = crearBotonUsuario(
        esCard
            ? (protegido ? "btn btn-sm btn-outline-secondary" : "btn btn-sm btn-outline-light btn-editar-usuario")
            : (protegido ? "btn btn-sm btn-outline-secondary me-1" : "btn btn-sm btn-outline-light me-1"),
        protegido ? "Usuario demo protegido" : (esCard ? "Editar" : ""),
        esCard ? "bi-pencil-fill" : "✏️",
        protegido,
        esCard
    );
    const eliminar = crearBotonUsuario(
        protegido ? "btn btn-sm btn-outline-secondary" : "btn btn-sm btn-outline-danger btn-eliminar-usuario",
        protegido ? "Usuario demo protegido" : (esCard ? "Eliminar" : ""),
        esCard ? "bi-trash-fill" : "🗑️",
        protegido,
        esCard
    );

    if (!protegido) {
        editar.addEventListener("click", () => abrirModalUsuario(usuario));
        eliminar.addEventListener("click", () => eliminarUsuario(usuario.id, false));
    }

    contenedor.append(editar, eliminar);
}
function inicializarTogglePasswordUsuario() {
    const passwordInput = document.getElementById("password");
    const togglePasswordBtn = document.getElementById("togglePassword");
    const eyeIcon = togglePasswordBtn?.querySelector(".eye-icon");

    if (!passwordInput || !togglePasswordBtn) return;

    const showPassword = () => {
        passwordInput.type = "text";
        togglePasswordBtn.setAttribute("aria-label", "Soltar para ocultar contraseña");
        togglePasswordBtn.setAttribute("title", "Soltar para ocultar contraseña");
        if (eyeIcon) eyeIcon.textContent = "👁";
    };

    const hidePassword = () => {
        passwordInput.type = "password";
        togglePasswordBtn.setAttribute("aria-label", "Mantener presionado para ver contraseña");
        togglePasswordBtn.setAttribute("title", "Mantener presionado para ver contraseña");
        if (eyeIcon) eyeIcon.textContent = "◉";
    };

    togglePasswordBtn.onmousedown = showPassword;
    togglePasswordBtn.onmouseup = hidePassword;
    togglePasswordBtn.onmouseleave = hidePassword;

    togglePasswordBtn.ontouchstart = (e) => {
        e.preventDefault();
        showPassword();
    };
    togglePasswordBtn.ontouchend = hidePassword;
    togglePasswordBtn.ontouchcancel = hidePassword;

    togglePasswordBtn.onkeydown = (e) => {
        if (e.key === " " || e.key === "Enter") {
            e.preventDefault();
            showPassword();
        }
    };

    togglePasswordBtn.onkeyup = (e) => {
        if (e.key === " " || e.key === "Enter") {
            e.preventDefault();
            hidePassword();
        }
    };

    hidePassword();
}
// ✏ Abre el modal con datos (si hay) o limpio para nuevo
function abrirModalUsuario(usuario = null) {
    if (usuario?.esDemoProtegido) {
        Swal.fire({
            icon: "info",
            title: "Usuario protegido",
            text: "Este usuario forma parte del entorno de demostración y no puede modificarse."
        });
        return;
    }

    clearUserFormErrors();
    document.getElementById("formUsuario").reset();
    document.getElementById("usuarioId").value = usuario?.id || "";
    document.getElementById("nombreUsuario").value = usuario?.nombre || "";
    document.getElementById("emailUsuario").value = usuario?.email || "";
    const passwordInput = document.getElementById("password");
    if (passwordInput) {
        passwordInput.required = !usuario;
        passwordInput.value = "";
        passwordInput.type = "password";
    }

    inicializarTogglePasswordUsuario();
    const modalEl = document.getElementById("modalUsuario");
    const modal = bootstrap.Modal.getOrCreateInstance(modalEl); 
    modal.show();
}
// 💾 Guarda o actualiza un usuario (POST o PUT)
async function guardarUsuario() {
    if (guardandoUsuario) return;
    guardandoUsuario = true;

    const id = (document.getElementById("usuarioId").value ?? "").trim();
    const nombre = (document.getElementById("nombreUsuario").value ?? "").trim();
    const email = (document.getElementById("emailUsuario").value ?? "").trim();
    const password = (document.getElementById("password").value ?? "").trim();
    const esNuevo = !id;

    const btnSubmit = document.querySelector('button[type="submit"][form="formUsuario"]');
    const originalHtml = btnSubmit?.innerHTML;

    try {
        // Validaciones inline
        clearUserFormErrors();

        const nombreRegex = /^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$/;
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

        if (!nombre) {
            setFieldError("nombreUsuario", "El nombre es obligatorio.");
            document.getElementById("nombreUsuario").focus();
            return;
        }

        if (nombre.length < 3) {
            setFieldError("nombreUsuario", "El nombre debe tener al menos 3 caracteres.");
            document.getElementById("nombreUsuario").focus();
            return;
        }

        if (!nombreRegex.test(nombre)) {
            setFieldError("nombreUsuario", "El nombre solo puede contener letras y espacios.");
            document.getElementById("nombreUsuario").focus();
            return;
        }

        if (!email) {
            setFieldError("emailUsuario", "El email es obligatorio.");
            document.getElementById("emailUsuario").focus();
            return;
        }

        if (!emailRegex.test(email)) {
            setFieldError("emailUsuario", "Email inválido.");
            document.getElementById("emailUsuario").focus();
            return;
        }

        if (esNuevo) {
            if (!password) {
                setFieldError("password", "La contraseña es obligatoria.");
                document.getElementById("password").focus();
                return;
            }

            if (password.length < 6) {
                setFieldError("password", "La contraseña debe tener al menos 6 caracteres.");
                document.getElementById("password").focus();
                return;
            }
        } else {
            if (password && password.length < 6) {
                setFieldError("password", "La contraseña debe tener al menos 6 caracteres.");
                document.getElementById("password").focus();
                return;
            }
        }

        if (!btnSubmit) {
            console.warn("No se encontró el botón submit del formulario formUsuario.");
            return;
        }

        setBotonUsuarioCargando(
            btnSubmit,
            true,
            '<span class="spinner-border spinner-border-sm me-2"></span>Guardando...'
        );

        const payload = { nombre, email };

        if (password && password.trim() !== "") {
            payload.password = password.trim();
        }

        const url = id ? `/api/usuarios/${id}` : "/api/usuarios";
        const method = id ? "PUT" : "POST";

        const res = await fetch(url, {
            method,
            headers: {
                ...authHeader(),
                "Content-Type": "application/json"
            },
            body: JSON.stringify(payload)
        });

        if (!res.ok) {
            let mensaje = "Error al guardar usuario";

            try {
                const json = await res.json();
                mensaje = json.detail || json.title || mensaje;
            } catch {
                // Si no viene JSON, usamos mensaje por defecto.
            }

            if (res.status === 429) {
                mensaje = "Demasiadas solicitudes. Espera unos segundos e intenta nuevamente.";
            }

            throw new Error(mensaje);
        }

        Swal.fire("Éxito", id ? "Usuario actualizado" : "Usuario creado", "success");

        const modalEl = document.getElementById("modalUsuario");
        bootstrap.Modal.getOrCreateInstance(modalEl).hide();

        await cargarUsuarios();
        window.refrescarUsuariosEnRoles?.();

    } catch (error) {
        console.error(error);
        Swal.fire("Error", error.message || "No se pudo guardar el usuario.", "error");

    } finally {
        if (btnSubmit) {
            setBotonUsuarioCargando(btnSubmit, false);
            if (originalHtml !== null && originalHtml !== undefined) {
                btnSubmit.innerHTML = originalHtml;
            }
        }

        guardandoUsuario = false;
    }
}
// ❌ Elimina un usuario después de confirmar con SweetAlert2
async function eliminarUsuario(id, esDemoProtegido = false) {
    if (esDemoProtegido) {
        Swal.fire({
            icon: "info",
            title: "Usuario protegido",
            text: "Este usuario forma parte del entorno de demostración y no puede eliminarse."
        });
        return;
    }

    if (!id) return;

    const claveAccion = `eliminar:${id}`;
    if (!iniciarAccionUsuario(claveAccion)) return;

    try {
        const confirm = await Swal.fire({
            title: "¿Eliminar usuario?",
            text: "Esta acción no se puede deshacer",
            icon: "warning",
            showCancelButton: true,
            confirmButtonText: "Sí, eliminar",
            cancelButtonText: "Cancelar"
        });

        if (!confirm.isConfirmed) return;

        Swal.fire({
            title: "Eliminando usuario...",
            allowOutsideClick: false,
            didOpen: () => Swal.showLoading()
        });

        const res = await fetch(`/api/usuarios/${id}`, {
            method: "DELETE",
            headers: authHeader()
        });

        if (!res.ok) {
            let mensaje = "Error al eliminar usuario";

            try {
                const json = await res.json();
                mensaje = json.detail || json.title || mensaje;
            } catch {
                // Si no viene JSON, usamos mensaje por defecto.
            }

            if (res.status === 429) {
                mensaje = "Demasiadas solicitudes. Espera unos segundos e intenta nuevamente.";
            }

            throw new Error(mensaje);
        }

        Swal.fire("Eliminado", "El usuario fue eliminado", "success");

        await cargarUsuarios();
        window.refrescarUsuariosEnRoles?.();

    } catch (error) {
        console.error(error);
        Swal.fire("Error", error.message || "No se pudo eliminar el usuario.", "error");

    } finally {
        finalizarAccionUsuario(claveAccion);
    }
}
// 🔐 Adjunta el JWT al header Authorization
function authHeader() {
    const token = localStorage.getItem("jwt_token");
    return {
        "Authorization": `Bearer ${token}`
    };
}
// 🔄 Exportar función de arranque para dashboard.html
window.initUsuariosModule = initUsuariosModule;
