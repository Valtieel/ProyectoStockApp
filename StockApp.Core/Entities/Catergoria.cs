namespace StockApp.Core.Entities;

public class Categoria
{
    public int Id {get; set;}
    public string Nombre {get; set; } = string.Empty;
    public string? Descripcion {get; set;}
    public bool Activo {get; set;} = true;

    //una catergoria tiene muchos productos
    public ICollection<Producto> Productos {get; set;} = new List<Producto>();
}