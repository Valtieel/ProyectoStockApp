using Microsoft.AspNetCore.Mvc;
using StockApp.Core.Entities;
using Microsoft.EntityFrameworkCore;
using StockApp.Data;
using System.Reflection.Metadata;

namespace StockApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BalanceController : ControllerBase
{
    private readonly AppDbContext _context;

    public BalanceController(AppDbContext context)
    {
        _context = context;
    }

    // GET api/balance?año=2026&mes=5
    // Devuelve el balance completo de un mes
    [HttpGet]
    public async Task<IActionResult> GetBalance([FromQuery] int año, [FromQuery] int mes)
    {
        var ventas = await _context.Ventas
            .Include(v => v.Detalles)
            .Where(v => v.Fecha.Year == año && v.Fecha.Month == mes)
            .ToListAsync();

        var totalVentas = ventas.Sum(v => v.Total);
        var cantidadVentas = ventas.Count;

        var porMetodoPago = ventas
            .GroupBy(v => v.MetodoPago)
            .Select(g => new
            {
                Metodo = g.Key,
                Total = g.Sum(v => v.Total),
                Cantidad = g.Count()
            })
            .ToList();

        var productoMasVendido = await _context.DetalleVentas
            .Include(d => d.Producto)
            .Where(d => d.Venta.Fecha.Year == año && d.Venta.Fecha.Month == mes)
            .GroupBy(d => d.Producto.Nombre)
            .Select(g => new
            {
                Producto = g.Key,
                TotalVendido = g.Sum(d => d.Cantidad)
            })
            .OrderByDescending(g => g.TotalVendido)
            .FirstOrDefaultAsync();

        var ventasPorDia = ventas
            .GroupBy(v => v.Fecha.Day)
            .Select(g => new
            {
                Dia = g.Key,
                Total = g.Sum(v => v.Total),
                Cantidad = g.Count()
            })
            .OrderBy(g => g.Dia)
            .ToList();

        return Ok(new
        {
            Año = año,
            Mes = mes,
            TotalVentas = totalVentas,
            CantidadVentas = cantidadVentas,
            PorMetodoPago = porMetodoPago,
            ProductoMasVendido = productoMasVendido,
            VentasPorDia = ventasPorDia
        });
    }

    // GET api/balance/hoy
    // Devuelve el resumen del día para el dashboard
    [HttpGet("hoy")]
    public async Task<IActionResult> GetResumenHoy()
    {
        var hoy = DateTime.Now;

        // Ventas de hoy
        var ventas = await _context.Ventas
            .Where(v => v.Fecha.Year == hoy.Year && v.Fecha.Month == hoy.Month && v.Fecha.Day == hoy.Day)
            .ToListAsync();

        var totalHoy = ventas.Sum(v => v.Total);
        var cantidadHoy = ventas.Count;

        // Caja abierta
        var cajaAbierta = await _context.Cajas
            .AnyAsync(c => c.Abierta);

        // Alertas
        var stockBajo = await _context.Productos
            .CountAsync(p => p.Activo && p.StockActual <= p.StockMinimo);

        var vencidos = await _context.Productos
            .CountAsync(p => p.Activo
                && p.FechaVencimiento.HasValue
                && p.FechaVencimiento.Value < hoy);

        var porVencer = await _context.Productos
            .CountAsync(p => p.Activo
                && p.FechaVencimiento.HasValue
                && p.FechaVencimiento.Value >= hoy
                && p.FechaVencimiento.Value <= hoy.AddDays(30));

        // Total productos activos
        var totalProductos = await _context.Productos
            .CountAsync(p => p.Activo);

        // ── NUEVO: Productos más vendidos hoy (top 4) ──────────────────────────
        // Busca en los detalles de las ventas de hoy, agrupa por producto
        // y devuelve los 4 con mayor cantidad vendida.
        var masVendidosHoy = await _context.DetalleVentas
            .Include(d => d.Producto)
            .Where(d => d.Venta.Fecha.Year == hoy.Year
                     && d.Venta.Fecha.Month == hoy.Month
                     && d.Venta.Fecha.Day == hoy.Day)
            .GroupBy(d => new { d.ProductoId, d.Producto.Nombre })
            .Select(g => new
            {
                Nombre = g.Key.Nombre,
                Cantidad = g.Sum(d => d.Cantidad)
            })
            .OrderByDescending(x => x.Cantidad)
            .Take(4)
            .ToListAsync();
        // ──────────────────────────────────────────────────────────────────────

        return Ok(new
        {
            Fecha = hoy,
            TotalVentasHoy = totalHoy,
            CantidadVentasHoy = cantidadHoy,
            CajaAbierta = cajaAbierta,
            StockBajo = stockBajo,
            Vencidos = vencidos,
            PorVencer = porVencer,
            TotalProductos = totalProductos,
            MasVendidosHoy = masVendidosHoy   // ← único campo nuevo
        });
    }
}