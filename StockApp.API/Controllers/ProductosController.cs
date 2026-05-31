using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockApp.Core.Entities;
using StockApp.Data;

namespace StockApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductosController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductosController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var productos = await _context.Productos
        .Where(p => p.Activo)
        .ToListAsync();
        return Ok(productos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var producto = await _context.Productos.FindAsync(id);
        if(producto == null) return NotFound();
        return Ok(producto);
    }
    
    [HttpPost]
    public async Task<IActionResult> Create(Producto producto)
    {
        _context.Productos.Add(producto);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new {id = producto.Id}, producto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Producto producto)
    { 
        if (id != producto.Id) return BadRequest();
        _context.Entry(producto).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var producto = await _context.Productos.FindAsync(id);
        if(producto == null) return NotFound();
        producto.Activo = false;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // POST api/productos/actualizar-precios
    // Actualiza el precio de todos los productos por un porcentaje
    // Ejemplo: 10 = sube 10%, -5 = baja 5%

    [HttpPost("actualizar-precios")]
    public async Task<IActionResult> ActualizarPrecios([FromBody] decimal porcentaje)
    {
        var productos = await _context.Productos
        .Where(p => p.Activo)
        .ToArrayAsync();

        foreach(var producto in productos)
        {
            //calculamos el nuevo precio con el porcentaje
            var aumento = producto.PrecioVenta * (porcentaje/100);
            producto.PrecioVenta = Math.Round(producto.PrecioVenta + aumento, 2);
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = $"Se actualizaron {productos.Count()} productos con un {porcentaje}%",
            ProductosActualizados = productos.Count()
        });
    }




}