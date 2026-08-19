Imports System.IdentityModel.Tokens.Jwt
Imports System.Security.Claims
Imports System.Text
Imports Microsoft.Extensions.Configuration
Imports Microsoft.IdentityModel.Tokens
Imports wms_api.Models

Namespace Services
    Public Class JwtTokenService
        Private ReadOnly _key As String
        Private ReadOnly _issuer As String
        Private ReadOnly _audience As String
        Private ReadOnly _expiresInDays As Integer

        Public Sub New(configuration As IConfiguration)
            _key = configuration.GetValue(Of String)("Jwt:Key")
            _issuer = configuration.GetValue(Of String)("Jwt:Issuer")
            _audience = configuration.GetValue(Of String)("Jwt:Audience")
            _expiresInDays = configuration.GetValue(Of Integer)("Jwt:ExpiresInDays", 7)
        End Sub

        Public Function GenerateToken(user As User) As String
            Dim claims As Claim() = {
                New Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                New Claim(JwtRegisteredClaimNames.Email, user.Email),
                New Claim("name", user.Name),
                New Claim("companyCode", user.CompanyCode),
                New Claim("positionCode", user.PositionCode),
                New Claim("userId", user.Id.ToString())
            }

            Dim signingKey = New SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key))
            Dim credentials = New SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)

            Dim token = New JwtSecurityToken(
                issuer:=_issuer,
                audience:=_audience,
                claims:=claims,
                expires:=DateTime.UtcNow.AddDays(_expiresInDays),
                signingCredentials:=credentials)

            Return New JwtSecurityTokenHandler().WriteToken(token)
        End Function
    End Class
End Namespace
