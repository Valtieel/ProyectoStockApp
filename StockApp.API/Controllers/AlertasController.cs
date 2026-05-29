using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockApp.Data;

namespace StockApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlertasController : ControllerBase
{
    private readonly AppDbContext _context;

    public AlertasController(AppDbContext context)
    {
        _context = context;
    }

    // GET api/alertas/stock-bajo
    // Devuelve productos cuyo stock actual es menor o igual al stock mínimo
    [HttpGet("stock-bajo")]
    public async Task<IActionResult> GetStockBajo()
    {
        var productos = await _context.Productos
            .Where(p => p.Activo && p.StockActual <= p.StockMinimo)
            .OrderBy(p => p.StockActual)
            .ToListAsync();

        return Ok(new
        {
            Cantidad = productos.Count,
            Mensaje = productos.Count == 0 
                ? "No hay productos con stock bajo." 
                : $"Hay {productos.Count} producto(s) con stock bajo.",
            Productos = productos
        });
    }

    // GET api/alertas/por-vencer
    // Devuelve productos que vencen en los próximos 30 días
    [HttpGet("por-vencer")]
    public async Task<IActionResult> GetPorVencer([FromQuery] int dias = 30)
    {
        var fechaLimite = DateTime.Today.AddDays(dias);

        var productos = await _context.Productos
            .Where(p => p.Activo 
                && p.FechaVencimiento.HasValue 
                && p.FechaVencimiento.Value <= fechaLimite
                && p.FechaVencimiento.Value >= DateTime.Today)
            .OrderBy(p => p.FechaVencimiento)
            .ToListAsync();

        return Ok(new
        {
            Cantidad = productos.Count,
            Mensaje = productos.Count == 0
                ? $"No hay productos por vencer en los próximos {dias} días."
                : $"Hay {productos.Count} producto(s) por vencer en los próximos {dias} días.",
            Productos = productos
        });
    }

    // GET api/alertas/vencidos
    // Devuelve productos que ya vencieron
    [HttpGet("vencidos")]
    public async Task<IActionResult> GetVencidos()
    {
        var productos = await _context.Productos
            .Where(p => p.Activo 
                && p.FechaVencimiento.HasValue 
                && p.FechaVencimiento.Value < DateTime.Today)
            .OrderBy(p => p.FechaVencimiento)
            .ToListAsync();

        return Ok(new
        {
            Cantidad = productos.Count,
            Mensaje = productos.Count == 0
                ? "No hay productos vencidos."
                : $"¡Atención! Hay {productos.Count} producto(s) vencido(s).",
            Productos = productos
        });
    }

    // GET api/alertas/resumen
    // Devuelve un resumen de todas las alertas juntas
    [HttpGet("resumen")]
    public async Task<IActionResult> GetResumen()
    {
        var hoy = DateTime.Today;
        var fechaLimite = hoy.AddDays(30);

        var stockBajo = await _context.Productos
            .CountAsync(p => p.Activo && p.StockActual <= p.StockMinimo);

        var porVencer = await _context.Productos
            .CountAsync(p => p.Activo 
                && p.FechaVencimiento.HasValue 
                && p.FechaVencimiento.Value <= fechaLimite
                && p.FechaVencimiento.Value >= hoy);

        var vencidos = await _context.Productos
            .CountAsync(p => p.Activo 
                && p.FechaVencimiento.HasValue 
                && p.FechaVencimiento.Value < hoy);

        return Ok(new
        {
            StockBajo = stockBajo,
            PorVencer = porVencer,
            Vencidos = vencidos,
            HayAlertas = stockBajo > 0 || porVencer > 0 || vencidos > 0
        });
    }
}