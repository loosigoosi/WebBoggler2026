
Public Class RoomMaster
    Private WithEvents _hourglass As Hourglass
    Private WithEvents _preStartDelay As Hourglass
    Private WithEvents _preValidationTimer As Hourglass
    Private WithEvents _postRoundTimer As Hourglass
    Private WithEvents _checkPlayersCycle As Hourglass
    Private _board As BigBoggler_Common.Board
    Private _distributionBoard As Board
    Private ReadOnly _roundPlayers As New Dictionary(Of Integer, BigBoggler_Common.Player)
    Friend Event RoundStart()
    Friend Event ElapsedSecond(secElapsed As Integer)
    Friend Event ScoreChange()
    Friend Event RoundTerminate()
    Friend Event ValidatedRresults(playersMetadata() As BigBoggler_Common.PlayerMetadata)
    Friend Event NewMatchKeepReady()
    Public Shared _syncObject As New Object

    Private Const BOARD_RANK = 5
    Private Const MIN_PLAYERS = 0
    Private Const GAME_PRE_START_DELAY = 5000 'Delay prima della partenza del round
#If DEBUG Then
    Private Const GAME_ROUND_DURATION_MIN = 0
    Private Const GAME_ROUND_DURATION_SEC = 25
#Else
    Private Const GAME_ROUND_DURATION_MIN = 3
    Private Const GAME_ROUND_DURATION_SEC = 1
#End If

    Private Const GAME_PRE_VALIDATION_INTERVAL = 3500 'Breve delay prima dell'invio risultati
    Private Const GAME_POST_ROUND_PAUSE_ = 10000 'periodo di stop per verifica e controllo risultati
    Private Const CYCLE_INTERVAL = 2000 'ad ogni ciclo controlla se il numero di giocatori è >= MIN_PLAYERS

    Friend Enum RoomMasterState
        Busy
        StartingRound
        RunningRound
        ShowTime
    End Enum

    Private _state As RoomMasterState


    Public Sub New() 'Optional pluginHost As IPluginHost)

        'System.Diagnostics.Debugger.Launch() '------------------------------------------ DEBUGGER

        _board = New BigBoggler_Common.Board(BOARD_RANK, "it-IT")
        _board.Shake()
        subStoreDistributionBoard()


        _state = RoomMasterState.Busy

        _hourglass = New Hourglass
        With _hourglass
            .Duration = New TimeSpan(0, GAME_ROUND_DURATION_MIN, GAME_ROUND_DURATION_SEC)
            .Perpetual = False
            .Reset()
        End With

        _preStartDelay = New Hourglass
        With _preStartDelay
            .Duration = New TimeSpan(0, 0, 0, 0, GAME_PRE_START_DELAY)
            .Perpetual = False
            .Reset()
        End With

        _preValidationTimer = New Hourglass
        With _preValidationTimer
            .Duration = New TimeSpan(0, 0, 0, 0, GAME_PRE_VALIDATION_INTERVAL)
            .Perpetual = False
            .Reset()
        End With

        _postRoundTimer = New Hourglass
        With _postRoundTimer
            .Duration = New TimeSpan(0, 0, 0, 0, GAME_POST_ROUND_PAUSE_)
            .Perpetual = False
            .Reset()
        End With

        _checkPlayersCycle = New Hourglass
        With _checkPlayersCycle
            .Duration = New TimeSpan(0, 0, 0, 0, CYCLE_INTERVAL)
            .Perpetual = True
            .Run()
        End With

    End Sub

    Friend ReadOnly Property Board As Board
        Get
            Return _distributionBoard
        End Get
    End Property

    Friend ReadOnly Property State As RoomMasterState
        Get
            Return _state
        End Get
    End Property

    Public Sub TryStartNewRoundNow(actorNr As Integer)
        SyncLock _syncObject
            Dim ready As Boolean = True
            _roundPlayers(actorNr).IsReady = True

            For Each ply In _roundPlayers.Values
                ready = (ready And ply.IsReady)
            Next
            'If ready Then _StartNewRoundTask.Schedule(Sub() _state = RoomMasterState.Busy, 10)
            If ready Then _state = RoomMasterState.Busy
        End SyncLock
    End Sub

    Public Sub AddRoundPlayer(PlayerID As Integer, name As String, gameScore As Integer, absolutescore As Long)
        Try
            _roundPlayers.Add(PlayerID, New BigBoggler_Common.Player With {.PeerID = PlayerID.ToString, .Name = name, .GameScore = gameScore, .AbsoluteScore = absolutescore})
        Catch ex As Exception

        End Try
    End Sub

    Public Sub RemoveRoundPlayer(PlayerID As Integer)
        Try
            _roundPlayers.Remove(PlayerID)
        Catch ex As Exception

        End Try
    End Sub

    Private Sub CheckPlayers()
        If _state = RoomMasterState.Busy Then
            If _roundPlayers.Count >= MIN_PLAYERS Then
                _state = RoomMasterState.StartingRound
                If _board Is Nothing Then

                    '//Dim language As String = _pluginHost.GameProperties("la")
                    Dim language As String = "Italiano"

                    If language = "Italiano" Then
                        _board = New BigBoggler_Common.Board(BOARD_RANK, "it-IT")
                    Else
                        _board = New BigBoggler_Common.Board(BOARD_RANK, "en-EN")
                    End If
                End If
                _board.Shake()
                subStoreDistributionBoard()
                RaiseEvent NewMatchKeepReady()

                _preStartDelay.Run()
            End If
        Else
            If _hourglass.IsRunning And _roundPlayers.Count < MIN_PLAYERS Then
                _hourglass.Reset()
                EndRound()
            End If

        End If

    End Sub

    Public Sub EndRound()
        '_RoomCheckFiber.Start()

        RaiseEvent RoundTerminate()
        _preValidationTimer.Run() 'dopo tre secondi fa partire il calcolo e l'invio dei risultati ai peer

    End Sub

    Public Sub AddWordList(metadata As BigBoggler_Common.WordListMetadata, actorNumber As Integer) ', peer As BigBogglerPeer)
        Dim wl As New BigBoggler_Common.WordList
        wl.SetMetadata(_board, metadata)
        _roundPlayers(actorNumber).WordList = wl
    End Sub


    Private Sub subValidateAndSendResults()
        _state = RoomMasterState.ShowTime
        MarkDuplicatedWords()
        UpdateScores()

        Dim playermeta(_roundPlayers.Count - 1) As BigBoggler_Common.PlayerMetadata

        Dim i As Integer = 0
        For Each player In _roundPlayers

            playermeta(i) = player.Value.GetMetadata
            i += 1
        Next

        RaiseEvent ValidatedRresults(playermeta) 'inviare i risultati che verranno valutati durante l'attesa di x sec
        '_StartNewRoundTask = _roomMasterFiber.Schedule((Sub()
        '                                                    If _state = RoomMasterState.ShowTime Then
        '                                                        _state = RoomMasterState.Busy
        '                                                    End If
        '                                                End Sub), GAME_POST_ROUND_PAUSE_)

        _postRoundTimer.Run()
    End Sub

    Friend Sub MarkDuplicatedWords()
        For Each player1 As BigBoggler_Common.Player In _roundPlayers.Values
            For Each player2 As BigBoggler_Common.Player In _roundPlayers.Values
                If player2 IsNot player1 Then
                    For Each w2 As BigBoggler_Common.WordBase In player2.WordList.Values

                        ''Per un confronto fra dadi:
                        'Dim found As Boolean = player1.WordList.Values.Any(Function(fw) w2 = fw)

                        'Per un confronto fra parole:
                        Dim found As Boolean = player1.WordList.Values.Any(Function(fw) w2.Text = fw.Text)

                        w2.Duplicated = found
                    Next
                End If
            Next
        Next

    End Sub

    Friend Sub UpdateScores()

        For p As Integer = 0 To _roundPlayers.Count - 1
            Dim player As BigBoggler_Common.Player = _roundPlayers.Values(p)
            For i = 0 To player.WordList.Count - 1
                Try
                    If player.WordList.Values(i).Duplicated Then
                        If player.WordList.Values(i).Duplicated Then player.WordList.Remove(player.WordList.Keys(i))
                    End If
                Catch ex As Exception

                End Try

            Next
            Dim score As Integer = player.WordList.GetTotalScore(False)
            player.GameScore = score
            player.AbsoluteScore += score
        Next

        RaiseEvent ScoreChange()

    End Sub

    Private Sub subStoreDistributionBoard()
        Dim wbBoard As New WebBogglerServer.Board()
        wbBoard.LocaleID = _board.LocaleID
        Dim dices As New WebBogglerServer.Dices
        For i As Integer = 0 To 4
            For j As Integer = 0 To 4
                Dim diceItem As BigBoggler_Common.Board.Dices.Dice = _board.DiceArray(i, j)
                Dim dice As New WebBogglerServer.Dice
                dice.Letter = diceItem.SelectedString
                dice.Rotation = diceItem.FaceRotation
                dice.Index = i * 5 + j
                dices.Add(dice)
            Next
        Next
        wbBoard.DicesVector = dices
        _distributionBoard = wbBoard

    End Sub


