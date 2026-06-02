using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockApp.Core.Entities;
using StockApp.Data;

namespace StockApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriasController : ControllerBase
{
    private readonly AppDbContext _context;

    public CategoriasController(AppDbContext context)
    {
        _context = context;
    }

    // GET api/categorias
    // Devuelve todas las categorias activas
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categorias = await _context.Categorias
            .Where(c => c.Activo)
            .OrderBy(c => c.Nombre)
            .ToListAsync();

        return Ok(categorias);
    }

    // POST api/categorias
    // Crea una nueva categoria
    [HttpPost]
    public async Task<IActionResult> Create(Categoria categoria)
    {
        _context.Categorias.Add(categoria);
        await _context.SaveChangesAsync();
        return Ok(categoria);
    }

    // PUT api/categorias/{id}
    // Edita una categoria existente
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Categoria categoria)
    {
        if (id != categoria.Id) return BadRequest();
        _context.Entry(categoria).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE api/categorias/{id}
    // Elimina una categoria (soft delete)
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var categoria = await _context.Categorias.FindAsync(id);
        if (categoria == null) return NotFound();
        categoria.Activo = false;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // GET api/categorias/{id}/productos
    // Devuelve todos los productos de una categoria
    [HttpGet("{id}/productos")]
    public async Task<IActionResult> GetProductos(int id)
    {
        var productos = await _context.Productos
            .Where(p => p.CategoriaId == id && p.Activo)
            .ToListAsync();

        return Ok(productos);
    }
}