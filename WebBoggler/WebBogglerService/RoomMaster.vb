Imports System.Timers
Imports System.Web.Configuration

Public Class RoomMaster

    Private ReadOnly _rooomPlayers As New Dictionary(Of String, BigBoggler_Common.Player)
    Private _board As BigBoggler_Common.Board
    Private _distributionBoard As Board
    Private _lexicon As New BigBoggler_Common.Lexicon("it-IT")
    Private _solutionWordList As BigBoggler_Common.WordList
    Private Shared _gameSerial As Long

    Public _syncObject As New Object

    Friend Event RoundStart()
    Friend Event ElapsedSecond(secElapsed As Integer)
    Friend Event ScoreChange()
    Friend Event RoundTerminate()
    Friend Event ValidatedRresults()
    Friend Event NewMatchKeepReady()

    Private Const BOARD_RANK = 5
    Private Const MIN_PLAYERS = 0

    Private Const GAME_PRE_START_DELAY = 3000 'Delay prima della partenza del round
#If DEBUG Then
    Private Const GAME_ROUND_DURATION_MS = 180000 '45000
#Else
    Private Const GAME_ROUND_DURATION_MS = 180000
#End If
    Private Const GAME_PRE_VALIDATION_INTERVAL_MS = 3500 'Breve delay prima dell'invio risultati
    Private Const GAME_SHOWTIME_PAUSE_MS = 60000 'periodo di stop per verifica e controllo risultati
    Private Const CYCLE_INTERVAL_MS = 250 'ad ogni ciclo controlla se il numero di giocatori è >= MIN_PLAYERS

    Private WithEvents _hourglassAsyncTimer As New Timer(GAME_ROUND_DURATION_MS) With {.AutoReset = False}
    Private WithEvents _preStartDelayAsyncTimer As New Timer(GAME_PRE_START_DELAY) With {.AutoReset = False}
    Private WithEvents _preValidationAsyncTimer As New Timer(GAME_PRE_VALIDATION_INTERVAL_MS) With {.AutoReset = False}
    Private WithEvents _showTimeAsyncTimer As New Timer(GAME_SHOWTIME_PAUSE_MS) With {.AutoReset = False}
    Private WithEvents _checkPlayersCycleAsyncTimer As New Timer(CYCLE_INTERVAL_MS) With {.AutoReset = True}

    Private Shared _RoundStartTimeUTC As DateTime

    Private Shared _state As RoomMasterState

    Public Enum RoomMasterState
        Ready
        SendingBoard
        KeepReady
        RunningRound
        PauseAfterRound
        ValidatingWordLists
        ShowTime
    End Enum

    Sub New(startingBoardSerial As Integer)

        'System.Diagnostics.Debugger.Launch() '------------------------------------------ DEBUGGER
        _board = New BigBoggler_Common.Board(BOARD_RANK, "it-IT")
        _board.Shake()
        subStoreDistributionBoard()

        _state = RoomMasterState.Ready

        _checkPlayersCycleAsyncTimer.Start()
        _gameSerial = startingBoardSerial
    End Sub

    Friend ReadOnly Property Lexicon As BigBoggler_Common.Lexicon
        Get
            Return _lexicon
        End Get
    End Property

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

    Friend ReadOnly Property RoomPlayers(clientID As String) As Players
        Get
            Dim players As New Players
            players.Items = New List(Of Player)
            For Each ply As BigBoggler_Common.Player In _rooomPlayers.Values
                Dim newPlayer As New Player With {.ID = ply.PeerID, .NickName = ply.Name, .Score = ply.GameScore, .WordList = ConvertWordList(ply.WordList)}
                If newPlayer.ID = clientID Then newPlayer.IsLocal = True
                players.Items.Add(newPlayer)
            Next
            Return players
        End Get

    End Property

    Friend Function AddRoundPlayer(playerID As String, name As String, gameScore As Integer, rank As Long)
        Try
            _rooomPlayers.Add(playerID, New BigBoggler_Common.Player With {.PeerID = playerID, .Name = name, .GameScore = gameScore, .AbsoluteScore = rank})
        Catch ex As Exception

        End Try
        If _rooomPlayers.Keys.Contains(playerID) Then
            Return True
        Else
            Return False
        End If

    End Function

    Friend Sub RemoveRoundPlayer(playerID As String)
        Try
            _rooomPlayers.Remove(playerID)
        Catch ex As Exception

        End Try
    End Sub

    Friend ReadOnly Property RoundDurationMS() As Integer
        Get
            Return GAME_ROUND_DURATION_MS
        End Get
    End Property
    Friend ReadOnly Property DeadTimeAmountMS() As Integer
        Get
            Return GAME_PRE_VALIDATION_INTERVAL_MS + GAME_SHOWTIME_PAUSE_MS + GAME_PRE_START_DELAY
        End Get
    End Property

    Friend ReadOnly Property RoundStartTimeUTC As DateTime
        Get
            Return _RoundStartTimeUTC
        End Get
    End Property

    Friend Function GetSolution() As WebBogglerService.WordList
        Return ConvertWordList(_solutionWordList)
    End Function

    Private Sub CheckPlayers()
        'Se il la room è in attesa di play, controllo se c'è il minimo di player
        'e se sì creo la board e la memorizzo come data contract Board da scaricare
        If _state = RoomMasterState.Ready Then
            If _rooomPlayers.Count >= MIN_PLAYERS Then
                _state = RoomMasterState.SendingBoard
                If _board Is Nothing Then

                    '//Selezione lingua
                    Dim language As String = "Italiano"

                    If language = "Italiano" Then
                        _board = New BigBoggler_Common.Board(BOARD_RANK, "it-IT")
                    Else
                        _board = New BigBoggler_Common.Board(BOARD_RANK, "en-EN")
                    End If
                End If
                _board.Shake()
                subStoreDistributionBoard()

                _state = RoomMasterState.KeepReady
                RaiseEvent NewMatchKeepReady()
                _preStartDelayAsyncTimer.Start()

            End If
        Else
            'Se tutti i player hanno abbandonato fermo il gioco
            If _hourglassAsyncTimer.Enabled And (_rooomPlayers.Count < MIN_PLAYERS) Then
                _hourglassAsyncTimer.Stop()
                EndRound()
            End If

        End If

    End Sub

    Private Sub subStoreDistributionBoard()

        Dim wbBoard As New Board()
        wbBoard.LocaleID = _board.LocaleID
        Dim dices As New WebBogglerService.Dices
        For i As Integer = 0 To 4
            For j As Integer = 0 To 4
                Dim diceItem As BigBoggler_Common.Board.Dices.Dice = _board.DiceArray(i, j)
                Dim dice As New WebBogglerService.Dice
                dice.Letter = diceItem.SelectedString
                dice.Rotation = diceItem.FaceRotation
                dice.Index = i * 5 + j
                dices.Add(dice)
            Next
        Next
        wbBoard.DicesVector = dices
        _solutionWordList = _board.Solve(_lexicon)
        wbBoard.WordCount = _solutionWordList.Count
        _gameSerial += 1

        wbBoard.GameSerial = _gameSerial
        _distributionBoard = wbBoard


        ''Scrivo il seriale della board in web.config
        Dim myConfiguration As Configuration = WebConfigurationManager.OpenWebConfiguration("~")
        myConfiguration.AppSettings.Settings("BoardsServedCount").Value = _gameSerial.ToString
        myConfiguration.Save()
        ''

    End Sub
    Private Sub EndRound()
        _state = RoomMasterState.PauseAfterRound

        RaiseEvent RoundTerminate()
        _preValidationAsyncTimer.Start()  'dopo tre secondi fa partire il calcolo e l'invio dei risultati ai peer

    End Sub

    Friend Sub AddWordList(wordlist As WordList, playerID As Integer) ', peer As BigBogglerPeer)
        _rooomPlayers(playerID).WordList = ConvertWordList(wordlist)
    End Sub


    Private Sub subValidateAndSendResults()
        _state = RoomMasterState.ValidatingWordLists

        MarkDuplicatedWords()
        UpdateScores()

        RaiseEvent ValidatedRresults() 'inviare i risultati che verranno valutati durante l'attesa di x sec

        _state = RoomMasterState.ShowTime
        _showTimeAsyncTimer.Start()
    End Sub

    Private Overloads Function ConvertWordList(wordList As WordList) As BigBoggler_Common.WordList
        Dim wl As New BigBoggler_Common.WordList
        For Each w As Word In wordList.Items
            Dim word As New BigBoggler_Common.WordBase()
            For Each d As Dice In w.DicePath
                word.AppendDiceLast(_board.DiceArray(d.Index \ 5, d.Index Mod 5))
            Next
            wl.Add(word.Text, word)
        Next
        Return wl
    End Function

    Private Overloads Function ConvertWordList(wordList As BigBoggler_Common.WordList) As WordList
        Dim wl As New WordList
        wl.Items = New List(Of Word)
        For Each w As BigBoggler_Common.WordBase In wordList.Values
            Dim word As New Word()
            word.DicePath = New List(Of Dice)
            For Each d As BigBoggler_Common.Board.Dices.Dice In w.DicePath
                Dim dice As New Dice With {.Index = d.Row * 5 + d.Column, .Letter = d.SelectedString}
                word.DicePath.Add(dice)
            Next
            wl.Items.Add(word)
        Next
        Return wl
    End Function

    Private Sub MarkDuplicatedWords()
        For Each player1 As KeyValuePair(Of String, BigBoggler_Common.Player) In _rooomPlayers
            For Each player2 As KeyValuePair(Of String, BigBoggler_Common.Player) In _rooomPlayers
                If player2.Value IsNot player1.Value Then
                    For Each w2 As KeyValuePair(Of String, BigBoggler_Common.WordBase) In player2.Value.WordList

                        ''Per un confronto fra dadi:
                        'Dim found As Boolean = player1.WordList.Values.Any(Function(fw) w2 = fw)

                        'Per un confronto fra parole:
                        Dim found As Boolean = player1.Value.WordList.Values.Any(Function(fw) w2.Value.Text = fw.Text)

                        w2.Value.Duplicated = found
                    Next
                End If
            Next
        Next

    End Sub

    Private Sub UpdateScores()

        For p As Integer = 0 To _rooomPlayers.Count - 1
            Dim player As KeyValuePair(Of String, BigBoggler_Common.Player) = _rooomPlayers.ElementAt(p)
            For i = 0 To player.Value.WordList.Count - 1
                Try
                    If player.Value.WordList.Values(i).Duplicated Then
                        If player.Value.WordList.Values(i).Duplicated Then player.Value.WordList.Remove(player.Value.WordList.Keys(i))
                    End If
                Catch ex As Exception

                End Try

            Next
            Dim score As Integer = player.Value.WordList.GetTotalScore(False)
            player.Value.GameScore += score
            'player.Value.AbsoluteScore += score
        Next

        RaiseEvent ScoreChange()

    End Sub

    Friend Sub SetPlayerReadyState(clientID As String, ready As Boolean)
        _rooomPlayers(clientID).IsReady = ready
    End Sub

    Friend Sub TryStartNewRoundNow()
        'controlla se tutti i player hanno inviato lo stato di ready e in tal caso inizia in anticipo il nuovo round
        SyncLock _syncObject
            Dim everyBodyIsReady As Boolean = True
            For Each ply As BigBoggler_Common.Player In _rooomPlayers.Values
                everyBodyIsReady = (everyBodyIsReady And ply.IsReady)
            Next
            'If ready Then _StartNewRoundTask.Schedule(Sub() _state = RoomMasterState.Busy, 10)
            If everyBodyIsReady Then _state = RoomMasterState.Ready
        End SyncLock
    End Sub