#Region "Event Handlers"


    Private Sub _Hourglass_TimeExpired(sender As Object, e As Hourglass.HourglassTickEventArgs) Handles _hourglass.TimeExpired
        EndRound()
    End Sub

    Private Sub _hourglass_ElapsedSecond(sender As Object, e As Hourglass.HourglassTickEventArgs) Handles _hourglass.ElapsedSecond

        RaiseEvent ElapsedSecond(_hourglass.ElapsedTime.TotalSeconds)
    End Sub

    Private Sub _preStartDelay_TimeExpired(sender As Object, e As EventArgs) Handles _preStartDelay.TimeExpired
        _state = RoomMasterState.RunningRound

        RaiseEvent RoundStart()

        For Each ply In _roundPlayers.Values
            ply.IsReady = False
        Next
        '-------------------------------------x minuti + x secondi per tempi morti
        _hourglass.Reset()
        _hourglass.Run()
    End Sub

    Private Sub _preValidationTimer_TimeExpired(sender As Object, e As EventArgs) Handles _preValidationTimer.TimeExpired
        subValidateAndSendResults()
    End Sub

    Private Sub _postRoundTimer_TimeExpired(sender As Object, e As EventArgs) Handles _postRoundTimer.TimeExpired
        If _state = RoomMasterState.ShowTime Then
            _state = RoomMasterState.Busy
        End If
    End Sub

    Private Sub _checkPlayersCycle_ElapsedSecond(sender As Object, e As EventArgs) Handles _checkPlayersCycle.ElapsedSecond
        CheckPlayers()
    End Sub

#End Region
End Class
