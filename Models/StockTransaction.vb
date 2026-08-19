Imports System.ComponentModel.DataAnnotations

Namespace Models
    Public Class StockTransaction
        <Key>
        Public Property Id As Guid = Guid.NewGuid()

        Public Property MaterialId As Guid

        <Required>
        <MaxLength(50)>
        Public Property CompanyCode As String

        <Required>
        <MaxLength(200)>
        Public Property MaterialName As String

        <Required>
        <MaxLength(50)>
        Public Property Unit As String

        Public Property Delta As Decimal

        Public Property QuantityBefore As Decimal

        Public Property QuantityAfter As Decimal

        ' Nullable + a name snapshot (not a live User reference) because rows written
        ' before this column existed have no known actor, and the account that made
        ' a later adjustment may since have been deleted (see AuthController.DeleteAccount)
        ' — the history should still say who did it at the time, unaffected either way.
        Public Property UserId As Guid?

        <MaxLength(200)>
        Public Property UserName As String

        Public Property CreatedAt As DateTime = DateTime.UtcNow
    End Class
End Namespace
