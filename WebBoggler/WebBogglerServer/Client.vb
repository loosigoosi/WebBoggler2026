Friend Class Client

    Private _ClientID As String
    Private _Callback As IWSCallback

    <Key>
    Public Property ClientID As String
        Get
            Return _ClientID
        End Get
        Set(value As String)
            _ClientID = value
        End Set
    End Property

    Public Property Callback As IWSCallback
        Get
            Return _Callback
        End Get
        Set(value As IWSCallback)
            _Callback = value
        End Set

    End Property

    Private Class Key
        Inherits Attribute
    End Class
End Class
