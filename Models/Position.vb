Namespace Models
    Public Class Position
        Public Property Code As String
        Public Property Name As String

        Public Sub New(code As String, name As String)
            Me.Code = code
            Me.Name = name
        End Sub
    End Class

    ' Mirrors the frontend's POSITIONS constant (src/types/auth.ts) exactly —
    ' keep both lists in sync if a position is added, renamed, or removed.
    Public Module Positions
        Public ReadOnly All As Position() = {
            New Position("manager", "ผู้จัดการคลังสินค้า"),
            New Position("staff", "พนักงานคลังสินค้า"),
            New Position("general", "เจ้าหน้าที่ทั่วไป")
        }

        Public Function Find(code As String) As Position
            Return All.FirstOrDefault(Function(p) p.Code = code)
        End Function
    End Module
End Namespace
