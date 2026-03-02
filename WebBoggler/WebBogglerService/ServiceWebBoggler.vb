
Imports System.Threading.Tasks
Imports System.Text
Imports System.ServiceModel.Channels
Imports System.Web.Configuration
Imports System.Net.WebSockets
Imports BigBoggler_Common

<CORSSupportBehavior>
<ServiceBehavior(InstanceContextMode:=InstanceContextMode.PerSession, ConcurrencyMode:=ConcurrencyMode.Multiple)>
Public Class ServiceWebBoggler
    Implements IServiceWebBoggler

    Private Shared WithEvents _RoomMaster As RoomMaster

    Friend Shared ServiceWebSocketInstance As ServiceWebSocket

    Friend Shared Event KeepReady()
    Friend Shared Event StartNewRound()
    Friend Shared Event EndRound()
    Friend Shared Event ShowTime()
    Friend Shared Event NewPlayer()
    Friend Shared Event RemovedPlayer()

    Friend Sub New()
        ServiceWebSocket.ServiceWebBogglerInstance = Me
        If Not WebConfigurationManager.AppSettings.AllKeys.Contains("BoardsServedCount") Then
            ''Scrivo il seriale della board in web.config
            Dim myConfiguration As Configuration = WebConfigurationManager.OpenWebConfiguration("~")
            myConfiguration.AppSettings.Settings.Add("BoardsServedCount", "0")
            myConfiguration.Save()
            ''
        End If

        Dim startingBoardSerial As Integer

        startingBoardSerial = Integer.Parse(WebConfigurationManager.AppSettings("BoardsServedCount"))
        _RoomMaster = New RoomMaster(startingBoardSerial)
    End Sub
    '''Public Class GameInfo
    '''    Public Property RoomState As String
    '''    Public Property RoundElapsedTimeMS As Double
    '''    Public Property RoundDurationMS As Integer
    '''    Public Property DeadTimeAmountMS As Integer
    '''    Public Property ServerTimeUTC As String
    '''End Class

#Region "Methods"

    Public Function IsServerAlive() As Boolean Implements IServiceWebBoggler.IsServerAlive
        Return True
    End Function

    Public Function GetBoard(ByVal localeID As String) As Board Implements IServiceWebBoggler.GetBoard
        Return _RoomMaster.Board
    End Function

    Public Function CheckWord(word As String) As Boolean Implements IServiceWebBoggler.CheckWord
        Return _RoomMaster.Lexicon.Validate(word)
    End Function

    Public Sub SendWordList(wordList As WordList, clientID As String) Implements IServiceWebBoggler.SendWordList
        _RoomMaster.AddWordList(wordList, clientID)
    End Sub

    Public Function Observe() As GameInfo Implements IServiceWebBoggler.Observe
        Dim info As New GameInfo
        info.RoomState = _RoomMaster.State.ToString()
        info.RoundElapsedTimeMS = (Now.ToUniversalTime() - _RoomMaster.RoundStartTimeUTC).TotalMilliseconds
        info.RoundDurationMS = _RoomMaster.RoundDurationMS
        info.DeadTimeAmountMS = _RoomMaster.DeadTimeAmountMS
        info.ServerTimeUTC = (Now.ToUniversalTime).ToString
        Return info
    End Function

    Public Function Join(clientID As String, userName As String) As Boolean Implements IServiceWebBoggler.Join
        If clientID.Length > 0 AndAlso clientID <> "-1" Then
            Dim result As Boolean
            Try
                result = _RoomMaster.AddRoundPlayer(clientID, userName, 0, 0)
            Catch ex As Exception
            Finally
            End Try

            If result Then
                RaiseEvent NewPlayer()
                Return True
            Else
                Return False
            End If
        Else
            Return False
        End If
    End Function

    Public Function Leave(clientID As String) As Boolean Implements IServiceWebBoggler.Leave

        If clientID.Length > 0 AndAlso clientID <> "-1" Then
            RemovePlayer(clientID)

            Return True '[@@] Effettuare verifica
        Else
            Return False
        End If
    End Function

    Public Function GetPlayers(clientID As String) As Players Implements IServiceWebBoggler.GetPlayers
        PurgePlayers()
        Return _RoomMaster.RoomPlayers(clientID)

    End Function

    Public Function GetSolution() As WordList Implements IServiceWebBoggler.GetSolution 'Restituisce solo parole >= 6 lettere
        Dim wl As New WordList
        wl.Items = _RoomMaster.GetSolution.Items.Where(Function(w As Word) w.DicePath.Count >= 7).ToList
        Return wl
    End Function

#End Region

#Region "Sub/Functions"

    Friend Function RemovePlayer(clientID As String) As Boolean
        Try
            _RoomMaster.RemoveRoundPlayer(clientID)
        Catch ex As Exception
            Return False
        Finally
        End Try

        RaiseEvent RemovedPlayer()
        Return True
    End Function

    Friend Sub SetPlayerReadyState(clientID As String, ready As Boolean)
        _RoomMaster.SetPlayerReadyState(clientID, ready)
        _RoomMaster.TryStartNewRoundNow() 'tenta lo start di un nuovo round che partirà solo se tutti i player sono ready
    End Sub

    Friend Sub PurgePlayers()
        Dim playerToRemove As New List(Of Player)
        For Each player As Player In _RoomMaster.RoomPlayers("").Items
            If Not ServiceWebSocketInstance.Clients.Contains(player.ID) Or player.ID = "-1" Then
                playerToRemove.Add(player)
            End If
        Next
        For Each player As Player In playerToRemove
            _RoomMaster.RoomPlayers("").Items.Remove(player)
        Next
    End Sub

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

    Private Shared Sub _RoomMaster_ValidatedRresults() Handles _RoomMaster.ValidatedRresults
        RaiseEvent ShowTime()
    End Sub

#End Region


End Class
