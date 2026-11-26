// Referencias a los campos y botones del formulario de registro
const formRegistro = document.getElementById("registerForm");
const nombre = document.getElementById("nombre");
const correo = document.getElementById("email");
const password = document.getElementById("password");
const confirmPassword = document.getElementById("confirmPassword");
const btnRegister = document.getElementById("btnRegister");
const mensajeRegistro = document.getElementById("mensajeRegistro");

// Elementos del modal para ingresar el código de verificación enviado al correo
const modalCodigo = document.getElementById("modalCodigo");
const btnCerrarModalCodigo = document.getElementById("btnCerrarModalCodigo");
const correoDestinoModal = document.getElementById("correoDestinoModal");
const codigoVerificacion = document.getElementById("codigoVerificacion");
const mensajeModalCodigo = document.getElementById("mensajeModalCodigo");
const btnConfirmarRegistro = document.getElementById("btnConfirmarRegistro");

// Elementos de éxito y variables de estado del flujo
const toastExitoRegistro = document.getElementById("toastExitoRegistro");
const textoToastExito = document.getElementById("textoToastExito");

let datosPendientesRegistro = null;
let temporizadorRedireccion = null;

// Mostrar el aviso de éxito y redirige al login tras unos segundos
function mostrarAlertaYRedirigir(mensaje, urlDestino = "login.html", duracionMs = 10000) {
    // Limpiar cualquier temporizador anterior para evitar redirecciones duplicadas
    if (temporizadorRedireccion) {
        clearTimeout(temporizadorRedireccion);
    }

    if (textoToastExito && mensaje) {
        textoToastExito.textContent = mensaje;
    }

    if (toastExitoRegistro) {
        toastExitoRegistro.classList.remove("hidden");
    }

    temporizadorRedireccion = setTimeout(() => {
        if (toastExitoRegistro) {
            toastExitoRegistro.classList.add("hidden");
        }
        window.location.replace(urlDestino);
    }, duracionMs);
}

// Limita el código a máximo 8 dígitos numéricos y habilita el botón solo cuando está completo
function actualizarEstadoBotonConfirmar() {
    const soloDigitos = (codigoVerificacion.value || "").replace(/\D/g, "").slice(0, 8);
    codigoVerificacion.value = soloDigitos;

    const tieneOchoDigitos = soloDigitos.length === 8;
    btnConfirmarRegistro.disabled = !tieneOchoDigitos;
    btnConfirmarRegistro.classList.toggle("opacity-50", !tieneOchoDigitos);
    btnConfirmarRegistro.classList.toggle("cursor-not-allowed", !tieneOchoDigitos);
}

// Abre el modal de verificación reseteando el campo y mensajes previos
function abrirModalCodigo(valorCorreo) {
    correoDestinoModal.textContent = valorCorreo;
    codigoVerificacion.value = "";
    mensajeModalCodigo.textContent = "";
    mensajeModalCodigo.classList.add("hidden");
    actualizarEstadoBotonConfirmar();

    modalCodigo.classList.remove("hidden");
    modalCodigo.classList.add("flex");
    codigoVerificacion.focus();
}

// Cerrar el modal y limpiar los datos que estaban en espera de confirmar
function cerrarModalCodigo() {
    modalCodigo.classList.add("hidden");
    modalCodigo.classList.remove("flex");
    datosPendientesRegistro = null;
}

// Al escribir en el código, oculta errores previos y actualiza el botón
codigoVerificacion.addEventListener("input", () => {
    mensajeModalCodigo.classList.add("hidden");
    actualizarEstadoBotonConfirmar();
});

btnCerrarModalCodigo.addEventListener("click", cerrarModalCodigo);

