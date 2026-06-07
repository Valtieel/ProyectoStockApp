using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StockApp.Core.Entities;
using StockApp.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace StockApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }
    //POST api/auth/login
    //Recibe mail y password, devuelve un token JWT
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var usuario = await _context.Usuarios
        .FirstOrDefaultAsync(u => u.Email == request.Email && u.Activo);

        if(usuario == null)
        return Unauthorized("Usuario no enncontrado");

        //verificamos la contraseña
        if(!VerificarPassword(request.Password, usuario.PasswordHash))
        return Unauthorized("Contraseña incorrecta.");

        //generamos el token JWT
        var token = GenerarToken(usuario);

        return Ok(new
        {
            Token = token,
            Nombre = usuario.Nombre,
            Email = usuario.Email,
            Rol = usuario.Rol
        });
    }
    
    //POST api/auth/ registro
    //crea un nuevo usuario (solo admin puede crear usuarios)

    [HttpPost("registro")]
    public async Task<IActionResult> Registro([FromBody] RegistroRequest request)
    {
        //verificamos que el email no exista
        var existe = await _context.Usuarios
        .AnyAsync(u => u.Email == request.Email);

        if(existe)
        return BadRequest("Ya existe un usuario con ese email.");

        var usuario = new Usuario
        {
            Nombre = request.Nombre,
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            Rol = request.Rol,
            Activo = true
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mensaje = $"Usuario {usuario.Nombre} creado correctamente."
        });
    }

    //genera un hash seguro de la contraseña 
    private string HashPassword(string Password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(Password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    //verifica que la contraseña ingresada coincida con el hash
    private bool VerificarPassword(string password, string hash)
    {
        return HashPassword(password) == hash;
    }

    //genera el token JWT con los datos del usuario
    private string GenerarToken(Usuario usuario)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

            var credenciales = new SigningCredentials(
                key, SecurityAlgorithms.HmacSha256);

                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                    new Claim(ClaimTypes.Name, usuario.Nombre),
                    new Claim(ClaimTypes.Email, usuario.Email),
                    new Claim(ClaimTypes.Role, usuario.Rol)
                };
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(8), //el token dura 8 horas
                signingCredentials: credenciales
            );

            return new JwtSecurityTokenHandler().WriteToken(token);    
    }

    // GET api/auth/usuarios
// Devuelve todos los usuarios
[HttpGet("usuarios")]
public async Task<IActionResult> GetUsuarios()
{
    var usuarios = await _context.Usuarios
        .Select(u => new
        {
            u.Id,
            u.Nombre,
            u.Email,
            u.Rol,
            u.Activo,
            u.FechaCreacion
        })
        .OrderBy(u => u.Nombre)
        .ToListAsync();

    return Ok(usuarios);
}

// PUT api/auth/usuarios/{id}/toggle
// Activa o desactiva un usuario
[HttpPut("usuarios/{id}/toggle")]
public async Task<IActionResult> ToggleUsuario(int id)
{
    var usuario = await _context.Usuarios.FindAsync(id);
    if (usuario == null) return NotFound();

    usuario.Activo = !usuario.Activo;
    await _context.SaveChangesAsync();

    return Ok(new
    {
        Mensaje = $"Usuario {usuario.Nombre} {(usuario.Activo ? "activado" : "desactivado")} correctamente.",
        Activo = usuario.Activo
    });
}
// PUT api/auth/cambiar-password
// Cambia la contraseña del usuario logueado
[HttpPut("cambiar-password")]
public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordRequest request)
{
    var usuario = await _context.Usuarios
        .FirstOrDefaultAsync(u => u.Email == request.Email && u.Activo);

    if (usuario == null)
        return NotFound("Usuario no encontrado.");

    // Verificamos la contraseña actual
    if (!VerificarPassword(request.PasswordActual, usuario.PasswordHash))
        return BadRequest("La contraseña actual es incorrecta.");

    // Actualizamos la contraseña
    usuario.PasswordHash = HashPassword(request.PasswordNueva);
    await _context.SaveChangesAsync();

    return Ok(new { Mensaje = "Contraseña actualizada correctamente." });
}

    public class LoginRequest
    {
        public string Email {get; set;} = string.Empty;
        public string Password {get; set;} = string.Empty;
    }

    public class RegistroRequest
    {
        public string Nombre {get; set;} = string.Empty;
        public string Email {get; set;} = string.Empty;
        public string Password {get; set;} = string.Empty;
        public string Rol {get; set;} = "empleado";
    }

    public class CambiarPasswordRequest
    {
        public string Email {get; set;} = string.Empty;
        public string PasswordActual {get; set;} = string.Empty;
        public string PasswordNueva {get; set;} = string.Empty;
    }
}