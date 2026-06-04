using System.Collections.Concurrent;

namespace StockApp.Core.Entities;

public class Cliente
{
    public int Id {get; set;}
    public string Nombre{get; set;} = string.Empty;
    public string? Telefono {get; set;}
    public string? Direccion{get; set;}
    public bool Activo{get; set;} = true;
    public DateTime FechaCreacion {get; set;} = DateTime.Now;

    public ICollection<CuentaCorriente> CuentasCOrrientes {get; set;} = new List<CuentaCorriente>();
}