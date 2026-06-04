using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockApp.Core.Entities;
using StockApp.Data;

namespace StockApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly AppDbContext _context;

    public ClientesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var clientes = await _context.Clientes
            .Where(c => c.Activo)
            .OrderBy(c => c.Nombre)
            .ToListAsync();

        return Ok(clientes);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.Id == id && c.Activo);

        if (cliente == null) return NotFound();

        var movimientos = await _context.CuentaCorrientes
            .Where(c => c.ClienteId == id)
            .ToListAsync();

        var deuda = movimientos.Where(m => m.Tipo == "deuda").Sum(m => m.Monto);
        var pagado = movimientos.Where(m => m.Tipo == "pago").Sum(m => m.Monto);

        return Ok(new
        {
            cliente.Id,
            cliente.Nombre,
            cliente.Telefono,
            cliente.Direccion,
            SaldoDeudor = deuda - pagado
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(Cliente cliente)
    {
        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync();
        return Ok(cliente);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Cliente cliente)
    {
        if (id != cliente.Id) return BadRequest();
        _context.Entry(cliente).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var cliente = await _context.Clientes.FindAsync(id);
        if (cliente == null) return NotFound();
        cliente.Activo = false;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/cuenta")]
    public async Task<IActionResult> GetCuenta(int id)
    {
        var cliente = await _context.Clientes.FindAsync(id);
        if (cliente == null) return NotFound();

        var movimientos = await _context.CuentaCorrientes
            .Where(c => c.ClienteId == id)
            .OrderByDescending(c => c.Fecha)
            .ToListAsync();

        var deuda = movimientos.Where(m => m.Tipo == "deuda").Sum(m => m.Monto);
        var pagado = movimientos.Where(m => m.Tipo == "pago").Sum(m => m.Monto);

        return Ok(new
        {
            Cliente = cliente.Nombre,
            SaldoDeudor = deuda - pagado,
            Movimientos = movimientos
        });
    }

    [HttpPost("{id}/deuda")]
    public async Task<IActionResult> RegistrarDeuda(int id, [FromBody] CuentaRequest request)
    {
        var cliente = await _context.Clientes.FindAsync(id);
        if (cliente == null) return NotFound();

        var movimiento = new CuentaCorriente
        {
            ClienteId = id,
            Tipo = "deuda",
            Monto = request.Monto,
            Descripcion = request.Descripcion,
            Fecha = DateTime.Now
        };

        _context.CuentaCorrientes.Add(movimiento);
        await _context.SaveChangesAsync();

        return Ok(new { Mensaje = $"Deuda de ${request.Monto} registrada para {cliente.Nombre}." });
    }

    [HttpPost("{id}/pago")]
    public async Task<IActionResult> RegistrarPago(int id, [FromBody] CuentaRequest request)
    {
        var cliente = await _context.Clientes.FindAsync(id);
        if (cliente == null) return NotFound();

        var movimiento = new CuentaCorriente
        {
            ClienteId = id,
            Tipo = "pago",
            Monto = request.Monto,
            Descripcion = request.Descripcion,
            Fecha = DateTime.Now
        };

        _context.CuentaCorrientes.Add(movimiento);
        await _context.SaveChangesAsync();

        return Ok(new { Mensaje = $"Pago de ${request.Monto} registrado para {cliente.Nombre}." });
    }
}

public class CuentaRequest
{
    public decimal Monto { get; set; }
    public string? Descripcion { get; set; }
}