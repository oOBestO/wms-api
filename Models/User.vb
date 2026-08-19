Imports System.ComponentModel.DataAnnotations

Namespace Models
    Public Class User
        <Key>
        Public Property Id As Guid = Guid.NewGuid()

        <Required>
        <MaxLength(200)>
        Public Property Name As String

        <Required>
        <MaxLength(256)>
        Public Property Email As String

        <Required>
        Public Property PasswordHash As String

        <Required>
        <MaxLength(50)>
        Public Property CompanyCode As String

        Public Property CreatedAt As DateTime = DateTime.UtcNow
    End Class
End Namespace
