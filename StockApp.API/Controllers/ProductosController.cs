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
            .Include(p => p.Categoria)
            .Where(p => p.Activo)
            .ToListAsync();

        var resultado = productos.Select(p => new
        {
            p.Id,
            p.Nombre,
            p.Descripcion,
            p.CategoriaId,
            CategoriaNombre = p.Categoria != null ? p.Categoria.Nombre : null,
            p.PrecioVenta,
            p.Costo,
            p.StockActual,
            p.StockMinimo,
            p.FechaVencimiento,
            p.Activo,
            p.FechaCreacion
        });

        return Ok(resultado);
    }

    // GET api/productos/eliminados
    // Tiene que estar ANTES de GetById para que el router no lo confunda
    [HttpGet("eliminados")]
    public async Task<IActionResult> GetEliminados()
    {
        var productos = await _context.Productos
            .Include(p => p.Categoria)
            .Where(p => !p.Activo)
            .ToListAsync();

        var resultado = productos.Select(p => new
        {
            p.Id,
            p.Nombre,
            p.Descripcion,
            p.CategoriaId,
            CategoriaNombre = p.Categoria != null ? p.Categoria.Nombre : null,
            p.PrecioVenta,
            p.Costo,
            p.StockActual,
            p.StockMinimo,
            p.FechaVencimiento,
            p.Activo
        });

        return Ok(resultado);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var producto = await _context.Productos.FindAsync(id);
        if (producto == null) return NotFound();
        return Ok(producto);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Producto producto)
    {
        _context.Productos.Add(producto);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = producto.Id }, producto);
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
        if (producto == null) return NotFound();
        producto.Activo = false;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("actualizar-precios")]
    public async Task<IActionResult> ActualizarPrecios([FromBody] decimal porcentaje)
    {
        var productos = await _context.Productos
            .Where(p => p.Activo)
            .ToArrayAsync();

        foreach (var producto in productos)
        {
            var aumento = producto.PrecioVenta * (porcentaje / 100);
            producto.PrecioVenta = Math.Round(producto.PrecioVenta + aumento, 2);
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mensaje = $"Se actualizaron {productos.Count()} productos con un {porcentaje}%",
            ProductosActualizados = productos.Count()
        });
    }

    [HttpPost("{id}/oferta")]
    public async Task<IActionResult> ActivarOferta(int id, [FromBody] OfertaRequest request)
    {
        var producto = await _context.Productos.FindAsync(id);
        if (producto == null) return NotFound();

        producto.PrecioVenta = request.PrecioOferta;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mensaje = $"Oferta activada para {producto.Nombre}. Nuevo precio: ${producto.PrecioVenta}",
            Producto = producto
        });
    }

    [HttpPost("{id}/descuento")]
    public async Task<IActionResult> AplicarDescuento(int id, [FromBody] decimal porcentaje)
    {
        var producto = await _context.Productos.FindAsync(id);
        if (producto == null) return NotFound();

        var descuento = producto.PrecioVenta * (porcentaje / 100);
        producto.PrecioVenta = Math.Round(producto.PrecioVenta - descuento, 2);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mensaje = $"Descuento de {porcentaje}% aplicado a {producto.Nombre}. Nuevo precio: ${producto.PrecioVenta}",
            Producto = producto
        });
    }

    [HttpPost("{id}/restaurar")]
    public async Task<IActionResult> Restaurar(int id)
    {
        var producto = await _context.Productos.FindAsync(id);
        if (producto == null) return NotFound();

        producto.Activo = true;
        await _context.SaveChangesAsync();

        return Ok(new { Mensaje = $"{producto.Nombre} restaurado correctamente." });
    }
}

public class OfertaRequest
{
    public decimal PrecioOferta { get; set; }
}