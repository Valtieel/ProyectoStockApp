using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using StockApp.Core.Entities;
using StockApp.Data;

namespace StockApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VentasController : ControllerBase
{
    private readonly AppDbContext _context;
    public VentasController(AppDbContext context)
    {
        _context = context;
    }

     // GET api/ventas
    // Devuelve todas las ventas con sus detalles
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var ventas = await _context.Ventas
        .Include(v => v.Detalles)
        .ThenInclude(d => d.Producto)
        .OrderByDescending(v => v.Fecha)
        .ToListAsync();

        return Ok(ventas);
    }

     // GET api/ventas/hoy
    // Devuelve todas las ventas del día de hoy

    [HttpGet("hoy")]
    public async Task<IActionResult> GetHoy()
    {
        var hoy = DateTime.Today;

        var ventas = await _context.Ventas
        .Include(v =>  v.Detalles)
        .ThenInclude(d => d.Producto)
        .Where(v => v.Fecha.Date == hoy)
        .OrderByDescending(v => v.Fecha)
        .ToListAsync();

        //calculamos el resumen del dia
        var resumen = new
        {
            Ventas = ventas,
            TotalVentas = ventas.Count,
            TotalRecaudado =ventas.Sum(v => v.Total)
        };

        return Ok(resumen);
     }

    // GET api/ventas/mes?año=2026&mes=5
    // Devuelve todas las ventas de un mes específico

    [HttpGet("mes")]
    public async Task<IActionResult> GetPorMeS([FromQuery] int año, [FromQuery] int mes)
    {
     var ventas = await _context.Ventas
     .Include(v => v.Detalles)
     .ThenInclude(d => d.Producto)
     .Where(v => v.Fecha.Year == año && v.Fecha.Month == mes)
     .OrderByDescending(v => v.Fecha)
     .ToListAsync();

     var resumen = new
     {
         Año = año,
         Mes = mes,
         Ventas = ventas,
         TotalVentas = ventas.Count,
         TotalRecaudado = ventas.Sum(v => v.Total)
     };

     return Ok(resumen);   
    }

    // POST api/ventas
    // Registra una nueva venta con todos sus productos
    // Automáticamente descuenta el stock de cada producto vendido

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CrearVentaRequest request)
    {
        //verificamos que haya una caja abierta
        var caja = await _context.Cajas.FirstOrDefaultAsync(c => c.Abierta);
        if(caja == null)
        return BadRequest("No hay una caja abierta. Abrí la caja antes de registrar ventas.");

        //creamos la venta

        var venta = new Venta
        {
            CajaId = caja.Id,
            MetodoPago = request.MetodoPago,
            Fecha = DateTime.Now,
            Detalles = new List<DetalleVenta>()
        };

        decimal total = 0;
        
        //procesamos cada producto de la venta

        foreach(var item in request.Items)
        {
            var producto = await _context.Productos.FindAsync(item.ProductoId);
            if(producto == null)
            return BadRequest($"El producto con ID {item.ProductoId} no existe. ");

            if(producto.StockActual < item.Cantidad)
            return BadRequest($"Stock insuficientas para {producto.Nombre}. Stock disponible: {producto.StockActual}");

            //descontamos el stock automaticamente 
            producto.StockActual -= item.Cantidad;

            var detalle = new DetalleVenta
            {
                ProductoId = item.ProductoId,
                Cantidad = item.Cantidad,
                PrecioUnitario = producto.PrecioVenta // guardamos el precio anual
            };

            total += detalle.Cantidad * detalle.PrecioUnitario;
            venta.Detalles.Add(detalle);
        }

        venta.Total = total;
        _context.Ventas.Add(venta);
        await _context.SaveChangesAsync();

        return Ok(venta);
        
    }

}

//clase auxiliar para recibir datos de la venta
public class CrearVentaRequest
{
    public string MetodoPago{get; set;} = string.Empty;
    public List<VentaItem> Items{get; set;} = new();
}

public class VentaItem
{
    public int ProductoId {get; set;}
    public int Cantidad {get; set;}
}