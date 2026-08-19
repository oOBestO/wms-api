Imports Microsoft.AspNetCore.Mvc
Imports Microsoft.EntityFrameworkCore
Imports wms_api.Data
Imports wms_api.Models

Namespace Controllers
    <ApiController>
    <Route("api/stock-transactions")>
    Public Class StockTransactionsController
        Inherits ControllerBase

        Private ReadOnly _db As WmsDbContext

        Public Sub New(db As WmsDbContext)
            _db = db
        End Sub

        <HttpGet>
        Public Async Function GetAll() As Task(Of ActionResult(Of IEnumerable(Of StockTransaction)))
            Dim transactions = Await _db.StockTransactions.AsNoTracking().
                OrderByDescending(Function(t) t.CreatedAt).ToListAsync()
            Return Ok(transactions)
        End Function
    End Class
End Namespace
