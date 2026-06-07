using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockApp.Core.Entities;
using StockApp.Data;

namespace StockApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfiguracionController : ControllerBase
{
    private readonly AppDbContext _context;
    
    public ConfiguracionController(AppDbContext context)
    {
        _context = context;
    }

    // GET api/configuracion/{clave}
    // Devuelve el valor de una configuración
    [HttpGet("{clave}")]
    public async Task<IActionResult> Get(string clave)
    {
        var config = await _context.Configuraciones
        .FirstOrDefaultAsync(c => c.Clave == clave);

        if(config == null)
        return Ok(new {Clave = clave, Valor = ""});

        return Ok(config);
    }
    // POST api/configuracion
    // Guarda o actualiza una configuración
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] Configuracion configuracion)
    {
        var existing = await _context.Configuraciones
        .FirstOrDefaultAsync(c=> c.Clave == configuracion.Clave);
        if(existing != null)
        {
            existing.Valor = configuracion.Valor;
        }
        else
        {
            _context.Configuraciones.Add(configuracion);
        }
        await _context.SaveChangesAsync();

        return Ok(new {Mensaje = "Configuración guardada correctamente."});
    }


}