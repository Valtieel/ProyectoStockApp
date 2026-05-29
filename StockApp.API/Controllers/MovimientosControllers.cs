using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockApp.Core.Entities;
using StockApp.Data;

namespace StockApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MovimientosController : ControllerBase
{
    private readonly AppDbContext _context;
    public MovimientosController(AppDbContext context)
    {
        _context = context;
    }

     // GET api/movimientos
    // Devuelve todos los movimientos ordenados por fecha

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var movimientos = await _context.Movimientos
        .Include(m => m.Producto)
        .OrderByDescending(m => m.Fecha)
        .ToListAsync();

        return Ok(movimientos);
    }

     // GET api/movimientos/producto/5
    // Devuelve todos los movimientos de un producto específico
    [HttpGet("producto/{productoId}")]
    public async Task<IActionResult> GetByProducto(int productoId)
    {
        var movimientos = await _context.Movimientos
        .Include(m => m.Producto)
        .Where(m => m.ProductoId == productoId)
        .OrderByDescending(m => m.Fecha)
        .ToListAsync();

        return Ok(movimientos);
    }

     // POST api/movimientos/entrada
    // Registra una entrada de mercadería y suma al stock

    [HttpPost("entrada")]
    public async Task<IActionResult> RegistrarEntrada([FromBody] MovimientoRequest request)
    {
        var producto = await _context.Productos.FindAsync(request.ProductoId);
        if(producto == null)
        return NotFound($"Producto con ID {request.ProductoId} no encontrado.");

        //sumamos el stock
        producto.StockActual += request.Cantidad;

        var movimiento = new Movimiento
        {
            ProductoId = request.ProductoId,
            Tipo = "Entrada",
            Cantidad = request.Cantidad,
            Motivo = request.Motivo,
            Fecha = DateTime.Now
        };

        _context.Movimientos.Add(movimiento);
        await _context.SaveChangesAsync();

        return Ok(new{

            Mensaje = $"Entrada registrada. Nuevo stock de {producto.Nombre}: {producto.StockActual}",
            Movimiento = movimiento
            
        });
    }

    // POST api/movimientos/salida
    // Registra una salida manual de mercadería (producto roto, vencido, etc)
    [HttpPost("salida")]
    public async Task<IActionResult> RegistrarSalida([FromBody] MovimientoRequest request)
    {
        var producto = await _context.Productos.FindAsync(request.ProductoId);
        if(producto == null)
        return NotFound($"Producto con ID {request.ProductoId} no encontrado.");

        //verificamos que haya suficiente stock
        if(producto.StockActual < request.Cantidad)
        return BadRequest($"Stock insuficientes. Stock disponible: {producto.StockActual}");

        //restamos el stock
        producto.StockActual -= request.Cantidad;

        var movimeinto = new Movimiento
        {
            ProductoId = request.ProductoId,
            Tipo = "Salida",
            Cantidad = request.Cantidad,
            Motivo = request.Motivo,
            Fecha = DateTime.Now
        };

        _context.Movimientos.Add(movimeinto);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mensaje = $"Salida registrada. Nuevo stock de {producto.Nombre}: {producto.StockActual}",
            Movimiento = movimeinto
        });
    }
}
//clase auxiliar para recibir los datos del movimiento
public class MovimientoRequest
{
    public int ProductoId {get; set;}
    public int Cantidad {get; set;}
    public string? Motivo {get; set;}
}