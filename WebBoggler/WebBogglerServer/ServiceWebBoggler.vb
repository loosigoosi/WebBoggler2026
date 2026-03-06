
Imports System.Threading.Tasks
Imports System.Text
Imports System.ServiceModel.Channels

Imports System.Net.WebSockets
Imports BigBoggler_Common


<CORSSupportBehavior>
<ServiceBehavior(InstanceContextMode:=InstanceContextMode.PerSession, ConcurrencyMode:=ConcurrencyMode.Multiple)>
Public Class ServiceWebBoggler
    Implements IServiceWebBoggler

    Private Shared WithEvents _RoomMaster As New RoomMaster
    Private Shared WithEvents _Hourglass As New Hourglass

    Friend Shared ServiceWebSocketInstance As ServiceWebSocket
    Friend Shared Event KeepReady()
    Friend Shared Event StartNewRound()
    Friend Shared Event EndRound()
    Friend Shared Event ShowTime()

    Friend Sub New()
        ServiceWebSocket.ServiceWebBogglerInstance = Me
    End Sub

#Region "Methods"
    Public Function GetBoard(ByVal localeID As String) As Board Implements IServiceWebBoggler.GetBoard
        Return _RoomMaster.Board
    End Function

    Public Function IsServerAlive() As Boolean Implements IServiceWebBoggler.IsServerAlive
        Return True
    End Function

    Friend Sub AddPlayer(client As Client)
        _RoomMaster.AddRoundPlayer(client.ClientID, "pippo " + DateTime.Now.ToString(), 0, 0)
    End Sub

#End Region

#Region "Properties"
    Friend Shared ReadOnly Property RoomMaster()
        Get
            Return _RoomMaster
        End Get
    End Property
#End Region

#Region "Event Handlers"

    Private Shared Sub _RoomMaster_NewMatchKeepReady() Handles _RoomMaster.NewMatchKeepReady
        RaiseEvent KeepReady()
    End Sub

    Private Shared Sub _RoomMaster_RoundStart() Handles _RoomMaster.RoundStart
        RaiseEvent StartNewRound()
    End Sub

    Private Shared Sub _RoomMaster_RoundTerminate() Handles _RoomMaster.RoundTerminate
        RaiseEvent EndRound()
    End Sub

    Private Shared Sub _RoomMaster_ValidatedRresults(playersMetadata() As PlayerMetadata) Handles _RoomMaster.ValidatedRresults
        RaiseEvent ShowTime()
    End Sub

#End Region


End Class
