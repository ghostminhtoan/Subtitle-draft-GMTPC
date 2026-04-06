Namespace Models

    ''' <summary>
    ''' Lưu thông tin một prompt với ID cố định
    ''' </summary>
    Public Class PromptItem
        Public Property Id As Integer
        Public Property Name As String
        Public Property Content As String

        Public Sub New()
        End Sub

        Public Sub New(id As Integer, name As String, content As String)
            Me.Id = id
            Me.Name = name
            Me.Content = content
        End Sub

        ''' <summary>
        ''' Trả về tên hiển thị dạng "STT. Tên"
        ''' </summary>
        Public ReadOnly Property DisplayName As String
            Get
                Return String.Format("{0}. {1}", Me.Id, Me.Name)
            End Get
        End Property
    End Class

End Namespace