// valida los datos y solicita el envío del código por correo
formRegistro.addEventListener("submit", async (evento) => {
    evento.preventDefault();

    mensajeRegistro.classList.add("hidden");
    btnRegister.disabled = true;
    btnRegister.textContent = "ENVIANDO CÓDIGO...";
    btnRegister.classList.add("opacity-50", "cursor-not-allowed");

    const valorCorreo = correo.value.trim();
    const valorPassword = password.value;
    const valorConfirmPassword = confirmPassword.value;

    // Valida que el correo tenga un formato correcto
    const regexCorreo = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!regexCorreo.test(valorCorreo)) {
        mensajeRegistro.textContent = "Por favor, ingresa un formato de correo electrónico válido.";
        mensajeRegistro.classList.remove("hidden");

        btnRegister.disabled = false;
        btnRegister.textContent = "CREAR CUENTA";
        btnRegister.classList.remove("opacity-50", "cursor-not-allowed");
        return;
    }

    // Valida que se ingresen mínimo 8 caracteres en la contraseña
    if (valorPassword.length < 8) {
        mensajeRegistro.textContent = "La contraseña debe tener al menos 8 caracteres.";
        mensajeRegistro.classList.remove("hidden");

        btnRegister.disabled = false;
        btnRegister.textContent = "CREAR CUENTA";
        btnRegister.classList.remove("opacity-50", "cursor-not-allowed");
        return;
    }

    // Verifica que ambas contraseñas coincidan
    if (valorPassword !== valorConfirmPassword) {
        mensajeRegistro.textContent = "Las contraseñas no coinciden.";
        mensajeRegistro.classList.remove("hidden");

        btnRegister.disabled = false;
        btnRegister.textContent = "CREAR CUENTA";
        btnRegister.classList.remove("opacity-50", "cursor-not-allowed");
        return;
    }

    const tipoUsoInput = document.querySelector('input[name="uso"]:checked');
    const tipoUso = tipoUsoInput ? tipoUsoInput.value : "personal";

    try {
        // Pedimos que se envíe el código de verificación al correo
        const respCodigo = await fetch("/api/Auth/enviar-codigo", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                Correo: valorCorreo
            })
        });

        const textoCodigo = await respCodigo.text();
        const dataCodigo = textoCodigo ? JSON.parse(textoCodigo) : {};

        if (!respCodigo.ok || !dataCodigo.success) {
            throw new Error(dataCodigo.message || "No se pudo enviar el código de verificación.");
        }

        // Guarda temporalmente los datos del formulario hasta validar el código
        datosPendientesRegistro = {
            Nombre: nombre.value.trim(),
            Correo: valorCorreo,
            password: valorPassword,
            TipoUso: tipoUso
        };

        abrirModalCodigo(valorCorreo);
    } catch (error) {
        mensajeRegistro.textContent = error.message || "Ocurrió un error al enviar el código.";
        mensajeRegistro.classList.remove("hidden");
    } finally {
        btnRegister.disabled = false;
        btnRegister.textContent = "CREAR CUENTA";
        btnRegister.classList.remove("opacity-50", "cursor-not-allowed");
    }
});

// confirmar el código de 8 dígitos y crear la cuenta
btnConfirmarRegistro.addEventListener("click", async () => {
    const valorCodigo = codigoVerificacion.value.trim();

    if (!/^\d{8}$/.test(valorCodigo)) {
        mensajeModalCodigo.textContent = "Debes escribir exactamente 8 dígitos.";
        mensajeModalCodigo.classList.remove("hidden");
        actualizarEstadoBotonConfirmar();
        return;
    }

    if (!datosPendientesRegistro) {
        mensajeModalCodigo.textContent = "Vuelve a enviar el formulario para solicitar el código.";
        mensajeModalCodigo.classList.remove("hidden");
        return;
    }

    mensajeModalCodigo.classList.add("hidden");
    btnConfirmarRegistro.disabled = true;
    btnConfirmarRegistro.textContent = "CREANDO CUENTA...";
    btnConfirmarRegistro.classList.add("opacity-50", "cursor-not-allowed");

    try {
        // Envia los datos junto con el código ingresado para completar el registro
        const respuesta = await fetch("/api/Auth/registrar", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                ...datosPendientesRegistro,
                CodigoVerificacion: valorCodigo
            })
        });

        const textoRespuesta = await respuesta.text();
        const resultado = textoRespuesta ? JSON.parse(textoRespuesta) : {};

        if (!respuesta.ok || !resultado.success) {
            throw new Error(resultado.message || "No fue posible registrar la cuenta.");
        }

        cerrarModalCodigo();
        mostrarAlertaYRedirigir(
            resultado.message || "¡Cuenta creada con éxito! Por favor inicia sesión.",
            "login.html",
            10000
        );
    } catch (error) {
        mensajeModalCodigo.textContent = error.message || "Ocurrió un error al validar el código.";
        mensajeModalCodigo.classList.remove("hidden");
        actualizarEstadoBotonConfirmar();
    } finally {
        btnConfirmarRegistro.textContent = "CONFIRMAR Y CREAR CUENTA";
        actualizarEstadoBotonConfirmar();
    }
});