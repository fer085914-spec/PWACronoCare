using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;

    public AuthController(IConfiguration configuration, IMemoryCache cache)
    {
        _configuration = configuration;
        _cache = cache;
    }

    private string CadenaSql =>
        _configuration.GetConnectionString("CadenaSQL")
        ?? throw new InvalidOperationException("Falta la cadena de conexión 'CadenaSQL' en appsettings.json.");

    private IActionResult RespuestaError(SqlException ex)
    {
        if (ex.Number >= 50001 && ex.Number <= 50009)
        {
            return BadRequest(new { success = false, message = ex.Message, errorNumber = ex.Number });
        }

        return StatusCode(500, new { success = false, message = "Ocurrió un error en el servidor. Inténtalo de nuevo." });
    }

    // ---------------------------------------------------------------
    // CUENTA
    // ---------------------------------------------------------------

    //[HttpPost("enviar-codigo")]
    //public IActionResult EnviarCodigo([FromBody] EnviarCodigoDto modelo)
    //{
    //    if (string.IsNullOrWhiteSpace(modelo?.Correo))
    //    {
    //        return BadRequest(new { success = false, message = "Ingresa un correo electrónico." });
    //    }

    //    string correo = modelo.Correo.Trim().ToLower();

    //    try
    //    {
    //        using (SqlConnection conn = new SqlConnection(CadenaSql))
    //        {
    //            conn.Open();

    //            using (SqlCommand cmdCorreo = new SqlCommand("SELECT 1 FROM dbo.USUARIO WHERE CORREO = @Correo", conn))
    //            {
    //                cmdCorreo.Parameters.AddWithValue("@Correo", correo);
    //                if (cmdCorreo.ExecuteScalar() != null)
    //                {
    //                    return BadRequest(new { success = false, message = "El correo ya está registrado." });
    //                }
    //            }

    //            string codigo8 = string.Empty;
    //            bool existeEnBd = true;
    //            int intentos = 0;

    //            while (existeEnBd && intentos < 20)
    //            {
    //                codigo8 = RandomNumberGenerator.GetInt32(10000000, 100000000).ToString("D8");

    //                using (SqlCommand cmdCodigo = new SqlCommand("SELECT 1 FROM dbo.USUARIO WHERE CODIGO_USUARIO = @Codigo", conn))
    //                {
    //                    cmdCodigo.Parameters.AddWithValue("@Codigo", codigo8);
    //                    existeEnBd = cmdCodigo.ExecuteScalar() != null;
    //                }

    //                intentos++;
    //            }

    //            if (existeEnBd)
    //            {
    //                return StatusCode(500, new { success = false, message = "No se pudo generar un código único. Intenta de nuevo." });
    //            }

    //            _cache.Set($"codigo_reg_{correo}", codigo8, TimeSpan.FromMinutes(10));

    //            var smtpSection = _configuration.GetSection("Smtp");
    //            using (var client = new SmtpClient(smtpSection["Host"], int.Parse(smtpSection["Port"] ?? "587")))
    //            {
    //                client.EnableSsl = bool.Parse(smtpSection["EnableSsl"] ?? "true");
    //                client.Credentials = new NetworkCredential(smtpSection["Usuario"], smtpSection["Password"]);

    //                var mail = new MailMessage
    //                {
    //                    From = new MailAddress(smtpSection["Remitente"] ?? smtpSection["Usuario"]!, "CronoCare"),
    //                    Subject = "Tu código de verificación de 8 dígitos",
    //                    Body = $"Tu código para completar el registro es: {codigo8}\n\nEste código vence en 10 minutos.",
    //                    IsBodyHtml = false
    //                };
    //                mail.To.Add(correo);
    //                client.Send(mail);
    //            }

    //            return Ok(new { success = true, message = "Código de 8 dígitos enviado a tu correo." });
    //        }
    //    }
    //    catch (SqlException ex)
    //    {
    //        return RespuestaError(ex);
    //    }
    //    catch (Exception)
    //    {
    //        return StatusCode(500, new { success = false, message = "No se pudo enviar el correo. Revisa la configuración SMTP." });
    //    }
    //}
    [HttpPost("enviar-codigo")]
    public IActionResult EnviarCodigo([FromBody] EnviarCodigoDto modelo)
    {
        if (string.IsNullOrWhiteSpace(modelo?.Correo))
        {
            return BadRequest(new { success = false, message = "Ingresa un correo electrónico." });
        }

        string correo = modelo.Correo.Trim().ToLower();

        try
        {
            using (SqlConnection conn = new SqlConnection(CadenaSql))
            {
                conn.Open();

                using (SqlCommand cmdCorreo = new SqlCommand("SELECT 1 FROM dbo.USUARIO WHERE CORREO = @Correo", conn))
                {
                    cmdCorreo.Parameters.AddWithValue("@Correo", correo);
                    if (cmdCorreo.ExecuteScalar() != null)
                    {
                        return BadRequest(new { success = false, message = "El correo ya está registrado." });
                    }
                }

                string codigo8 = string.Empty;
                bool existeEnBd = true;
                int intentos = 0;

                while (existeEnBd && intentos < 20)
                {
                    codigo8 = RandomNumberGenerator.GetInt32(10000000, 100000000).ToString("D8");

                    using (SqlCommand cmdCodigo = new SqlCommand("SELECT 1 FROM dbo.USUARIO WHERE CODIGO_USUARIO = @Codigo", conn))
                    {
                        cmdCodigo.Parameters.AddWithValue("@Codigo", codigo8);
                        existeEnBd = cmdCodigo.ExecuteScalar() != null;
                    }

                    intentos++;
                }

                if (existeEnBd)
                {
                    return StatusCode(500, new { success = false, message = "No se pudo generar un código único. Intenta de nuevo." });
                }

                _cache.Set($"codigo_reg_{correo}", codigo8, TimeSpan.FromMinutes(10));

                string htmlCuerpo = @"
<!DOCTYPE html>
<html lang=""es"">
<head>
    <meta charset=""UTF-8"">
</head>
<body style=""margin: 0; padding: 28px 16px; background-color: #f4f7f8; font-family: Arial, Helvetica, sans-serif; text-align: center;"">
    <div style=""max-width: 460px; margin: 0 auto; background-color: #ffffff; border-radius: 14px; padding: 28px 24px; box-shadow: 0 6px 20px rgba(0,0,0,0.06);"">
        <p style=""margin: 0 0 18px 0; font-size: 16px; color: #222222;"">
            Tu código de verificación para completar el registro a cronocare es :
        </p>

        <div style=""margin: 12px 0 18px 0;"">
            <img src=""cid:codigoImagen"" alt=""Código de verificación"" style=""display: block; margin: 0 auto; max-width: 100%; height: auto; border-radius: 10px;"" />
        </div>

        <p style=""margin: 0; font-size: 14px; color: #444444;"">
            Este código vence en 10 minutos
        </p>
    </div>
</body>
</html>";

                var smtpSection = _configuration.GetSection("Smtp");
                string host = smtpSection["Host"] ?? throw new InvalidOperationException("Falta Smtp:Host.");
                int port = int.Parse(smtpSection["Port"] ?? "587");
                bool enableSsl = bool.Parse(smtpSection["EnableSsl"] ?? "true");
                string usuario = smtpSection["Usuario"] ?? throw new InvalidOperationException("Falta Smtp:Usuario.");
                string password = smtpSection["Password"] ?? throw new InvalidOperationException("Falta Smtp:Password.");
                string remitente = string.IsNullOrWhiteSpace(smtpSection["Remitente"]) ? usuario : smtpSection["Remitente"]!;

                using (var client = new SmtpClient(host, port))
                {
                    client.EnableSsl = enableSsl;
                    client.Credentials = new NetworkCredential(usuario, password);

                    using var mail = new MailMessage
                    {
                        From = new MailAddress(remitente, "CronoCare"),
                        Subject = "Codigo de verificacion CronoCare",
                        IsBodyHtml = true
                    };
                    mail.To.Add(correo);

                    using var vistaHtml = AlternateView.CreateAlternateViewFromString(htmlCuerpo, Encoding.UTF8, "text/html");

                    byte[] pngBytes = CrearPngCodigo8Digitos(codigo8);
                    using var msImagen = new MemoryStream(pngBytes);

                    var codigoRecurso = new LinkedResource(msImagen, "image/png")
                    {
                        ContentId = "codigoImagen",
                        TransferEncoding = TransferEncoding.Base64
                    };
                    vistaHtml.LinkedResources.Add(codigoRecurso);

                    mail.AlternateViews.Add(vistaHtml);
                    client.Send(mail);
                }

                return Ok(new { success = true, message = "Código de 8 dígitos enviado a tu correo." });
            }
        }
        catch (SqlException ex)
        {
            return RespuestaError(ex);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"No se pudo enviar el correo: {ex.Message}" });
        }
    }

    private static byte[] CrearPngCodigo8Digitos(string codigo8)
    {
        var glifos = new Dictionary<char, string[]>
        {
            ['0'] = ["01110", "10001", "10011", "10101", "11001", "10001", "01110"],
            ['1'] = ["00100", "01100", "00100", "00100", "00100", "00100", "01110"],
            ['2'] = ["01110", "10001", "00001", "00110", "01000", "10000", "11111"],
            ['3'] = ["11110", "00001", "00001", "01110", "00001", "00001", "11110"],
            ['4'] = ["00010", "00110", "01010", "10010", "11111", "00010", "00010"],
            ['5'] = ["11111", "10000", "11110", "00001", "00001", "10001", "01110"],
            ['6'] = ["01110", "10000", "11110", "10001", "10001", "10001", "01110"],
            ['7'] = ["11111", "00001", "00010", "00100", "01000", "01000", "01000"],
            ['8'] = ["01110", "10001", "10001", "01110", "10001", "10001", "01110"],
            ['9'] = ["01110", "10001", "10001", "01111", "00001", "00001", "01110"]
        };

        int ancho = 320;
        int alto = 76;
        int escala = 4;
        int anchoDigito = 5 * escala;
        int altoDigito = 7 * escala;
        int espacioEntre = 10;

        string digitos = (codigo8 ?? "").PadLeft(8, '0');
        int totalAnchoTexto = digitos.Length * anchoDigito + (digitos.Length - 1) * espacioEntre;
        int inicioX = Math.Max(0, (ancho - totalAnchoTexto) / 2);
        int inicioY = Math.Max(0, (alto - altoDigito) / 2);

        byte bgR = 40, bgG = 127, bgB = 137; // #287F89
        byte[,] r = new byte[alto, ancho];
        byte[,] g = new byte[alto, ancho];
        byte[,] b = new byte[alto, ancho];

        for (int y = 0; y < alto; y++)
        {
            for (int x = 0; x < ancho; x++)
            {
                r[y, x] = bgR;
                g[y, x] = bgG;
                b[y, x] = bgB;
            }
        }

        for (int i = 0; i < digitos.Length; i++)
        {
            char c = digitos[i];
            if (!glifos.TryGetValue(c, out var patron)) patron = glifos['0'];

            int xBase = inicioX + i * (anchoDigito + espacioEntre);
            for (int fy = 0; fy < 7; fy++)
            {
                for (int fx = 0; fx < 5; fx++)
                {
                    if (patron[fy][fx] == '1')
                    {
                        int pxStart = xBase + fx * escala;
                        int pyStart = inicioY + fy * escala;
                        for (int dy = 0; dy < escala; dy++)
                        {
                            for (int dx = 0; dx < escala; dx++)
                            {
                                int xx = pxStart + dx;
                                int yy = pyStart + dy;
                                if (xx >= 0 && xx < ancho && yy >= 0 && yy < alto)
                                {
                                    r[yy, xx] = 255;
                                    g[yy, xx] = 255;
                                    b[yy, xx] = 255;
                                }
                            }
                        }
                    }
                }
            }
        }

        byte[] rawScanlines = new byte[alto * (1 + ancho * 3)];
        int idx = 0;
        for (int y = 0; y < alto; y++)
        {
            rawScanlines[idx++] = 0;
            for (int x = 0; x < ancho; x++)
            {
                rawScanlines[idx++] = r[y, x];
                rawScanlines[idx++] = g[y, x];
                rawScanlines[idx++] = b[y, x];
            }
        }

        byte[] compressedData;
        using (var msComp = new MemoryStream())
        {
            using (var zStream = new ZLibStream(msComp, CompressionLevel.Optimal, leaveOpen: true))
            {
                zStream.Write(rawScanlines, 0, rawScanlines.Length);
            }
            compressedData = msComp.ToArray();
        }

        using var msPng = new MemoryStream();
        msPng.Write([137, 80, 78, 71, 13, 10, 26, 10], 0, 8);

        byte[] ihdrData = new byte[13];
        WriteBigEndianInt32(ihdrData, 0, ancho);
        WriteBigEndianInt32(ihdrData, 4, alto);
        ihdrData[8] = 8;
        ihdrData[9] = 2;
        WritePngChunk(msPng, "IHDR", ihdrData);
        WritePngChunk(msPng, "IDAT", compressedData);
        WritePngChunk(msPng, "IEND", []);

        return msPng.ToArray();
    }

    private static void WritePngChunk(Stream s, string type, byte[] data)
    {
        byte[] lenBytes = new byte[4];
        WriteBigEndianInt32(lenBytes, 0, data.Length);
        s.Write(lenBytes, 0, 4);

        byte[] typeBytes = Encoding.ASCII.GetBytes(type);
        s.Write(typeBytes, 0, 4);

        if (data.Length > 0)
        {
            s.Write(data, 0, data.Length);
        }

        uint crc = ComputeCrc32(typeBytes, data);
        byte[] crcBytes = new byte[4];
        WriteBigEndianInt32(crcBytes, 0, unchecked((int)crc));
        s.Write(crcBytes, 0, 4);
    }

    private static void WriteBigEndianInt32(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)((value >> 24) & 0xFF);
        buffer[offset + 1] = (byte)((value >> 16) & 0xFF);
        buffer[offset + 2] = (byte)((value >> 8) & 0xFF);
        buffer[offset + 3] = (byte)(value & 0xFF);
    }

    private static uint ComputeCrc32(byte[] typeBytes, byte[] data)
    {
        uint crc = 0xFFFFFFFFu;
        foreach (byte b in typeBytes)
        {
            crc ^= b;
            for (int i = 0; i < 8; i++)
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : (crc >> 1);
        }
        foreach (byte b in data)
        {
            crc ^= b;
            for (int i = 0; i < 8; i++)
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : (crc >> 1);
        }
        return crc ^ 0xFFFFFFFFu;
    }

    [HttpPost("registrar")]
    public IActionResult Registrar([FromBody] UsuarioRegistroDto modelo)
    {
        string correo = (modelo?.Correo ?? string.Empty).Trim().ToLower();
        string codigoIngresado = (modelo?.CodigoVerificacion ?? string.Empty).Trim();

        if (!_cache.TryGetValue($"codigo_reg_{correo}", out string? codigoGuardado) ||
            string.IsNullOrEmpty(codigoGuardado) ||
            codigoGuardado != codigoIngresado)
        {
            return BadRequest(new { success = false, message = "El código de 8 dígitos es inválido o ha expirado." });
        }

        try
        {
            using (SqlConnection conn = new SqlConnection(CadenaSql))
            {
                using (SqlCommand cmd = new SqlCommand("sp_Usuario_Registrar", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Nombre", modelo.Nombre);
                    cmd.Parameters.AddWithValue("@Correo", modelo.Correo);
                    cmd.Parameters.AddWithValue("@Contrasena", modelo.Contrasena);
                    cmd.Parameters.AddWithValue("@TipoUso", modelo.TipoUso);
                    cmd.Parameters.AddWithValue("@CodigoVerificacion", codigoIngresado);

                    conn.Open();
                    object resultado = cmd.ExecuteScalar();

                    _cache.Remove($"codigo_reg_{correo}");

                    return Ok(new { success = true, message = "Usuario registrado correctamente", idUsuario = resultado });
                }
            }
        }
        catch (SqlException ex)
        {
            return RespuestaError(ex);
        }
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] UsuarioLoginDto modelo)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(CadenaSql))
            {
                using (SqlCommand cmd = new SqlCommand("sp_Usuario_ObtenerPorCorreo", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Correo", modelo.Correo);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string contrasenaBD = reader["CONTRASENA"].ToString();

                            if (contrasenaBD == modelo.Contrasena)
                            {
                                return Ok(new
                                {
                                    success = true,
                                    message = "Inicio de sesión exitoso",
                                    usuario = new
                                    {
                                        id = reader["ID_USUARIO"],
                                        nombre = reader["NOMBRE"].ToString(),
                                        correo = reader["CORREO"].ToString(),
                                        tipoUso = reader["TIPO_USO"].ToString()
                                    }
                                });
                            }
                        }
                    }
                }
            }

            return BadRequest(new { success = false, message = "Correo o contraseña incorrectos." });
        }
        catch (SqlException ex)
        {
            return RespuestaError(ex);
        }
    }

    // ---------------------------------------------------------------
    // CATÁLOGOS
    // ---------------------------------------------------------------

    [HttpGet("catalogos/tipos-perfil")]
    public IActionResult ListarTiposPerfil()
    {
        var tipos = new List<object>();

        try
        {
            using (SqlConnection conn = new SqlConnection(CadenaSql))
            {
                using (SqlCommand cmd = new SqlCommand("sp_TipoPerfil_Listar", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tipos.Add(new
                            {
                                idTipoPerfil = reader["ID_TIPO_PERFIL"],
                                nombre = reader["NOMBRE"].ToString()
                            });
                        }
                    }
                }
            }

            return Ok(tipos);
        }
        catch (SqlException ex)
        {
            return RespuestaError(ex);
        }
    }

    // Catálogo de vías de administración desde la BD
    [HttpGet("catalogos/vias-administracion")]
    public IActionResult ListarViasAdministracion()
    {
        var vias = new List<object>();

        try
        {
            using (SqlConnection conn = new SqlConnection(CadenaSql))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT ID_VIA, NOMBRE FROM VIA_ADMINISTRACION", conn))
                {
                    cmd.CommandType = CommandType.Text;
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            vias.Add(new
                            {
                                idVia = reader["ID_VIA"],
                                nombre = reader["NOMBRE"].ToString()
                            });
                        }
                    }
                }
            }

            return Ok(vias);
        }
        catch (SqlException ex)
        {
            return RespuestaError(ex);
        }
    }

    // ---------------------------------------------------------------
    // PERFILES
    // ---------------------------------------------------------------

    [HttpGet("perfiles/{idUsuario}")]
    public IActionResult ListarPerfiles(long idUsuario)
    {
        var perfiles = new List<object>();

        try
        {
            using (SqlConnection conn = new SqlConnection(CadenaSql))
            {
                using (SqlCommand cmd = new SqlCommand("sp_Perfil_ListarPorUsuario", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            perfiles.Add(new
                            {
                                idPerfil = reader["ID_PERFIL"],
                                nombre = reader["NOMBRE"].ToString(),
                                fechaNacimiento = Convert.ToDateTime(reader["FECHA_NACIMIENTO"]).ToString("yyyy-MM-dd"),
                                edad = reader["EDAD"],
                                idTipoPerfil = reader["ID_TIPO_PERFIL"],
                                tipoPerfil = reader["TIPO_PERFIL"].ToString()
                            });
                        }
                    }
                }
            }

            return Ok(perfiles);
        }
        catch (SqlException ex)
        {
            return RespuestaError(ex);
        }
    }

    [HttpGet("perfiles/selector/{idUsuario}")]
    public IActionResult ListarPerfilesParaSelector(long idUsuario)
    {
        var perfiles = new List<object>();

        try
        {
            using (SqlConnection conn = new SqlConnection(CadenaSql))
            {
                using (SqlCommand cmd = new SqlCommand("sp_Perfil_ListarParaSelector", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            perfiles.Add(new
                            {
                                idPerfil = reader["ID_PERFIL"],
                                nombre = reader["NOMBRE"].ToString(),
                                fechaNacimiento = Convert.ToDateTime(reader["FECHA_NACIMIENTO"]).ToString("yyyy-MM-dd"),
                                edad = reader["EDAD"],
                                idTipoPerfil = reader["ID_TIPO_PERFIL"],
                                tipoPerfil = reader["TIPO_PERFIL"].ToString()
                            });
                        }
                    }
                }
            }

            return Ok(perfiles);
        }
        catch (SqlException ex)
        {
            return RespuestaError(ex);
        }
    }

    [HttpPost("perfil/crear")]
    public IActionResult CrearPerfil([FromBody] PerfilCreacionDto modelo)
    {
        if (modelo == null || modelo.FechaNacimiento == default(DateTime))
        {
            return BadRequest(new { success = false, message = "Completa el nombre, el tipo y la fecha de nacimiento." });
        }

        try
        {
            using (SqlConnection conn = new SqlConnection(CadenaSql))
            {
                using (SqlCommand cmd = new SqlCommand("sp_Perfil_Crear", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IdUsuario", modelo.IdUsuario);
                    cmd.Parameters.AddWithValue("@IdTipoPerfil", modelo.IdTipoPerfil);
                    cmd.Parameters.AddWithValue("@Nombre", modelo.Nombre ?? string.Empty);
                    cmd.Parameters.Add("@FechaNacimiento", SqlDbType.Date).Value = modelo.FechaNacimiento.Date;

                    conn.Open();
                    object idPerfil = cmd.ExecuteScalar();

                    return Ok(new { success = true, message = "Perfil creado correctamente", idPerfil = idPerfil });
                }
            }
        }
        catch (SqlException ex)
        {
            return RespuestaError(ex);
        }
    }

    [HttpDelete("perfil/ocultar/{idUsuario}/{idPerfil}")]
    public IActionResult OcultarPerfil(long idUsuario, long idPerfil)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(CadenaSql))
            {
                using (SqlCommand cmd = new SqlCommand("sp_Perfil_Ocultar", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                    cmd.Parameters.AddWithValue("@IdPerfil", idPerfil);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return Ok(new
                            {
                                success = true,
                                message = "Perfil eliminado correctamente.",
                                idPerfil = reader["ID_PERFIL"]
                            });
                        }
                    }
                }
            }

            return NotFound(new { success = false, message = "No se encontró el perfil." });
        }
        catch (SqlException ex)
        {
            return RespuestaError(ex);
        }
    }

    // ---------------------------------------------------------------
    // MEDICAMENTOS
    // ---------------------------------------------------------------

    [HttpGet("medicamentos/activos/{idUsuario}/{idPerfil}")]
    public IActionResult ListarMedicamentosActivos(long idUsuario, long idPerfil)
    {
        var medicamentos = new List<object>();

        try
        {
            using (SqlConnection conn = new SqlConnection(CadenaSql))
            {
                using (SqlCommand cmd = new SqlCommand("sp_Medicamento_ListarActivos", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                    cmd.Parameters.AddWithValue("@IdPerfil", idPerfil);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            medicamentos.Add(new
                            {
                                IdMedicamento = reader["ID_MEDICAMENTO"],
                                NombreMedicamento = reader["NOMBRE_MEDICAMENTO"],
                                Foto = reader["FOTO"] != DBNull.Value ? reader["FOTO"] : null,
                                ViaAdministracion = reader["VIA_ADMINISTRACION"],
                                ProximaToma = reader["PROXIMA_TOMA"] != DBNull.Value ? reader["PROXIMA_TOMA"] : null
                            });
                        }
                    }
                }
            }

            return Ok(medicamentos);
        }
        catch (SqlException ex)
        {
            return RespuestaError(ex);
        }
    }

    [HttpPost("medicamento/crear")]
    public IActionResult RegistrarMedicamento([FromBody] MedicamentoCreacionDto modelo)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(CadenaSql))
            {
                using (SqlCommand cmd = new SqlCommand("sp_Medicamento_Registrar", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IdUsuario", modelo.IdUsuario);
                    cmd.Parameters.AddWithValue("@IdPerfil", modelo.IdPerfil);
                    cmd.Parameters.AddWithValue("@IdVia", modelo.IdVia);
                    cmd.Parameters.AddWithValue("@NombreMedicamento", modelo.NombreMedicamento);
                    cmd.Parameters.AddWithValue("@Foto", string.IsNullOrEmpty(modelo.Foto) ? (object)DBNull.Value : modelo.Foto);
                    cmd.Parameters.AddWithValue("@IntervaloHoras", modelo.IntervaloHoras);
                    cmd.Parameters.AddWithValue("@DuracionDias", modelo.DuracionDias);
                    cmd.Parameters.AddWithValue("@HoraPrimeraToma", modelo.HoraInicio);

                    if (modelo.FechaInicio.HasValue)
                    {
                        cmd.Parameters.Add("@FechaInicio", SqlDbType.Date).Value = modelo.FechaInicio.Value.Date;
                    }
                    else
                    {
                        cmd.Parameters.Add("@FechaInicio", SqlDbType.Date).Value = DBNull.Value;
                    }

                    conn.Open();
                    object resultado = cmd.ExecuteScalar();

                    return Ok(new { success = true, message = "Medicamento registrado correctamente", idMedicamento = resultado });
                }
            }
        }
        catch (SqlException ex)
        {
            return RespuestaError(ex);
        }
    }

    [HttpGet("medicamentos/detalle/{idUsuario}/{idMedicamento}")]
    public IActionResult ObtenerDetalleMedicamento(long idUsuario, long idMedicamento)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(CadenaSql))
            {
                using (SqlCommand cmd = new SqlCommand("sp_Medicamento_ObtenerDetalle", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                    cmd.Parameters.AddWithValue("@IdMedicamento", idMedicamento);
                    cmd.Parameters.AddWithValue("@Fecha", DBNull.Value);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return NotFound(new { success = false, message = "Medicamento no encontrado." });
                        }

                        var medicamentoDetalle = new
                        {
                            idMedicamento = reader["ID_MEDICAMENTO"],
                            nombreMedicamento = reader["NOMBRE_MEDICAMENTO"].ToString(),
                            idVia = reader["ID_VIA"],
                            viaAdministracion = reader["VIA_ADMINISTRACION"].ToString(),
                            intervaloHoras = reader["INTERVALO_HORAS"],
                            duracionDias = reader["DURACION_DIAS"],
                            horaPrimeraToma = reader["HORA_PRIMERA_TOMA"].ToString(),
                            foto = reader["FOTO"] != DBNull.Value ? reader["FOTO"].ToString() : null,
                            tomas = new List<object>()
                        };

                        if (reader.NextResult())
                        {
                            while (reader.Read())
                            {
                                ((List<object>)medicamentoDetalle.tomas).Add(new
                                {
                                    fechaHoraProgramada = reader["FECHA_HORA_PROGRAMADA"],
                                    hora = Convert.ToDateTime(reader["FECHA_HORA_PROGRAMADA"]).ToString("HH:mm"),
                                    estado = reader["ESTADO"].ToString()
                                });
                            }
                        }

                        return Ok(medicamentoDetalle);
                    }
                }
            }
        }
        catch (SqlException ex)
        {
            return RespuestaError(ex);
        }
    }

    [HttpGet("agenda/hoy/{idUsuario}")]
    public IActionResult ObtenerAgendaHoy(long idUsuario)
    {
        var agenda = new List<object>();
        try
        {
            using (SqlConnection conn = new SqlConnection(CadenaSql))
            {
                using (SqlCommand cmd = new SqlCommand("sp_Toma_ListarHoy", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                    cmd.Parameters.AddWithValue("@Fecha", DBNull.Value);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            agenda.Add(new
                            {
                                idToma = reader["ID_TOMA"],
                                fechaHoraProgramada = reader["FECHA_HORA_PROGRAMADA"],
                                estadoToma = reader["ESTADO"].ToString(),
                                idMedicamento = reader["ID_MEDICAMENTO"],
                                nombreMedicamento = reader["NOMBRE_MEDICAMENTO"].ToString(),
                                perfil = new
                                {
                                    idPerfil = reader["ID_PERFIL"],
                                    nombre = reader["NOMBRE_PERFIL"].ToString(),
                                    relacion = reader["TIPO_PERFIL"].ToString(),
                                    fechaNacimiento = Convert.ToDateTime(reader["FECHA_NACIMIENTO"]).ToString("yyyy-MM-dd")
                                }
                            });
                        }
                    }
                }
            }
            return Ok(agenda);
        }
        catch (SqlException ex)
        {
            return RespuestaError(ex);
        }
    }

    [HttpPost("agenda/actualizar-toma")]
    public IActionResult ActualizarEstadoToma([FromBody] TomaActualizacionDto modelo)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(CadenaSql))
            {
                using (SqlCommand cmd = new SqlCommand("sp_Toma_Registrar", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IdUsuario", modelo.IdUsuario);
                    cmd.Parameters.AddWithValue("@IdToma", modelo.IdToma);
                    cmd.Parameters.AddWithValue("@Estado", modelo.NuevoEstado);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return Ok(new
                            {
                                success = true,
                                message = "Estado actualizado correctamente",
                                idToma = reader["ID_TOMA"],
                                estado = reader["ESTADO"].ToString(),
                                fechaHoraConfirmada = reader["FECHA_HORA_CONFIRMADA"]
                            });
                        }
                    }
                }
            }

            return BadRequest(new { success = false, message = "No se pudo actualizar la toma." });
        }
        catch (SqlException ex)
        {
            return RespuestaError(ex);
        }
    }

    [HttpPut("medicamento/editar")]
    public IActionResult EditarMedicamento([FromBody] MedicamentoEdicionDto modelo)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(CadenaSql))
            {
                using (SqlCommand cmd = new SqlCommand("sp_Medicamento_Editar", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IdUsuario", modelo.IdUsuario);
                    cmd.Parameters.AddWithValue("@IdMedicamento", modelo.IdMedicamento);
                    cmd.Parameters.AddWithValue("@IdVia", modelo.IdVia);
                    cmd.Parameters.AddWithValue("@NombreMedicamento", modelo.NombreMedicamento);
                    cmd.Parameters.AddWithValue("@Foto", string.IsNullOrEmpty(modelo.Foto) ? (object)DBNull.Value : modelo.Foto);
                    cmd.Parameters.AddWithValue("@IntervaloHoras", modelo.IntervaloHoras);
                    cmd.Parameters.AddWithValue("@DuracionDias", modelo.DuracionDias);
                    cmd.Parameters.AddWithValue("@HoraPrimeraToma", modelo.HoraPrimeraToma);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            long idMed = Convert.ToInt64(reader["ID_MEDICAMENTO"]);
                            int tomasRegeneradas = Convert.ToInt32(reader["TOMAS_REGENERADAS"]);

                            return Ok(new
                            {
                                success = true,
                                message = "Medicamento actualizado correctamente",
                                idMedicamento = idMed,
                                tomasRegeneradas = tomasRegeneradas
                            });
                        }
                    }
                }
            }
            return BadRequest(new { success = false, message = "No se pudo actualizar el medicamento." });
        }
        catch (SqlException ex)
        {
            return RespuestaError(ex);
        }
    }

    [HttpGet("medicamentos/concluidos/usuario/{idUsuario}")]
    public IActionResult ListarMedicamentosConcluidosPorUsuario(long idUsuario)
    {
        var concluidos = new List<object>();

        try
        {
            using (SqlConnection conn = new SqlConnection(CadenaSql))
            {
                var perfilesIds = new List<long>();
                using (SqlCommand cmdPerfil = new SqlCommand("SELECT ID_PERFIL FROM PERFIL_CUIDADO WHERE ID_USUARIO = @IdUsuario", conn))
                {
                    cmdPerfil.Parameters.AddWithValue("@IdUsuario", idUsuario);
                    conn.Open();
                    using (SqlDataReader rPerfil = cmdPerfil.ExecuteReader())
                    {
                        while (rPerfil.Read())
                        {
                            perfilesIds.Add(Convert.ToInt64(rPerfil["ID_PERFIL"]));
                        }
                    }
                }

                foreach (var idPerfil in perfilesIds)
                {
                    using (SqlCommand cmd = new SqlCommand("sp_Medicamento_ListarConcluidos", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                        cmd.Parameters.AddWithValue("@IdPerfil", idPerfil);

                        if (conn.State != ConnectionState.Open) conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                concluidos.Add(new
                                {
                                    idMedicamento = reader["ID_MEDICAMENTO"],
                                    nombreMedicamento = reader["NOMBRE_MEDICAMENTO"].ToString(),
                                    foto = reader["FOTO"] != DBNull.Value ? reader["FOTO"].ToString() : null,
                                    viaAdministracion = reader["VIA_ADMINISTRACION"].ToString(),
                                    fechaInicio = Convert.ToDateTime(reader["FECHA_INICIO"]).ToString("yyyy-MM-dd"),
                                    fechaFin = Convert.ToDateTime(reader["FECHA_FIN"]).ToString("yyyy-MM-dd"),
                                    diasTomados = reader["DIAS_TOMADOS"],
                                    tomasTomadas = reader["TOMAS_TOMADAS"],
                                    tomasTotales = reader["TOMAS_TOTALES"]
                                });
                            }
                        }
                    }
                }
            }

            return Ok(concluidos);
        }
        catch (SqlException ex)
        {
            return RespuestaError(ex);
        }
    }

    [HttpGet("medicamento/concluido-detalle/{idMedicamento}")]
    public IActionResult ObtenerDetalleConcluido(long idMedicamento)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(CadenaSql))
            {
                using (SqlCommand cmd = new SqlCommand("sp_Medicamento_DetalleConcluido", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IdMedicamento", idMedicamento);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var resultado = new
                            {
                                idMedicamento = reader["ID_MEDICAMENTO"],
                                nombreMedicamento = reader["NOMBRE_MEDICAMENTO"].ToString(),
                                foto = reader["FOTO"] != DBNull.Value ? reader["FOTO"].ToString() : null,
                                idPerfil = reader["ID_PERFIL"],
                                nombrePerfil = reader["NOMBRE_PERFIL"].ToString(),
                                fechaInicio = Convert.ToDateTime(reader["FECHA_INICIO"]).ToString("yyyy-MM-dd"),
                                fechaConclusion = Convert.ToDateTime(reader["FECHA_FIN"]).ToString("yyyy-MM-dd"),
                                diasTomados = reader["DIAS_TOMADOS"],
                                tomasTomadas = reader["TOMAS_TOMADAS"],
                                tomasNoCompletadas = reader["TOMAS_NO_COMPLETADAS"]
                            };

                            return Ok(resultado);
                        }
                    }
                }
            }

            return NotFound(new { mensaje = "Medicamento concluido no encontrado." });
        }
        catch (SqlException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("medicamento/ocultar/{idMedicamento}")]
    public IActionResult OcultarMedicamento(long idMedicamento, [FromQuery] long? idUsuario = null)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(CadenaSql))
            {
                using (SqlCommand cmd = new SqlCommand("sp_Medicamento_Ocultar", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IdMedicamento", idMedicamento);
                    cmd.Parameters.AddWithValue("@IdUsuario", (object?)idUsuario ?? DBNull.Value);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    return Ok(new { success = true, mensaje = "Medicamento ocultado correctamente." });
                }
            }
        }
        catch (SqlException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public class EnviarCodigoDto
{
    public string Correo { get; set; }
}

public class UsuarioRegistroDto
{
    public string Nombre { get; set; }
    public string Correo { get; set; }
    [JsonPropertyName("password")]
    public string Contrasena { get; set; }
    public string TipoUso { get; set; }
    public string CodigoVerificacion { get; set; }
}

public class PerfilCreacionDto
{
    public long IdUsuario { get; set; }
    public long IdTipoPerfil { get; set; }
    public string Nombre { get; set; }
    public DateTime FechaNacimiento { get; set; }
}

public class UsuarioLoginDto
{
    public string Correo { get; set; }
    public string Contrasena { get; set; }
}

public class MedicamentoCreacionDto
{
    public long IdUsuario { get; set; }
    public long IdPerfil { get; set; }
    public long IdVia { get; set; }
    public string NombreMedicamento { get; set; }
    public string? Foto { get; set; } = null;
    public int IntervaloHoras { get; set; }
    public int DuracionDias { get; set; }
    public string HoraInicio { get; set; }
    public DateTime? FechaInicio { get; set; } = null;
}

public class TomaActualizacionDto
{
    public long IdUsuario { get; set; }
    public long IdToma { get; set; }
    public string NuevoEstado { get; set; }
}

public class MedicamentoEdicionDto
{
    public long IdUsuario { get; set; }
    public long IdMedicamento { get; set; }
    public long IdVia { get; set; }
    public string NombreMedicamento { get; set; }
    public string? Foto { get; set; } = null;
    public int IntervaloHoras { get; set; }
    public int DuracionDias { get; set; }
    public string HoraPrimeraToma { get; set; }
}