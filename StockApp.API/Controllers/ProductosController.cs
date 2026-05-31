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

    // POST api/productos/{id}/oferta
    // Activa una oferta para un producto con un precio especial
    [HttpPost("{id}/oferta")]
    public async Task<IActionResult> ActivarOferta(int id, [FromBody] OfertaRequest request)
    {
        var producto = await _context.Productos.FindAsync(id);
        if(producto == null)
        return NotFound();

        //guardamos el precio origignal y aplicamos el precio de oferta
        producto.PrecioVenta = request.PrecioOferta;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mensaje = $"Oferta activada para {producto.Nombre}. Nuevo precio: ${producto.PrecioVenta}",
            Producto = producto
        });

    }
    // POST api/productos/{id}/descuento
    // POST api/productos/{id}/descuento
    [HttpPost("{id}/descuento")]
    public async Task<IActionResult> AplicarDescuento(int id, [FromBody] decimal porcentaje)
    {
        var producto = await _context.Productos.FindAsync(id);
        if(producto == null)
        return NotFound();

        var descuento = producto.PrecioVenta * (porcentaje / 100);
        producto.PrecioVenta = Math.Round(producto.PrecioVenta - descuento, 2);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mensaje = $"Descuento de {porcentaje}% aplicado a {producto.Nombre}. Nuevo precio: ${producto.PrecioVenta}",
            Producto = producto
        });
    }

}

public class OfertaRequest
{
    public decimal PrecioOferta { get; set; }
}