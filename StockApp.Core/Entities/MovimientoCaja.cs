namespace StockApp.Core.Entities;

// Representa un movimiento manual de dinero en la caja, sin estar asociado
// a una venta de producto: por ejemplo un retiro de efectivo, un pago a un
// proveedor, o un ingreso extra (vuelto de cambio, aporte del dueño, etc).
public class MovimientoCaja
{
    public int Id { get; set; }

    // A qué caja (turno) pertenece este movimiento.
    public int CajaId { get; set; }
    public Caja? Caja { get; set; }

    // "ingreso" o "egreso"
    public string Tipo { get; set; } = string.Empty;

    public decimal Monto { get; set; }

    // Ej: "Retiro de caja", "Pago a proveedor - Dist. Norte"
    public string Descripcion { get; set; } = string.Empty;

    public DateTime Fecha { get; set; } = DateTime.Now;
}