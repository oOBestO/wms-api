Imports Microsoft.EntityFrameworkCore
Imports wms_api.Models

Namespace Data
    Public Class WmsDbContext
        Inherits DbContext

        Public Sub New(options As DbContextOptions(Of WmsDbContext))
            MyBase.New(options)
        End Sub

        Public Property Materials As DbSet(Of Material)
        Public Property StockTransactions As DbSet(Of StockTransaction)
        Public Property Users As DbSet(Of User)

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            modelBuilder.Entity(Of Material)(Sub(entity)
                                                  entity.Property(Function(m) m.Quantity).HasColumnType("decimal(18,3)")
                                                  entity.Property(Function(m) m.MinQuantity).HasColumnType("decimal(18,3)")
                                              End Sub)

            modelBuilder.Entity(Of StockTransaction)(Sub(entity)
                                                          entity.Property(Function(t) t.Delta).HasColumnType("decimal(18,3)")
                                                          entity.Property(Function(t) t.QuantityBefore).HasColumnType("decimal(18,3)")
                                                          entity.Property(Function(t) t.QuantityAfter).HasColumnType("decimal(18,3)")
                                                          entity.HasIndex(Function(t) t.MaterialId)
                                                      End Sub)

            modelBuilder.Entity(Of User)(Sub(entity)
                                              entity.HasIndex(Function(u) u.Email).IsUnique()
                                          End Sub)
        End Sub
    End Class
End Namespace
