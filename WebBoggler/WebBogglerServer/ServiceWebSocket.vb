
Imports System.Threading.Tasks
Imports System.Text
Imports System.ServiceModel.Channels
Imports System.ServiceModel
Imports System.Net.WebSockets

<ServiceBehavior(InstanceContextMode:=InstanceContextMode.PerSession, ConcurrencyMode:=ConcurrencyMode.Reentrant)>
Public Class ServiceWebSocket
    Implements IServiceWebSocket

    Friend Shared Clients As New Clients
    Dim _Random As Random = New System.Random()
    Friend Shared WithEvents ServiceWebBogglerInstance As New ServiceWebBoggler()
    Private Shared RoomMaster As RoomMaster

    Public Sub New()
        _Random = New Random()
        ServiceWebBoggler.ServiceWebSocketInstance = Me
        RoomMaster = ServiceWebBoggler.RoomMaster
    End Sub

#Region "WebSockets"
    Public Async Function SendMessageToServer(msg As Message) As Task Implements IServiceWebSocket.SendMessageToServer
        Dim callback = OperationContext.Current.GetCallbackChannel(Of IWSCallback)()
        If msg.IsEmpty OrElse DirectCast(callback, IChannel).State <> CommunicationState.Opened Then
            Exit Function
        End If

        Dim body As Byte() = msg.GetBody(Of Byte())()
        Dim msgTextFromClient As String = Encoding.UTF8.GetString(body)

        Select Case msgTextFromClient.ToUpper
            Case "REGISTER"
                Dim rnd As Integer = _Random.Next
                Dim key As String = rnd.ToString
                Dim newClient As New Client With {.ClientID = key, .Callback = callback}
                SkimConnections()
                Clients.Add(newClient)
                'ServiceTransponder.IncomingClients.Enqueue(client)
                ServiceWebBogglerInstance.AddPlayer(newClient)
                Await SendMessageAsync(newClient, "REGISTERED")

            Case "REMOVE"
                Dim co As ICommunicationObject = CType(callback, ICommunicationObject)
                If co.State <> CommunicationState.Opened Then
                    Dim stateObj As Object = Nothing
                    Dim result As IAsyncResult = co.BeginClose(AddressOf SkimConnections, stateObj)
                End If

            Case Else 'echo
                Dim msgTextToClient As String = String.Format("Got message {0} at {1}", msgTextFromClient, DateTime.Now.ToLongTimeString())
                Await callback.SendMessageToClient(CreateMessage(msgTextToClient))

        End Select


    End Function

    Friend Shared Async Function SendMessageAsync(client As Client, msg As String) As Task
        Await client.Callback.SendMessageToClient(CreateMessage(msg))
    End Function

    Private Shared Function CreateMessage(msgText As String) As Message
        Dim msg As Message = ByteStreamMessage.CreateMessage(New ArraySegment(Of Byte)(Encoding.UTF8.GetBytes(msgText)))
        msg.Properties("WebSocketMessageProperty") = New WebSocketMessageProperty() With {
            .MessageType = WebSocketMessageType.Text
        }
        Return msg
    End Function

#End Region

    Private Shared Sub SkimConnections()
        Dim lockObject As New Object
        SyncLock (Clients.SyncRoot)
            Dim toDelete As New List(Of Client)
            For Each cli As Client In Clients
                Dim co As ICommunicationObject = CType(cli.Callback, ICommunicationObject)
                If co.State <> CommunicationState.Opened Then
                    toDelete.Add(cli)
                End If
            Next
            For Each item In toDelete
                Clients.Remove(item)
            Next

        End SyncLock
    End Sub
#Region "Event Handlers"

    Private Shared Async Sub ServiceWebBogglerInstance_KeepReady() Handles ServiceWebBogglerInstance.KeepReady
        SkimConnections()
        For Each cli As Client In Clients
            Await SendMessageAsync(cli, "GET_BOARD")
        Next
    End Sub

    Private Shared Async Sub ServiceWebBogglerInstance_StartNewRound() Handles ServiceWebBogglerInstance.StartNewRound
        SkimConnections()
        For Each cli As Client In Clients
            Await SendMessageAsync(cli, "START_ROUND")
        Next
    End Sub

    Private Shared Async Sub ServiceWebBogglerInstance_EndRound() Handles ServiceWebBogglerInstance.EndRound
        SkimConnections()
        For Each cli As Client In Clients
            Await SendMessageAsync(cli, "END_ROUND")
        Next
    End Sub

    Private Shared Async Sub ServiceWebBogglerInstance_ShowTime() Handles ServiceWebBogglerInstance.ShowTime
        SkimConnections()
        For Each cli As Client In Clients
            Await SendMessageAsync(cli, "SHOW_TIME")
        Next
    End Sub



#End Region

End Class
