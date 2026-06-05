using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockApp.Core.Entities;
using StockApp.Data;

namespace StockApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProveedoresController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProveedoresController(AppDbContext context)
    {
        _context = context;
    }

    // GET api/proveedores
    // Devuelve todos los proveedores activos
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var proveedores = await _context.Proveedores
            .Where(p => p.Activo)
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        return Ok(proveedores);
    }

    // GET api/proveedores/{id}
    // Devuelve un proveedor con sus productos
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var proveedor = await _context.Proveedores
            .Include(p => p.Productos.Where(pr => pr.Activo))
            .FirstOrDefaultAsync(p => p.Id == id && p.Activo);

        if (proveedor == null) return NotFound();

        return Ok(proveedor);
    }

    // POST api/proveedores
    // Crea un nuevo proveedor
    [HttpPost]
    public async Task<IActionResult> Create(Proveedor proveedor)
    {
        _context.Proveedores.Add(proveedor);
        await _context.SaveChangesAsync();
        return Ok(proveedor);
    }

    // PUT api/proveedores/{id}
    // Edita un proveedor
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Proveedor proveedor)
    {
        if (id != proveedor.Id) return BadRequest();
        _context.Entry(proveedor).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE api/proveedores/{id}
    // Elimina un proveedor (soft delete)
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var proveedor = await _context.Proveedores.FindAsync(id);
        if (proveedor == null) return NotFound();
        proveedor.Activo = false;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // GET api/proveedores/{id}/productos
    // Devuelve todos los productos de un proveedor
    [HttpGet("{id}/productos")]
    public async Task<IActionResult> GetProductos(int id)
    {
        var productos = await _context.Productos
            .Where(p => p.ProveedorId == id && p.Activo)
            .ToListAsync();

        return Ok(productos);
    }
}