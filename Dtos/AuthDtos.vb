Imports System.ComponentModel.DataAnnotations

Namespace Dtos
    Public Class LoginDto
        <Required>
        <EmailAddress>
        Public Property Email As String

        <Required>
        Public Property Password As String
    End Class

    Public Class RegisterDto
        <Required>
        Public Property Name As String

        <Required>
        <EmailAddress>
        Public Property Email As String

        <Required>
        <MinLength(6)>
        Public Property Password As String

        <Required>
        Public Property CompanyCode As String
    End Class

    Public Class AuthUserDto
        Public Property Id As Guid
        Public Property Name As String
        Public Property Email As String
        Public Property CompanyCode As String
        Public Property CompanyName As String
    End Class

    Public Class AuthResponseDto
        Public Property Token As String
        Public Property User As AuthUserDto
    End Class
End Namespace
