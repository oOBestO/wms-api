Imports System.ComponentModel.DataAnnotations

Namespace Models
    Public Class StockTransaction
        <Key>
        Public Property Id As Guid = Guid.NewGuid()

        Public Property MaterialId As Guid

        <Required>
        <MaxLength(200)>
        Public Property MaterialName As String

        <Required>
        <MaxLength(50)>
        Public Property Unit As String

        Public Property Delta As Decimal

        Public Property QuantityBefore As Decimal

        Public Property QuantityAfter As Decimal

        Public Property CreatedAt As DateTime = DateTime.UtcNow
    End Class
End Namespace
