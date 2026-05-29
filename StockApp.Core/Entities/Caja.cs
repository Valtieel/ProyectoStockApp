namespace StockApp.Core.Entities;

public class Caja
{
    public int Id { get; set; }
    public decimal MontoInicial { get; set; }
    public decimal? MontoFinal { get; set; }
    public DateTime FechaApertura { get; set; } = DateTime.Now;
    public DateTime? FechaCierre { get; set; }
    public bool Abierta { get; set; } = true;

    public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
}