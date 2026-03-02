
Imports System.Threading.Tasks
Imports System.Text
Imports System.ServiceModel.Channels
Imports System.ServiceModel
Imports System.Net.WebSockets

<ServiceBehavior(InstanceContextMode:=InstanceContextMode.PerSession, ConcurrencyMode:=ConcurrencyMode.Multiple)>
Public Class ServiceWebSocket
    Implements IServiceWebSocket

    Private Shared WSClients As New Clients
    Private _Random As Random = New System.Random()
    Friend Shared WithEvents ServiceWebBogglerInstance As New ServiceWebBoggler()
    Private Shared RoomMaster As RoomMaster
    Private ChannelClosedEventHandler As New EventHandler(AddressOf Channel_Closed)
    Private ChannelFaultedEventHandler As New EventHandler(AddressOf Channel_Faulted)

    Public Sub New()
        _Random = New Random()
        ServiceWebBoggler.ServiceWebSocketInstance = Me
    End Sub

#Region "WebSockets"
    Public Async Function MessageListener(msg As Message) As Task Implements IServiceWebSocket.MessageListener
        Dim callback = OperationContext.Current.GetCallbackChannel(Of IWSCallback)()

        'Non c'è modo in VB di determinare se un evento ha già uno handler registrato, ma basta rimuoverlo prima
        'Se non era registrato, removehandler non ha effetto.
        RemoveHandler OperationContext.Current.Channel.Faulted, ChannelFaultedEventHandler
        RemoveHandler OperationContext.Current.Channel.Closed, ChannelClosedEventHandler
        AddHandler OperationContext.Current.Channel.Faulted, ChannelFaultedEventHandler
        AddHandler OperationContext.Current.Channel.Closed, ChannelClosedEventHandler


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
                WSClients.Add(newClient)

                Await SendMessageAsync(newClient, "REGISTERED")
                Await SendMessageAsync(newClient, "#" + Trim(key.ToString))

            Case "REMOVE"
                Dim co As ICommunicationObject = CType(callback, ICommunicationObject)
                If co.State <> CommunicationState.Opened Then
                    Dim stateObj As Object = Nothing
                    Dim result As IAsyncResult = co.BeginClose(AddressOf SkimConnections, stateObj)
                End If

            Case "READY"
                ServiceWebBogglerInstance.SetPlayerReadyState(WSClients.GetClientByCallback(callback).ClientID, True)

            Case "NOTREADY"
                ServiceWebBogglerInstance.SetPlayerReadyState(WSClients.GetClientByCallback(callback).ClientID, False)

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
        msg.Properties("WebSocketMessageProperty") = New WebSocketMessageProperty() With {.MessageType = WebSocketMessageType.Text}
        Return msg
    End Function

#End Region

    Friend ReadOnly Property Clients As Clients
        Get
            Return WSClients
        End Get
    End Property

    Private Shared Sub SkimConnections()
        Dim lockObject As New Object
        SyncLock (WSClients.SyncRoot)
            Dim toDelete As New List(Of Client)
            For Each cli As Client In WSClients
                Dim co As ICommunicationObject = CType(cli.Callback, ICommunicationObject)
                If co.State <> CommunicationState.Opened Then
                    toDelete.Add(cli)
                End If
            Next
            For Each item In toDelete
                WSClients.Remove(item)
                ServiceWebBogglerInstance.RemovePlayer(item.ClientID)
            Next

        End SyncLock
    End Sub

    Private Shared Async Sub SendUpdatePlayersRequest()
        For Each cli As Client In WSClients
            Await SendMessageAsync(cli, "UPDATE_PLAYERS")
        Next
    End Sub

#Region "Event Handlers"
    Private Sub Channel_Closed(sender As Object, e As EventArgs)
        SkimConnections()
    End Sub

    Private Sub Channel_Faulted(sender As Object, e As EventArgs)
        SkimConnections()
    End Sub

    Private Shared Async Sub ServiceWebBogglerInstance_KeepReady() Handles ServiceWebBogglerInstance.KeepReady
        SkimConnections()
        For Each cli As Client In WSClients
            Await SendMessageAsync(cli, "GET_BOARD")
        Next
    End Sub

    Private Shared Async Sub ServiceWebBogglerInstance_StartNewRound() Handles ServiceWebBogglerInstance.StartNewRound
        SkimConnections()
        For Each cli As Client In WSClients
            Await SendMessageAsync(cli, "START_ROUND")
        Next
    End Sub

    Private Shared Async Sub ServiceWebBogglerInstance_EndRound() Handles ServiceWebBogglerInstance.EndRound
        SkimConnections()
        For Each cli As Client In WSClients
            Await SendMessageAsync(cli, "END_ROUND")
        Next
    End Sub

    Private Shared Async Sub ServiceWebBogglerInstance_ShowTime() Handles ServiceWebBogglerInstance.ShowTime
        SkimConnections()
        For Each cli As Client In WSClients
            Await SendMessageAsync(cli, "SHOW_TIME")
        Next
    End Sub

    Private Shared Sub ServiceWebBogglerInstance_NewPlayer() Handles ServiceWebBogglerInstance.NewPlayer
        SkimConnections()
        SendUpdatePlayersRequest()
    End Sub

    Private Shared Sub ServiceWebBogglerInstance_RemovedPlayer() Handles ServiceWebBogglerInstance.RemovedPlayer
        SkimConnections()
        SendUpdatePlayersRequest()
    End Sub



#End Region

End Class
