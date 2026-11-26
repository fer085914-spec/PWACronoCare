// Clave con la que guardamos los datos del usuario en sessionStorage
const SESSION_KEY = "empleados_usuario";

// Lee y parsea el usuario guardado
function obtenerUsuario() {
    const valor = sessionStorage.getItem(SESSION_KEY);
    try {
        return valor ? JSON.parse(valor) : null;
    } catch {
        sessionStorage.removeItem(SESSION_KEY);
        return null;
    }
}

// Bloquea el acceso a vistas privadas: si no hay sesión activa, manda directo al login
function protegerPagina() {
    if (!obtenerUsuario()) window.location.replace("login.html");
}

// Pinta el nombre del usuario en el elemento del DOM que le indiquemos
function mostrarUsuario(elementId) {
    const usuario = obtenerUsuario();
    const elemento = document.getElementById(elementId);
    if (usuario && elemento) elemento.textContent = usuario.nombre;
}

// Borra la sesión actual y redirige a login.html sin dejar esta vista en el historial
function cerrarSesion() {
    sessionStorage.removeItem(SESSION_KEY);
    window.location.replace("login.html");
}