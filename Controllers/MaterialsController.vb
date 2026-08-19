Imports Microsoft.AspNetCore.Authorization
Imports Microsoft.AspNetCore.Mvc
Imports Microsoft.EntityFrameworkCore
Imports wms_api.Data
Imports wms_api.Dtos
Imports wms_api.Models

Namespace Controllers
    <Authorize>
    <ApiController>
    <Route("api/materials")>
    Public Class MaterialsController
        Inherits ControllerBase

        Private ReadOnly _db As WmsDbContext

        Public Sub New(db As WmsDbContext)
            _db = db
        End Sub

        Private Shared Function PlainText(status As Integer, message As String) As ContentResult
            Return New ContentResult With {.StatusCode = status, .ContentType = "text/plain", .Content = message}
        End Function

        ' Materials/StockTransactions share one physical table across all companies,
        ' isolated by this column rather than by separate tables (mirrors how Users
        ' is scoped) — every query and write below must filter/stamp by it.
        Private ReadOnly Property CurrentCompanyCode As String
            Get
                Return User.FindFirst("companyCode")?.Value
            End Get
        End Property

        ' Job-position permission tiers: "manager" can create/edit/delete materials
        ' and adjust stock; "staff" can only adjust stock; "general" is view-only
        ' and never reaches these actions (GetAll/GetById stay open to everyone).
        Private ReadOnly Property CurrentPositionCode As String
            Get
                Return User.FindFirst("positionCode")?.Value
            End Get
        End Property

        Private ReadOnly Property CanManageMaterials As Boolean
            Get
                Return CurrentPositionCode = "manager"
            End Get
        End Property

        Private ReadOnly Property CanAdjustStock As Boolean
            Get
                Return CurrentPositionCode = "manager" OrElse CurrentPositionCode = "staff"
            End Get
        End Property

        Private ReadOnly Property CurrentUserId As Guid?
            Get
                Dim raw = User.FindFirst("userId")?.Value
                Dim id As Guid
                If Guid.TryParse(raw, id) Then Return id
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property CurrentUserName As String
            Get
                Return User.FindFirst("name")?.Value
            End Get
        End Property

        ' Only the parent company can see stock across every company — everyone
        ' else stays scoped to their own CompanyCode like the rest of this controller.
        <HttpGet("all-companies")>
        Public Async Function GetAllCompanies() As Task(Of ActionResult(Of IEnumerable(Of MaterialWithCompanyDto)))
            If CurrentCompanyCode <> "parent" Then
                Return PlainText(403, "เฉพาะบริษัทแม่เท่านั้นที่เข้าถึงข้อมูลนี้ได้")
            End If

            Dim materials = Await _db.Materials.AsNoTracking().
                OrderBy(Function(m) m.CompanyCode).ThenBy(Function(m) m.Name).ToListAsync()

            Dim result = materials.Select(Function(m) New MaterialWithCompanyDto With {
                .Id = m.Id,
                .Name = m.Name,
                .Unit = m.Unit,
                .Quantity = m.Quantity,
                .MinQuantity = m.MinQuantity,
                .Note = m.Note,
                .CompanyCode = m.CompanyCode,
                .CompanyName = If(Companies.Find(m.CompanyCode)?.Name, m.CompanyCode)
            })

            Return Ok(result)
        End Function

        <HttpGet>
        Public Async Function GetAll() As Task(Of ActionResult(Of IEnumerable(Of Material)))
            Dim materials = Await _db.Materials.AsNoTracking().
                Where(Function(m) m.CompanyCode = CurrentCompanyCode).
                OrderBy(Function(m) m.Name).ToListAsync()
            Return Ok(materials)
        End Function

        <HttpGet("{id:guid}")>
        Public Async Function GetById(id As Guid) As Task(Of ActionResult(Of Material))
            Dim material = Await _db.Materials.AsNoTracking().
                FirstOrDefaultAsync(Function(m) m.Id = id AndAlso m.CompanyCode = CurrentCompanyCode)
            If material Is Nothing Then Return NotFound()
            Return Ok(material)
        End Function

        <HttpPost>
        Public Async Function Create(<FromBody> dto As CreateMaterialDto) As Task(Of ActionResult(Of Material))
            If Not CanManageMaterials Then Return PlainText(403, "คุณไม่มีสิทธิ์เพิ่มวัตถุดิบ")
            If Not ModelState.IsValid Then Return BadRequest(ModelState)

            Dim now = DateTime.UtcNow
            Dim material As New Material With {
                .Name = dto.Name,
                .Unit = dto.Unit,
                .CompanyCode = CurrentCompanyCode,
                .Quantity = dto.Quantity,
                .MinQuantity = dto.MinQuantity,
                .Note = dto.Note,
                .CreatedAt = now,
                .UpdatedAt = now
            }

            _db.Materials.Add(material)
            Await _db.SaveChangesAsync()

            Return CreatedAtAction(NameOf(GetById), New With {material.Id}, material)
        End Function

        <HttpPatch("{id:guid}")>
        Public Async Function Update(id As Guid, <FromBody> dto As UpdateMaterialDto) As Task(Of ActionResult(Of Material))
            If Not CanManageMaterials Then Return PlainText(403, "คุณไม่มีสิทธิ์แก้ไขวัตถุดิบ")
            If Not ModelState.IsValid Then Return BadRequest(ModelState)

            Dim material = Await _db.Materials.
                FirstOrDefaultAsync(Function(m) m.Id = id AndAlso m.CompanyCode = CurrentCompanyCode)
            If material Is Nothing Then Return NotFound()

            material.Name = dto.Name
            material.Unit = dto.Unit
            material.MinQuantity = dto.MinQuantity
            material.Note = dto.Note
            material.UpdatedAt = DateTime.UtcNow

            Await _db.SaveChangesAsync()
            Return Ok(material)
        End Function

        <HttpPost("{id:guid}/adjust-stock")>
        Public Async Function AdjustStock(id As Guid, <FromBody> dto As AdjustStockDto) As Task(Of ActionResult(Of Material))
            If Not CanAdjustStock Then Return PlainText(403, "คุณไม่มีสิทธิ์ปรับสต๊อก")

            Dim material = Await _db.Materials.
                FirstOrDefaultAsync(Function(m) m.Id = id AndAlso m.CompanyCode = CurrentCompanyCode)
            If material Is Nothing Then Return NotFound()

            Dim quantityBefore = material.Quantity
            Dim newQuantity = material.Quantity + dto.Delta
            If newQuantity < 0 Then
                ModelState.AddModelError(NameOf(dto.Delta), "Resulting quantity cannot be negative.")
                Return BadRequest(ModelState)
            End If

            Dim now = DateTime.UtcNow
            material.Quantity = newQuantity
            material.UpdatedAt = now

            _db.StockTransactions.Add(New StockTransaction With {
                .MaterialId = material.Id,
                .CompanyCode = material.CompanyCode,
                .MaterialName = material.Name,
                .Unit = material.Unit,
                .Delta = dto.Delta,
                .QuantityBefore = quantityBefore,
                .QuantityAfter = newQuantity,
                .UserId = CurrentUserId,
                .UserName = CurrentUserName,
                .CreatedAt = now
            })

            Await _db.SaveChangesAsync()
            Return Ok(material)
        End Function

        <HttpDelete("{id:guid}")>
        Public Async Function Delete(id As Guid) As Task(Of IActionResult)
            If Not CanManageMaterials Then Return PlainText(403, "คุณไม่มีสิทธิ์ลบวัตถุดิบ")

            Dim material = Await _db.Materials.
                FirstOrDefaultAsync(Function(m) m.Id = id AndAlso m.CompanyCode = CurrentCompanyCode)
            If material Is Nothing Then Return NotFound()

            _db.Materials.Remove(material)
            Await _db.SaveChangesAsync()
            Return NoContent()
        End Function
    End Class
End Namespace
