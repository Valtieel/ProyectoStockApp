using System.Net.Http.Headers;
using System.Reflection.Metadata.Ecma335;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockApp.Core.Entities;
using StockApp.Data;

namespace StockApp.API.Controllers;

//ruta base: api/cajas

[ApiController]
[Route("api/[controller]")]
public class CajasController : ControllerBase
{
    private readonly AppDbContext _context;

    public CajasController(AppDbContext context)
    {
        _context = context;
    }

     // GET api/cajas/actual
    // Devuelve la caja que está abierta en este momento
    // Solo puede haber UNA caja abierta a la vez

    [HttpGet("actual")]
    public async Task<IActionResult> GetCajaActual()
    {
        var caja = await _context.Cajas
        .FirstOrDefaultAsync(c => c.Abierta);

        if(caja == null)
        return NotFound("No hay ninguna caja abierta en este momento.");

        return Ok(caja);
    }

    // GET api/cajas/actual/detalle
    // Devuelve el detalle completo de la caja abierta para el dashboard:
    // ventas del turno + movimientos manuales (combinados como "movimientos"),
    // desglose por método de pago, y ticket promedio.
    [HttpGet("actual/detalle")]
    public async Task<IActionResult> GetDetalleCajaActual()
    {
        var caja = await _context.Cajas
            .Include(c => c.Ventas)
                .ThenInclude(v => v.Detalles)
            .Include(c => c.MovimientosCaja)
            .FirstOrDefaultAsync(c => c.Abierta);

        if (caja == null)
            return NotFound("No hay ninguna caja abierta en este momento.");

        var ventas = caja.Ventas.OrderByDescending(v => v.Fecha).ToList();
        var movimientosManuales = caja.MovimientosCaja.OrderByDescending(m => m.Fecha).ToList();

        var totalVentas = ventas.Sum(v => v.Total);
        var cantidadVentas = ventas.Count;
        var ticketPromedio = cantidadVentas > 0 ? totalVentas / cantidadVentas : 0;

        // Total de ingresos/egresos manuales (para mostrar el efectivo disponible real)
        var totalIngresosManuales = movimientosManuales.Where(m => m.Tipo == "ingreso").Sum(m => m.Monto);
        var totalEgresosManuales = movimientosManuales.Where(m => m.Tipo == "egreso").Sum(m => m.Monto);

        // Desglose por método de pago (solo ventas, los movimientos manuales no tienen método)
        var porMetodoPago = ventas
            .GroupBy(v => v.MetodoPago)
            .Select(g => new
            {
                Metodo = g.Key,
                Total = g.Sum(v => v.Total),
                Cantidad = g.Count()
            })
            .OrderByDescending(g => g.Total)
            .ToList();

        // Combinamos ventas + movimientos manuales en una sola lista de "movimientos",
        // cada uno con su tipo, signo del monto y descripción, ordenados por fecha.
        var movimientosVentas = ventas.Select(v => new
        {
            v.Id,
            Tipo = "venta",
            Descripcion = $"Venta #{v.Id:0000} · {v.MetodoPago}",
            Monto = v.Total,
            EsIngreso = true,
            v.Fecha
        });

        var movimientosCajaDto = movimientosManuales.Select(m => new
        {
            m.Id,
            Tipo = m.Tipo,
            Descripcion = m.Descripcion,
            Monto = m.Monto,
            EsIngreso = m.Tipo == "ingreso",
            m.Fecha
        });

        var movimientos = movimientosVentas
            .Concat(movimientosCajaDto)
            .OrderByDescending(m => m.Fecha)
            .ToList();

        return Ok(new
        {
            caja.Id,
            caja.MontoInicial,
            caja.FechaApertura,
            TotalVentas = totalVentas,
            CantidadVentas = cantidadVentas,
            TicketPromedio = ticketPromedio,
            TotalIngresosManuales = totalIngresosManuales,
            TotalEgresosManuales = totalEgresosManuales,
            PorMetodoPago = porMetodoPago,
            Movimientos = movimientos
        });
    }

