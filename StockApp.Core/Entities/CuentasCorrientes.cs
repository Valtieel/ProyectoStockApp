namespace StockApp.Core.Entities;

public class CuentaCorriente
{
    public int Id {get; set;}
    public int ClienteId {get; set;}
    public string Tipo{get; set;} = string.Empty; //"deuda" o "pago"
    public decimal Monto {get; set;}
    public string? Descripcion {get; set;}
    public DateTime Fecha {get; set;} = DateTime.Now;

    public Cliente Cliente {get; set;} = null!;
}