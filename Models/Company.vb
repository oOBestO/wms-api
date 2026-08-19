Namespace Models
    Public Class Company
        Public Property Code As String
        Public Property Name As String

        Public Sub New(code As String, name As String)
            Me.Code = code
            Me.Name = name
        End Sub
    End Class

    ' Mirrors the frontend's COMPANIES constant (src/types/auth.ts) exactly —
    ' keep both lists in sync if a company is added, renamed, or removed.
    Public Module Companies
        Public ReadOnly All As Company() = {
            New Company("parent", "บริษัท เอบีซี จำกัด"),
            New Company("subsidiary-1", "บริษัท เอบีซี สาขา 1 จำกัด"),
            New Company("subsidiary-2", "บริษัท เอบีซี สาขา 2 จำกัด")
        }

        Public Function Find(code As String) As Company
            Return All.FirstOrDefault(Function(c) c.Code = code)
        End Function
    End Module
End Namespace
