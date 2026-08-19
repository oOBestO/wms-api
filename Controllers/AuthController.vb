Imports Microsoft.AspNetCore.Mvc
Imports Microsoft.EntityFrameworkCore
Imports wms_api.Data
Imports wms_api.Dtos
Imports wms_api.Models
Imports wms_api.Services

Namespace Controllers
    <ApiController>
    <Route("api/auth")>
    Public Class AuthController
        Inherits ControllerBase

        Private ReadOnly _db As WmsDbContext
        Private ReadOnly _tokens As JwtTokenService

        Public Sub New(db As WmsDbContext, tokens As JwtTokenService)
            _db = db
            _tokens = tokens
        End Sub

        Private Shared Function PlainText(status As Integer, message As String) As ContentResult
            Return New ContentResult With {.StatusCode = status, .ContentType = "text/plain", .Content = message}
        End Function

        Private Shared Function ToDto(user As User) As AuthUserDto
            Dim company = Companies.Find(user.CompanyCode)
            Return New AuthUserDto With {
                .Id = user.Id,
                .Name = user.Name,
                .Email = user.Email,
                .CompanyCode = user.CompanyCode,
                .CompanyName = If(company IsNot Nothing, company.Name, user.CompanyCode)
            }
        End Function

        <HttpPost("login")>
        Public Async Function Login(<FromBody> dto As LoginDto) As Task(Of ActionResult(Of AuthResponseDto))
            If Not ModelState.IsValid Then Return BadRequest(ModelState)

            Dim email = dto.Email.Trim()
            Dim user = Await _db.Users.FirstOrDefaultAsync(Function(u) u.Email = email)
            If user Is Nothing OrElse Not PasswordHasher.Verify(dto.Password, user.PasswordHash) Then
                Return PlainText(401, "อีเมลหรือรหัสผ่านไม่ถูกต้อง")
            End If

            Dim response As New AuthResponseDto With {
                .Token = _tokens.GenerateToken(user),
                .User = ToDto(user)
            }
            Return Ok(response)
        End Function

        <HttpPost("register")>
        Public Async Function Register(<FromBody> dto As RegisterDto) As Task(Of ActionResult(Of AuthResponseDto))
            If Not ModelState.IsValid Then Return BadRequest(ModelState)

            If Companies.Find(dto.CompanyCode) Is Nothing Then
                Return PlainText(400, "ไม่พบบริษัทที่เลือก")
            End If

            Dim email = dto.Email.Trim()
            Dim exists = Await _db.Users.AnyAsync(Function(u) u.Email = email)
            If exists Then
                Return PlainText(409, "อีเมลนี้ถูกใช้งานแล้ว")
            End If

            Dim now = DateTime.UtcNow
            Dim user As New User With {
                .Name = dto.Name.Trim(),
                .Email = email,
                .PasswordHash = PasswordHasher.Hash(dto.Password),
                .CompanyCode = dto.CompanyCode,
                .CreatedAt = now
            }

            _db.Users.Add(user)
            Await _db.SaveChangesAsync()

            Dim response As New AuthResponseDto With {
                .Token = _tokens.GenerateToken(user),
                .User = ToDto(user)
            }
            Return Ok(response)
        End Function
    End Class
End Namespace
