function mostrarError(texto) {
    const mensajeError = document.getElementById("mensajeError");
    mensajeError.innerText = texto;
    mensajeError.style.display = "block";
    mensajeError.classList.remove("shake"); // Reiniciar animación
    void mensajeError.offsetWidth;          // Trigger reflow
    mensajeError.classList.add("shake");    // Aplicar animación
}

if (window.matchMedia?.("(prefers-reduced-motion: reduce)").matches) {
    document.getElementById("bg-video")?.pause();
}
// ===============================
// Demo Context Handling (SE2)
// ===============================
function getDemoContextFromUrl() {
    const params = new URLSearchParams(window.location.search);
    const demo = (params.get("demo") || "").toLowerCase();

    const allowed = ["casos", "roles", "usuarios"];
    return allowed.includes(demo) ? demo : null;
}

const demoContext = getDemoContextFromUrl();

if (demoContext) {
    sessionStorage.setItem("demoContext", demoContext);
} else {
    sessionStorage.removeItem("demoContext");
}
function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

let warmupPromise = null;
let warmupReady = false;

async function ensureBackendReady(force = false) {
    if (warmupReady && !force) return true;
    if (warmupPromise && !force) return warmupPromise;

    warmupPromise = (async () => {
        for (let attempt = 1; attempt <= 3; attempt++) {
            try {
                const response = await fetch(`/api/system/warmup?timeoutMs=25000`, {
                    method: "GET",
                    cache: "no-store"
                });

                if (response.ok) {
                    warmupReady = true;
                    return true;
                }
            } catch (_) {
                // silencioso a propósito
            }

            await sleep(1200 * attempt);
        }

        return false;
    })();

    try {
        return await warmupPromise;
    } finally {
        if (!warmupReady) {
            warmupPromise = null;
        }
    }
}
const passwordInput = document.getElementById("password");
const togglePasswordBtn = document.getElementById("togglePassword");
const eyeIcon = togglePasswordBtn?.querySelector(".eye-icon");

if (passwordInput && togglePasswordBtn) {
    const showPassword = () => {
        passwordInput.type = "text";
        togglePasswordBtn.setAttribute("aria-label", "Soltar para ocultar contraseña");
        togglePasswordBtn.setAttribute("title", "Soltar para ocultar contraseña");
        if (eyeIcon) eyeIcon.textContent = "◎";
    };

    const hidePassword = () => {
        passwordInput.type = "password";
        togglePasswordBtn.setAttribute("aria-label", "Mantener presionado para ver contraseña");
        togglePasswordBtn.setAttribute("title", "Mantener presionado para ver contraseña");
        if (eyeIcon) eyeIcon.textContent = "◉";
    };

    togglePasswordBtn.addEventListener("mousedown", showPassword);
    togglePasswordBtn.addEventListener("mouseup", hidePassword);
    togglePasswordBtn.addEventListener("mouseleave", hidePassword);

    togglePasswordBtn.addEventListener("touchstart", (e) => {
        e.preventDefault();
        showPassword();
    }, { passive: false });

    togglePasswordBtn.addEventListener("touchend", hidePassword);
    togglePasswordBtn.addEventListener("touchcancel", hidePassword);

    togglePasswordBtn.addEventListener("keydown", (e) => {
        if (e.code === "Space" || e.code === "Enter") {
            e.preventDefault();
            showPassword();
        }
    });

    togglePasswordBtn.addEventListener("keyup", (e) => {
        if (e.code === "Space" || e.code === "Enter") {
            e.preventDefault();
            hidePassword();
        }
    });
}
if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", () => {
        ensureBackendReady().catch(() => {
            // silencioso a propósito
        });
    });
} else {
    ensureBackendReady().catch(() => {
        // silencioso a propósito
    });
}

document.getElementById("loginForm").addEventListener("submit", async function (e) {
    e.preventDefault();

    const email = document.getElementById("email").value;
    const password = document.getElementById("password").value;
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

    if (!email || !password) {
        mostrarError("Completa todos los campos.");

        return;
    }

    if (!emailRegex.test(email)) {
        mostrarError("Ingresa un correo válido.");

        return;
    }

    const loginData = {
        email: email,
        password: password
    };

try {
    const ready = await ensureBackendReady();

    if (!ready) {
        mostrarError("La demo se está preparando. Intenta nuevamente en unos segundos.");
        return;
    }

    const response = await fetch("/api/Auth/login", {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify(loginData)
    });

    if (response.status === 401) {
        mostrarError("Correo o contraseña inválidos.");
        return;
    }

    if (response.status === 429) {
        const body = await response.json().catch(() => null);
        mostrarError(body?.message || "Demasiados intentos. Intenta nuevamente en unos segundos.");
        return;
    }

    if (!response.ok) {
        mostrarError("La demo se está preparando. Intenta nuevamente en unos segundos.");
        return;
    }

    const data = await response.json();
        if (!data.token) {
            mostrarError("No fue posible iniciar sesión.");
            return;
        }
        const token = data.token;

        // Decodificar manualmente
        const payloadBase64 = token.split('.')[1];
        const payloadJson = atob(payloadBase64);
        const payload = JSON.parse(payloadJson);

        const rawRoles = payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
        const roles = Array.isArray(rawRoles) ? rawRoles : (rawRoles ? [rawRoles] : []);

        if (roles.length === 0) {
            localStorage.removeItem("jwt_token");
            localStorage.removeItem("usuario_actual");

            mostrarError("Tu cuenta no tiene un rol asignado. No es posible ingresar a esta demo.");
            return;
        }

        const usuario = {
            email: payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"] || "",
            nombre: payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"] || "Usuario",
            roles: roles
        };

        localStorage.setItem("jwt_token", token);
        localStorage.setItem("usuario_actual", JSON.stringify(usuario));
        // 🔹 Leer el contexto demo que ya guardaste arriba (casos|roles|usuarios)
        const ctx = sessionStorage.getItem("demoContext");

        let hash = "";
        if (ctx === "casos") hash = "#mod-casos";
        else if (ctx === "roles") hash = "#mod-roles";
        else if (ctx === "usuarios") hash = "#mod-usuarios";
        window.location.replace("dashboard.html" + hash);

} catch (error) {
    console.error("Login error:", error);
    mostrarError("La demo se está preparando. Intenta nuevamente en unos segundos.");
}
});
// Ocultar mensaje de error al escribir nuevamente
document.getElementById("email").addEventListener("input", () => {
    document.getElementById("mensajeError").style.display = "none";
});
document.getElementById("password").addEventListener("input", () => {
    document.getElementById("mensajeError").style.display = "none";
});

