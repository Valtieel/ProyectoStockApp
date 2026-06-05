namespace StockApp.Core.Entities;

public class Producto
{
    public int Id{get; set;}
    public string Nombre{get; set; } = string.Empty;
    public string? Descripcion{get; set;}
    public decimal PrecioVenta{get; set;}
    public decimal Costo {get; set;}
    public int StockActual {get; set;}
    public int StockMinimo{get; set;}
    public DateTime? FechaVencimiento{get; set;}
    public bool Activo {get; set;} = true;
    public DateTime FechaCreacion {get; set;} = DateTime.Now;

    //relacion con categoria (opcional)
    public int? CategoriaId {get; set;}
    public Categoria? Categoria{get; set;}

    public int? ProveedorId { get; set; }
public Proveedor? Proveedor { get; set; }

    public ICollection<Movimiento> Movimientos{get; set;} = new List<Movimiento>();
    public ICollection<DetalleVenta> DetallesVenta{get; set; } = new List<DetalleVenta>();

}