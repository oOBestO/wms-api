Imports System.ComponentModel.DataAnnotations

Namespace Dtos
    Public Class CreateMaterialDto
        <Required>
        Public Property Name As String

        <Required>
        Public Property Unit As String

        Public Property Quantity As Decimal

        Public Property MinQuantity As Decimal?

        Public Property Note As String
    End Class

    Public Class UpdateMaterialDto
        <Required>
        Public Property Name As String

        <Required>
        Public Property Unit As String

        Public Property MinQuantity As Decimal?

        Public Property Note As String
    End Class

    Public Class AdjustStockDto
        Public Property Delta As Decimal
    End Class
End Namespace