    // POST api/cajas/movimiento
    // Registra un movimiento manual de caja (ingreso o egreso) que no está
    // asociado a una venta de producto: retiro de efectivo, pago a proveedor, etc.
    [HttpPost("movimiento")]
    public async Task<IActionResult> RegistrarMovimiento([FromBody] MovimientoCajaRequest request)
    {
        var caja = await _context.Cajas.FirstOrDefaultAsync(c => c.Abierta);
        if (caja == null)
            return BadRequest("No hay una caja abierta. Abrí la caja antes de registrar movimientos.");

        if (request.Tipo != "ingreso" && request.Tipo != "egreso")
            return BadRequest("El tipo de movimiento debe ser 'ingreso' o 'egreso'.");

        if (request.Monto <= 0)
            return BadRequest("El monto debe ser mayor a 0.");

        var movimiento = new MovimientoCaja
        {
            CajaId = caja.Id,
            Tipo = request.Tipo,
            Monto = request.Monto,
            Descripcion = request.Descripcion,
            Fecha = DateTime.Now
        };

        _context.MovimientosCaja.Add(movimiento);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mensaje = $"{(request.Tipo == "ingreso" ? "Ingreso" : "Egreso")} de ${request.Monto} registrado correctamente.",
            Movimiento = movimiento
        });
    }

       // GET api/cajas
    // Devuelve el historial de todas las cajas

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var cajas = await _context.Cajas
        .OrderByDescending(c => c.FechaApertura) //la mas reciente primero
        .ToListAsync();

        return Ok(cajas);
    }

     // POST api/cajas/abrir
    // Abre una nueva caja con el monto inicial del día

    [HttpPost("abrir")]
    public async Task<IActionResult> AbrirCaja([FromBody] decimal montoInicial)
    {
        //verificamos que no haya otra caja abierta
        var cajaAbierta = await _context.Cajas.AnyAsync(c => c.Abierta);
        if(cajaAbierta)
        return BadRequest("Ya hay una caja abierta. Cerrala antes de abrir una nuevas");

        var caja = new Caja
        {
            MontoInicial = montoInicial,
            FechaApertura = DateTime.Now,
            Abierta = true
        };

        _context.Cajas.Add(caja);
        await _context.SaveChangesAsync();
        return Ok(caja);
    }

    // POST api/cajas/cerrar
    // Cierra la caja actual con el monto final contado

    [HttpPost("cerrar")]
    public async Task<IActionResult> CerrarCaja([FromBody] decimal montoFinal)
    {
        //buscamos la caja abierta

        var caja = await _context.Cajas
        .Include(c => c.Ventas) // incluimos las ventas para calcular el total
        .Include(c => c.MovimientosCaja)
        .FirstOrDefaultAsync(c => c.Abierta);

        if(caja == null)
        return NotFound("No hay nignuna caja abierta.");

        //calculamos el total vendido del dia
        var totalVentas = caja.Ventas.Sum(v => v.Total);

        // sumamos los movimientos manuales (ingresos suman, egresos restan)
        var totalIngresosManuales = caja.MovimientosCaja.Where(m => m.Tipo == "ingreso").Sum(m => m.Monto);
        var totalEgresosManuales = caja.MovimientosCaja.Where(m => m.Tipo == "egreso").Sum(m => m.Monto);

        var montoEsperado = caja.MontoInicial + totalVentas + totalIngresosManuales - totalEgresosManuales;

        //cerramos la caja

        caja.MontoFinal = montoFinal;
        caja.FechaCierre = DateTime.Now;
        caja.Abierta = false;

        await _context.SaveChangesAsync();

        //devolvemos el resumen del cierre

        return Ok(new
        {
            caja.Id,
            caja.MontoInicial,
            TotalVentas = totalVentas,
            TotalIngresosManuales = totalIngresosManuales,
            TotalEgresosManuales = totalEgresosManuales,
            MontoEsperado = montoEsperado,
            MontoReal = montoFinal,
            Diferencia = montoFinal - montoEsperado,
            caja.FechaApertura,
            caja.FechaCierre
        });

    }

}

// clase auxiliar para recibir datos del movimiento manual de caja
public class MovimientoCajaRequest
{
    public string Tipo { get; set; } = string.Empty; // "ingreso" o "egreso"
    public decimal Monto { get; set; }
    public string Descripcion { get; set; } = string.Empty;
}