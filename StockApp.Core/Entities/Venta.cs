namespace StockApp.Core.Entities;

public class Venta
{
    public int Id {get; set;}
    public int CajaId {get; set;}
    public decimal Total {get; set;}
    public string MetodoPago{get; set;} = string.Empty; //"efectivo" "transferencia "tarjeta"
    public DateTime Fecha {get; set;} = DateTime.Now;

    public Caja Caja{get; set;} = null!;
    public ICollection<DetalleVenta> Detalles {get; set;} = new List<DetalleVenta>();

}