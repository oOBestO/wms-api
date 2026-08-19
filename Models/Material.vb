Imports System.ComponentModel.DataAnnotations

Namespace Models
    Public Class Material
        <Key>
        Public Property Id As Guid = Guid.NewGuid()

        <Required>
        <MaxLength(200)>
        Public Property Name As String

        <Required>
        <MaxLength(50)>
        Public Property Unit As String

        <Required>
        <MaxLength(50)>
        Public Property CompanyCode As String

        Public Property Quantity As Decimal

        Public Property MinQuantity As Decimal?

        <MaxLength(1000)>
        Public Property Note As String

        Public Property CreatedAt As DateTime = DateTime.UtcNow

        Public Property UpdatedAt As DateTime = DateTime.UtcNow
    End Class
End Namespace
