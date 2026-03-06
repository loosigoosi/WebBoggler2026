
Imports System.Reflection

Friend Class Clients
    Inherits SynchronizedKeyedCollection(Of String, Client)

    Private keyProperty As PropertyInfo

    Public Sub New()
        MyBase.New
        For Each prop In GetType(Client).GetProperties
            ' this requires .net 4, which I couldn't use due to the WPF shadow effect deprication
            'if (property.PropertyType == typeof(TKey) && property.IsDefined(typeof(KeyAttribute), true))
            If ((prop.PropertyType = GetType(String)) _
                        AndAlso ((prop.Name.ToUpper = "CLIENTID") _
                        OrElse (prop.Name.ToUpper = "KEY"))) Then
                Me.keyProperty = prop
                Return
            End If

        Next
        Throw New ArgumentException(String.Format("Unable to find a property in {0} that is named Id or Key and is of type {1}.", GetType(IWSCallback).Name, GetType(String).Name))
    End Sub

    Protected Overrides Function GetKeyForItem(ByVal item As Client) As String
        Return CType(Me.keyProperty.GetValue(item, Nothing), String)
    End Function



    Public Function GetClientByCallback(callback As IWSCallback)
        Return Me.Where(Function(cb) cb.Callback Is callback)
    End Function

End Class
