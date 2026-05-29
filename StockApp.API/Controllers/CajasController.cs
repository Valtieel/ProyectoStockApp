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
        .FirstOrDefaultAsync(c => c.Abierta);

        if(caja == null)
        return NotFound("No hay nignuna caja abierta.");

        //calculamos el total vendido del dia
        var totalVentas = caja.Ventas.Sum(v => v.Total);

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
            MontoEsperado = caja.MontoInicial + totalVentas,
            MontoReal = montoFinal,
            Diferencia = montoFinal - (caja.MontoInicial + totalVentas),
            caja.FechaApertura,
            caja.FechaCierre
        });

    }

}