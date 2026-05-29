using Microsoft.EntityFrameworkCore;
using StockApp.Core.Entities;

namespace StockApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options){}

    public DbSet<Producto> Productos {get; set;}
    public DbSet<Movimiento> Movimientos {get; set;}
    public DbSet<Venta> Ventas {get; set;}
    public DbSet<DetalleVenta> DetalleVentas{get; set;}
    public DbSet<Caja> Cajas {get; set;}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Producto>(entity =>
        {
         entity.Property(p => p.Nombre).IsRequired().HasMaxLength(200);
         entity.Property(p => p.PrecioVenta).HasPrecision(10, 2);
         entity.Property(p => p.Costo).HasPrecision(10, 2);   
        });

        modelBuilder.Entity<Venta>(entity =>
        {
         entity.Property(v => v.Total).HasPrecision(10, 2);
         entity.Property(v => v.MetodoPago).HasMaxLength(50);   
        });

       modelBuilder.Entity<DetalleVenta>(entity =>
        {
            entity.Property(d => d.PrecioUnitario).HasPrecision(10, 2);
            entity.Ignore(d => d.Subtotal);
        });

        modelBuilder.Entity<Caja>(entity =>
        {
            entity.Property(c => c.MontoInicial).HasPrecision(10, 2);
            entity.Property(c => c.MontoFinal).HasPrecision(10, 2);
        });
    }
}