// Esperamos a que cargue todo el HTML antes de tocar elementos del DOM
document.addEventListener("DOMContentLoaded", () => {
    // --- Nombre del usuario y protección de sesión ---
    // Buscar los datos que guardamos en localStorage al iniciar sesión
    const usuarioGuardado = localStorage.getItem("usuarioLogueado");

    if (usuarioGuardado) {
        try {
            // Convertimos el texto guardado de vuelta a objeto para leer su nombre
            const usuario = JSON.parse(usuarioGuardado);
            const spanNombre = document.getElementById("nombreUsuario");

            // Solo reemplazamos el texto si existe el span y viene el nombre
            if (spanNombre && usuario.nombre) {
                spanNombre.textContent = usuario.nombre;
            }
        } catch (error) {
            console.error("Error al leer los datos del usuario desde localStorage:", error);
        }
    } else {
        // Si no hay sesión activa, mandamos directo al login
        window.location.href = "login.html";
    }

    // --- Menú lateral ---
    const sidebar = document.getElementById('sidebar');
    const overlay = document.getElementById('sidebarOverlay');
    const openBtn = document.getElementById('openSidebar');
    const closeBtn = document.getElementById('closeSidebar');

    function toggleMenu() {
        if (sidebar && overlay) {
            sidebar.classList.toggle('translate-x-full');
            overlay.classList.toggle('hidden');
        }
    }

    if (openBtn) openBtn.addEventListener('click', toggleMenu);
    if (closeBtn) closeBtn.addEventListener('click', toggleMenu);
    if (overlay) overlay.addEventListener('click', toggleMenu);

    function cerrarSesion() {
        // Limpiamos la sesión guardada y sacamos al usuario al login sin dejar historial
        localStorage.removeItem("usuarioLogueado");
        window.location.replace("login.html");
    }

    const btnCerrarSesionLateral = document.getElementById('btnCerrarSesionLateral');
    if (btnCerrarSesionLateral) {
        btnCerrarSesionLateral.addEventListener('click', cerrarSesion);
    }

    const btnCerrarSesionMovil = document.getElementById('btnCerrarSesion');
    if (btnCerrarSesionMovil) {
        btnCerrarSesionMovil.addEventListener('click', cerrarSesion);
    }
});