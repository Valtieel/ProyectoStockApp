using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockApp.Data;

namespace StockApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportesController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReportesController(AppDbContext context)
    {
        _context = context;
    }

    // GET api/reportes/ventas?añoDesde=2026&mesDesde=1&añoHasta=2026&mesHasta=5
    // Exporta las ventas de un período a Excel
    [HttpGet("ventas")]
    public async Task<IActionResult> ExportarVentas(
        [FromQuery] int añoDesde, [FromQuery] int mesDesde,
        [FromQuery] int añoHasta, [FromQuery] int mesHasta)
    {
        var fechaDesde = new DateTime(añoDesde, mesDesde, 1);
        var fechaHasta = new DateTime(añoHasta, mesHasta,
            DateTime.DaysInMonth(añoHasta, mesHasta));

        var ventas = await _context.Ventas
            .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto)
            .Where(v => v.Fecha >= fechaDesde && v.Fecha <= fechaHasta)
            .OrderBy(v => v.Fecha)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var hoja = workbook.Worksheets.Add("Ventas");

        // Encabezados
        hoja.Cell(1, 1).Value = "Fecha";
        hoja.Cell(1, 2).Value = "Hora";
        hoja.Cell(1, 3).Value = "Producto";
        hoja.Cell(1, 4).Value = "Cantidad";
        hoja.Cell(1, 5).Value = "Precio Unitario";
        hoja.Cell(1, 6).Value = "Subtotal";
        hoja.Cell(1, 7).Value = "Método de Pago";
        hoja.Cell(1, 8).Value = "Total Venta";

        // Estilo encabezados
        var encabezados = hoja.Range("A1:H1");
        encabezados.Style.Font.Bold = true;
        encabezados.Style.Fill.BackgroundColor = XLColor.FromHtml("#3498db");
        encabezados.Style.Font.FontColor = XLColor.White;

        int fila = 2;
        foreach (var venta in ventas)
        {
            foreach (var detalle in venta.Detalles)
            {
                hoja.Cell(fila, 1).Value = venta.Fecha.ToString("dd/MM/yyyy");
                hoja.Cell(fila, 2).Value = venta.Fecha.ToString("HH:mm");
                hoja.Cell(fila, 3).Value = detalle.Producto?.Nombre ?? "-";
                hoja.Cell(fila, 4).Value = detalle.Cantidad;
                hoja.Cell(fila, 5).Value = (double)detalle.PrecioUnitario;
                hoja.Cell(fila, 6).Value = (double)(detalle.Cantidad * detalle.PrecioUnitario);
                hoja.Cell(fila, 7).Value = venta.MetodoPago;
                hoja.Cell(fila, 8).Value = (double)venta.Total;
                fila++;
            }
        }

        hoja.Columns().AdjustToContents();

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Ventas_{mesDesde}-{añoDesde}_al_{mesHasta}-{añoHasta}.xlsx");
    }

    // GET api/reportes/stock
    // Exporta el stock actual a Excel
    [HttpGet("stock")]
    public async Task<IActionResult> ExportarStock()
    {
        var productos = await _context.Productos
            .Include(p => p.Categoria)
            .Where(p => p.Activo)
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var hoja = workbook.Worksheets.Add("Stock");

        hoja.Cell(1, 1).Value = "Nombre";
        hoja.Cell(1, 2).Value = "Categoría";
        hoja.Cell(1, 3).Value = "Stock Actual";
        hoja.Cell(1, 4).Value = "Stock Mínimo";
        hoja.Cell(1, 5).Value = "Precio Venta";
        hoja.Cell(1, 6).Value = "Costo";
        hoja.Cell(1, 7).Value = "Vencimiento";
        hoja.Cell(1, 8).Value = "Estado";

        var encabezados = hoja.Range("A1:H1");
        encabezados.Style.Font.Bold = true;
        encabezados.Style.Fill.BackgroundColor = XLColor.FromHtml("#2ecc71");
        encabezados.Style.Font.FontColor = XLColor.White;

        int fila = 2;
        foreach (var p in productos)
        {
            hoja.Cell(fila, 1).Value = p.Nombre;
            hoja.Cell(fila, 2).Value = p.Categoria?.Nombre ?? "-";
            hoja.Cell(fila, 3).Value = p.StockActual;
            hoja.Cell(fila, 4).Value = p.StockMinimo;
            hoja.Cell(fila, 5).Value = (double)p.PrecioVenta;
            hoja.Cell(fila, 6).Value = (double)p.Costo;
            hoja.Cell(fila, 7).Value = p.FechaVencimiento.HasValue
                ? p.FechaVencimiento.Value.ToString("dd/MM/yyyy") : "-";
            hoja.Cell(fila, 8).Value = p.StockActual <= p.StockMinimo
                ? "STOCK BAJO" : "OK";

            if (p.StockActual <= p.StockMinimo)
                hoja.Row(fila).Style.Fill.BackgroundColor = XLColor.FromHtml("#ffe5e5");

            fila++;
        }

        hoja.Columns().AdjustToContents();

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Stock_{DateTime.Now:dd-MM-yyyy}.xlsx");
    }

     // GET api/reportes/movimientos?añoDesde=2026&mesDesde=1&añoHasta=2026&mesHasta=5
    // Exporta los movimientos de un período a Excel

    [HttpGet("movimientos")]
    public async Task<IActionResult> ExportarMovimientos(
        [FromQuery] int añoDesde, [FromQuery] int mesDesde,
        [FromQuery] int añoHasta, [FromQuery] int mesHasta)
    {
        var fechaDesde = new DateTime(añoDesde, mesDesde, 1);
        var fechaHasta = new DateTime(añoHasta, mesHasta,
        DateTime.DaysInMonth(añoHasta, mesHasta));

        var movimientos = await _context.Movimientos
        .Include(m => m.Producto)
        .Where(m => m.Fecha >= fechaDesde && m.Fecha <= fechaHasta)
        .OrderBy(m => m.Fecha)
        .ToListAsync();

        using var workbook = new XLWorkbook();
        var hoja = workbook.Worksheets.Add("Movimientos");

        hoja.Cell(1, 1).Value = "Fecha";
        hoja.Cell(1, 2).Value = "Hora";
        hoja.Cell(1, 3).Value = "Producto";
        hoja.Cell(1, 4).Value = "Tipo";
        hoja.Cell(1, 5).Value = "Cantidad";
        hoja.Cell(1, 6).Value = "Motivo";

        var encabezados = hoja.Range("A1:F1");
        encabezados.Style.Font.Bold = true;
        encabezados.Style.Fill.BackgroundColor = XLColor.FromHtml("#e67e22");
        encabezados.Style.Font.FontColor = XLColor.White;

        int fila = 2;
        foreach(var m in movimientos)
        {
            hoja.Cell(fila, 1).Value = m.Fecha.ToString("dd/MM/yyyy");
            hoja.Cell(fila, 2).Value = m.Fecha.ToString("HH:mm");
            hoja.Cell(fila, 3).Value = m.Producto?.Nombre ?? "-";
            hoja.Cell(fila, 4).Value = m.Tipo;
            hoja.Cell(fila, 5).Value = m.Cantidad;
            hoja.Cell(fila, 6).Value = m.Motivo ?? "-";

            if (m.Tipo == "entrada")
            hoja.Row(fila).Style.Fill.BackgroundColor = XLColor.FromHtml("#eafaf1");
            else
            hoja.Row(fila).Style.Fill.BackgroundColor = XLColor.FromHtml("#fdecea");

            fila++;
        }
        hoja.Columns().AdjustToContents();

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return File(stream.ToArray(),
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
         $"Movimientos_{mesDesde}-{añoDesde}_al_{mesHasta}-{añoHasta}.xlsx"); 

    }

}