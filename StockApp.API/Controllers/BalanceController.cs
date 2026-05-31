using Microsoft.AspNetCore.Mvc;
using StockApp.Core.Entities;
using Microsoft.EntityFrameworkCore;
using StockApp.Data;

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
        // Total de ventas del mes
        var ventas = await _context.Ventas
            .Include(v => v.Detalles)
            .Where(v => v.Fecha.Year == año && v.Fecha.Month == mes)
            .ToListAsync();

        var totalVentas = ventas.Sum(v => v.Total);
        var cantidadVentas = ventas.Count;

        // Desglose por método de pago
        var porMetodoPago = ventas
            .GroupBy(v => v.MetodoPago)
            .Select(g => new
            {
                Metodo = g.Key,
                Total = g.Sum(v => v.Total),
                Cantidad = g.Count()
            })
            .ToList();

        // Producto más vendido del mes
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

        // Ventas por día del mes
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
}