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

        <HttpGet>
        Public Async Function GetAll() As Task(Of ActionResult(Of IEnumerable(Of Material)))
            Dim materials = Await _db.Materials.AsNoTracking().OrderBy(Function(m) m.Name).ToListAsync()
            Return Ok(materials)
        End Function

        <HttpGet("{id:guid}")>
        Public Async Function GetById(id As Guid) As Task(Of ActionResult(Of Material))
            Dim material = Await _db.Materials.AsNoTracking().FirstOrDefaultAsync(Function(m) m.Id = id)
            If material Is Nothing Then Return NotFound()
            Return Ok(material)
        End Function

        <HttpPost>
        Public Async Function Create(<FromBody> dto As CreateMaterialDto) As Task(Of ActionResult(Of Material))
            If Not ModelState.IsValid Then Return BadRequest(ModelState)

            Dim now = DateTime.UtcNow
            Dim material As New Material With {
                .Name = dto.Name,
                .Unit = dto.Unit,
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
            If Not ModelState.IsValid Then Return BadRequest(ModelState)

            Dim material = Await _db.Materials.FirstOrDefaultAsync(Function(m) m.Id = id)
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
            Dim material = Await _db.Materials.FirstOrDefaultAsync(Function(m) m.Id = id)
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
                .MaterialName = material.Name,
                .Unit = material.Unit,
                .Delta = dto.Delta,
                .QuantityBefore = quantityBefore,
                .QuantityAfter = newQuantity,
                .CreatedAt = now
            })

            Await _db.SaveChangesAsync()
            Return Ok(material)
        End Function

        <HttpDelete("{id:guid}")>
        Public Async Function Delete(id As Guid) As Task(Of IActionResult)
            Dim material = Await _db.Materials.FirstOrDefaultAsync(Function(m) m.Id = id)
            If material Is Nothing Then Return NotFound()

            _db.Materials.Remove(material)
            Await _db.SaveChangesAsync()
            Return NoContent()
        End Function
    End Class
End Namespace
