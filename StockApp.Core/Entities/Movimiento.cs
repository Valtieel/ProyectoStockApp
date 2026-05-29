namespace StockApp.Core.Entities;

public class Movimiento
{
    public int Id{get; set;}
    public int ProductoId{get; set;}
    public string Tipo{get; set; } = string.Empty; //"entrada" o "salida"
    public int Cantidad {get; set;}
    public string? Motivo{get; set;}
    public DateTime Fecha{get; set;} = DateTime.Now;

    public Producto Producto {get; set;} = null;
}