Imports Microsoft.AspNetCore.Authorization
Imports Microsoft.AspNetCore.Mvc
Imports Microsoft.EntityFrameworkCore
Imports wms_api.Data
Imports wms_api.Models

Namespace Controllers
    <Authorize>
    <ApiController>
    <Route("api/stock-transactions")>
    Public Class StockTransactionsController
        Inherits ControllerBase

        Private ReadOnly _db As WmsDbContext

        Public Sub New(db As WmsDbContext)
            _db = db
        End Sub

        Private ReadOnly Property CurrentCompanyCode As String
            Get
                Return User.FindFirst("companyCode")?.Value
            End Get
        End Property

        <HttpGet>
        Public Async Function GetAll() As Task(Of ActionResult(Of IEnumerable(Of StockTransaction)))
            Dim transactions = Await _db.StockTransactions.AsNoTracking().
                Where(Function(t) t.CompanyCode = CurrentCompanyCode).
                OrderByDescending(Function(t) t.CreatedAt).ToListAsync()
            Return Ok(transactions)
        End Function
    End Class
End Namespace