#Region "Event Handlers"


    Private Sub _preStartDelayAsyncTimer_Elapsed(sender As Object, e As ElapsedEventArgs) Handles _preStartDelayAsyncTimer.Elapsed
        _state = RoomMasterState.RunningRound

        RaiseEvent RoundStart()

        For Each ply As BigBoggler_Common.Player In _rooomPlayers.Values
            ply.IsReady = False
        Next

        _hourglassAsyncTimer.Start()
        _RoundStartTimeUTC = Now.ToUniversalTime

    End Sub

    Private Sub _hourglassAsyncTimer_Elapsed(sender As Object, e As ElapsedEventArgs) Handles _hourglassAsyncTimer.Elapsed
        EndRound()
    End Sub

    Private Sub _preValidationAsyncTimer_Elapsed(sender As Object, e As ElapsedEventArgs) Handles _preValidationAsyncTimer.Elapsed

        subValidateAndSendResults()
    End Sub

    Private Sub _showTimeAsyncTimer_Elapsed(sender As Object, e As ElapsedEventArgs) Handles _showTimeAsyncTimer.Elapsed
        If _state = RoomMasterState.ShowTime Then
            _state = RoomMasterState.Ready
        End If

    End Sub

    Private Sub _checkPlayersCycleAsyncTimer_Elapsed(sender As Object, e As ElapsedEventArgs) Handles _checkPlayersCycleAsyncTimer.Elapsed
        CheckPlayers()
    End Sub

#End Region
End Class
