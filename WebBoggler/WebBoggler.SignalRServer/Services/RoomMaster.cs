using BigBoggler_Common; // TEMPORANEO: Solo per Board.Shake() e Lexicon
using BigBoggler.Models;  // NUOVO: Modelli unificati
using BigBoggler.Timing.Server; // NUOVO: ServerHourglass
using BigBoggler.Lexicon; // NUOVO: Lexicon service
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Timers;
using static System.Formats.Asn1.AsnWriter;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;
using DtoModels = WebBoggler.SignalRServer.Models; // Alias per i DTO
using Timer = System.Timers.Timer;
#nullable disable

namespace WebBoggler.SignalRServer.Services;

public class RoomMaster
{
    // Usa BigBoggler.Models invece dei DTO locali
    private readonly ConcurrentDictionary<string, DtoModels.Player> _roomPlayers = new();
    
    // TEMPORANEO: usa ancora BigBoggler_Common.Board per Shake()
    private BigBoggler_Common.Board? _board;
    
    // Per distribuzione: convertiamo in BigBoggler.Models.Board
    private DtoModels.Board? _distributionBoard;
    
    private static long _gameSerial = 0;

    private readonly object _syncObject = new();
    private readonly IHubContext<GameHub> _hubContext;

    // Eventi
    public event Func<Task>? RoundStart;
    public event Func<Task>? RoundTerminate;
    public event Func<Task>? ValidatedResults;
    public event Func<Task>? NewMatchKeepReady;
    public event Func<Task>? ScoreChange;
    public event Func<Task>? BoardDiscarded;

    private const int BOARD_RANK = 5;
    private const int MIN_PLAYERS = 1;
    private const int GAME_PRE_START_DELAY = 3000;
    #if DEBUG
    private const int GAME_ROUND_DURATION_MS = 60000;
    #else
    private const int GAME_ROUND_DURATION_MS = 180000;
    #endif
    private const int GAME_PRE_VALIDATION_INTERVAL_MS = 3500;
    private const int GAME_SHOWTIME_PAUSE_MS = 60000;
    private const int CYCLE_INTERVAL_MS = 250;
    private const int DISCARD_ALLOWED_TIME_MS = 15000;

    private readonly ServerHourglass _roundTimer;
    private readonly Timer _preStartDelayTimer;
    private readonly Timer _preValidationTimer;
    private readonly Timer _showTimeTimer;
    private readonly Timer _checkPlayersCycleTimer;
    private readonly Timer _discardAllowedTimer;

    private static DateTime _roundStartTimeUTC;
    private RoomMasterState _state = RoomMasterState.Ready;
    private BigBoggler_Common.Lexicon _lexicon = new BigBoggler_Common.Lexicon("it-IT");
    private bool _discardAllowed = false;
    public enum RoomMasterState
    {
        Ready,
        SendingBoard,
        KeepReady,
        RunningRound,
        PauseAfterRound,
        ValidatingWordLists,
        ShowTime,
        //TO ADD\\ RoundDurationMs
    }

    public RoomMaster(IHubContext<GameHub> hubContext, long startingBoardSerial = 0)
    {
        _hubContext = hubContext;
        _gameSerial = startingBoardSerial;

        // Crea il primo board usando BigBoggler_Common con Shake()
        _board = new BigBoggler_Common.Board(BOARD_RANK, "it-IT");
        _board.Shake();
        StoreDistributionBoard();

        // NUOVO: ServerHourglass al posto di _hourglassTimer
        _roundTimer = new ServerHourglass
        {
            Duration = TimeSpan.FromMilliseconds(GAME_ROUND_DURATION_MS)
        };
        _roundTimer.OnExpiredAsync(async () => 
        {
            Console.WriteLine("[RoomMaster] Round timer expired - Calling EndRound()");
            EndRound();
            await Task.CompletedTask;
        });

        // Altri timer rimangono Timer standard
        _preStartDelayTimer = new Timer(GAME_PRE_START_DELAY) { AutoReset = false };
        _preStartDelayTimer.Elapsed += PreStartDelayTimer_Elapsed;

        _preValidationTimer = new Timer(GAME_PRE_VALIDATION_INTERVAL_MS) { AutoReset = false };
        _preValidationTimer.Elapsed += PreValidationTimer_Elapsed;

        _showTimeTimer = new Timer(GAME_SHOWTIME_PAUSE_MS) { AutoReset = false };
        _showTimeTimer.Elapsed += ShowTimeTimer_Elapsed;

        _checkPlayersCycleTimer = new Timer(CYCLE_INTERVAL_MS) { AutoReset = true };
        _checkPlayersCycleTimer.Elapsed += CheckPlayersCycleTimer_Elapsed;
        _checkPlayersCycleTimer.Start();

        _discardAllowedTimer = new Timer(DISCARD_ALLOWED_TIME_MS) { AutoReset = false };
        _discardAllowedTimer.Elapsed += DiscardAllowedTimer_Elapsed;

        _state = RoomMasterState.Ready;
    }

    public RoomMasterState State => _state;
    public DtoModels.Board? Board => _distributionBoard;
    public int RoundDurationMS => GAME_ROUND_DURATION_MS;
    public int DeadTimeAmountMS => GAME_PRE_VALIDATION_INTERVAL_MS + GAME_SHOWTIME_PAUSE_MS + GAME_PRE_START_DELAY;
    public DateTime RoundStartTimeUTC => _roundStartTimeUTC;
    public bool DiscardAllowed => _discardAllowed;
    public DtoModels.Players GetRoomPlayers(string clientID)
    {
        var playersList = new List<DtoModels.Player>();

        foreach (var ply in _roomPlayers.Values)
        {
            string displayName = ply.NickName ?? "";

            // Aggiungi Spunta se IsReady
            if (ply.IsReady)
            {
                displayName = "✓ " + displayName;
            }
            else
            {
                displayName = ". " + displayName;
            }

            // Aggiungi parentesi quadre per il giocatore locale
            if (ply.ID == clientID)
            {
                displayName = "[" + displayName + "]";
            }

            var newPlayer = new DtoModels.Player
            {
                ID = ply.ID,
                NickName = displayName,
                Score = ply.Score,
                WordList = ply.WordList,
                IsLocal = ply.ID == clientID,
                IsReady = ply.IsReady
            };
            playersList.Add(newPlayer);
        }

        // Ordina: player locale in cima, poi gli altri
        var sortedList = playersList.OrderByDescending(p => p.IsLocal).ToList();
        return new DtoModels.Players { Items = sortedList.ToArray() };
    }

    public bool AddRoundPlayer(string playerID, string name, int gameScore = 0, long rank = 0)
    {
        var player = new DtoModels.Player
        {
            ID = playerID,
            NickName = name,
            Score = gameScore,
            IsReady = false,
            WantsDiscard = false,
            WordList = new DtoModels.WordList { Items = Array.Empty<DtoModels.Word>() }
        };

        return _roomPlayers.TryAdd(playerID, player);
    }

    public void OnPlayerJoined()
    {
        // Quando un secondo giocatore si unisce, resetta tutto per un nuovo inizio equo
        if (_roomPlayers.Count == 2)
        {
            Console.WriteLine("[RoomMaster.OnPlayerJoined] Second player joined - Resetting game state");

            // Ferma tutti i timer
            _roundTimer.Reset();
            _preStartDelayTimer.Stop();
            _preValidationTimer.Stop();
            _showTimeTimer.Stop();
            _discardAllowedTimer.Stop();
            _discardAllowed = false;

            // Reset stato ready e punteggi per tutti i giocatori
            foreach (var player in _roomPlayers.Values)
            {
                player.IsReady = false;
                player.Score = 0; // Reset punteggio - tutti partono da zero
                player.WantsDiscard = false;
            }

            // Genera nuova board (il seriale _gameSerial continua a incrementare, NON si resetta)
            _board = new BigBoggler_Common.Board(BOARD_RANK, "it-IT");
            _board.Shake();
            StoreDistributionBoard(); // Incrementa _gameSerial

            // Torna allo stato Ready
            _state = RoomMasterState.Ready;

            Console.WriteLine("[RoomMaster.OnPlayerJoined] Game reset completed - new board generated");
        }
    }

    public void RemoveRoundPlayer(string playerID)
    {
        _roomPlayers.TryRemove(playerID, out _);
    }

    public DtoModels.WordList? GetSolution()
    {
        try
        {
            var bigBogSolution = _board?.Solve(_lexicon);
            if (bigBogSolution == null || bigBogSolution.Count == 0)
            {
                return new DtoModels.WordList { Items = Array.Empty<DtoModels.Word>() };
            }

            return ConvertBigBogWordListToDto(bigBogSolution);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetSolution: {ex.Message}");
            return new DtoModels.WordList { Items = Array.Empty<DtoModels.Word>() };
        }
    }

    public bool CheckWord(string wordString)
    {
        // Per ora accetta tutte le parole - implementeremo dopo
        // return true;

        return _lexicon.Validate(wordString); // Non è che ci fosse granché da pensare, badòla!
         
    }

//    WORDLIST CONVERSION TO DO old VB version.\\
//    Private Overloads Function ConvertWordList(wordList As WordList) As BigBoggler_Common.WordList
//        Dim wl As New BigBoggler_Common.WordList
//        For Each w As Word In wordList.Items
//            Dim word As New BigBoggler_Common.WordBase()
//            For Each d As Dice In w.DicePath
//                word.AppendDiceLast(_board.DiceArray(d.Index \ 5, d.Index Mod 5))
//            Next
//            wl.Add(word.Text, word)
//        Next
//        Return wl
//    End Function

//    Private Overloads Function ConvertWordList(wordList As BigBoggler_Common.WordList) As WordList
//        Dim wl As New WordList
//        wl.Items = New List(Of Word)
//        For Each w As BigBoggler_Common.WordBase In wordList.Values
//            Dim word As New Word()
//            word.DicePath = New List(Of Dice)
//            For Each d As BigBoggler_Common.Board.Dices.Dice In w.DicePath
//                Dim dice As New Dice With {.Index = d.Row * 5 + d.Column, .Letter = d.SelectedString}
//    word.DicePath.Add(dice)
//Next

//wl.Items.Add(word)
//        Next
//        Return wl
//    End Function


    // Converte da BigBoggler.WordList a DTO WordList (per GetSolution)
    private DtoModels.WordList ConvertBigBogWordListToDto(BigBoggler_Common.WordList bigBogWordList)
    {
        var dtoWords = new List<DtoModels.Word>();

        foreach (var wordEntry in bigBogWordList.Values)
        {
            var dtoWord = new DtoModels.Word
            {
                DicePath = wordEntry.DicePath.Select(d => new DtoModels.Dice
                {
                    Index = d.Row * 5 + d.Column,
                    Letter = d.SelectedString,
                    Row = d.Row,
                    Column = d.Column,
                    Rotation = d.FaceRotation
                }).ToList()
            };
            dtoWords.Add(dtoWord);
        }

        return new DtoModels.WordList { Items = dtoWords.ToArray() };
    }

    // Converte da DTO WordList a BigBoggler.WordList (per validazione WordList giocatori)
    private BigBoggler_Common.WordList ConvertDtoWordListToBigBog(DtoModels.WordList dtoWordList)
    {
        var bigBogWordList = new BigBoggler_Common.WordList();

        if (dtoWordList?.Items == null || _board == null)
            return bigBogWordList;

        foreach (var dtoWord in dtoWordList.Items)
        {
            if (dtoWord?.DicePath == null || dtoWord.DicePath.Count == 0)
                continue;

            var bigBogWord = new BigBoggler_Common.WordBase();

            foreach (var dtoDice in dtoWord.DicePath)
            {
                // Recupera il dado vero dal board usando l'indice
                var row = dtoDice.Index / 5;
                var col = dtoDice.Index % 5;
                var boardDice = _board.DiceArray[row, col];
                bigBogWord.AppendDiceLast(boardDice);
            }

            // Aggiunge alla WordList usando il testo come chiave
            if (!string.IsNullOrEmpty(bigBogWord.Text))
            {
                bigBogWordList.Add(bigBogWord.Text, bigBogWord);
            }
        }

        return bigBogWordList;
    }


//    WORDLIST CONVERSION TO DO old VB version.\\
    //Private Sub MarkDuplicatedWords()
    //For Each player1 As KeyValuePair(Of String, BigBoggler_Common.Player) In _rooomPlayers
    //        For Each player2 As KeyValuePair(Of String, BigBoggler_Common.Player) In _rooomPlayers
    //            If player2.Value IsNot player1.Value Then
    //                For Each w2 As KeyValuePair(Of String, BigBoggler_Common.WordBase) In player2.Value.WordList

    //                    ''Per un confronto fra dadi:
    //                    'Dim found As Boolean = player1.WordList.Values.Any(Function(fw) w2 = fw)

    //                    'Per un confronto fra parole:
    //                    Dim found As Boolean = player1.Value.WordList.Values.Any(Function(fw) w2.Value.Text = fw.Text)

    //                    w2.Value.Duplicated = found
    //                Next
    //            End If
    //        Next
    //    Next




    public void AddWordList(DtoModels.WordList wordList, string playerID)
    {
        if (_roomPlayers.TryGetValue(playerID, out var player))
        {
            player.WordList = wordList;
        }
    }

    public void SetPlayerReadyState(string clientID, bool ready)
    {
        Console.WriteLine($"[RoomMaster.SetPlayerReadyState] ClientID: {clientID}, Ready: {ready}");

        if (_roomPlayers.TryGetValue(clientID, out var player))
        {
            player.IsReady = ready;
            Console.WriteLine($"[RoomMaster.SetPlayerReadyState] Player {player.NickName ?? "Unknown"} set to IsReady={ready}");
        }
        else
        {
            Console.WriteLine($"[RoomMaster.SetPlayerReadyState] ERROR: ClientID {clientID} not found in _roomPlayers!");
            Console.WriteLine($"[RoomMaster.SetPlayerReadyState] Available players: {string.Join(", ", _roomPlayers.Keys)}");
        }
    }

    public void SetPlayerDiscardState(string clientID, bool wantsDiscard)
    {
        Console.WriteLine($"[RoomMaster.SetPlayerDiscardState] ClientID: {clientID}, WantsDiscard: {wantsDiscard}");

        if (_roomPlayers.TryGetValue(clientID, out var player))
        {
            // Permetti solo se il discard è ancora consentito (primi 15 secondi)
            if (_discardAllowed || !wantsDiscard)
            {
                player.WantsDiscard = wantsDiscard;
                Console.WriteLine($"[RoomMaster.SetPlayerDiscardState] Player {player.NickName} set to WantsDiscard={wantsDiscard}");
            }
            else
            {
                Console.WriteLine($"[RoomMaster.SetPlayerDiscardState] Discard time expired - ignoring request");
            }
        }
        else
        {
            Console.WriteLine($"[RoomMaster.SetPlayerDiscardState] ERROR: ClientID {clientID} not found in _roomPlayers!");
        }
    }

    public void TryStartNewRoundNow()
    {
        lock (_syncObject)
        {
            Console.WriteLine($"[RoomMaster.TryStartNewRoundNow] Current state: {_state}");

            // Parte solo se siamo in attesa (Ready o ShowTime), NON durante un round in corso
            if (_state != RoomMasterState.Ready && _state != RoomMasterState.ShowTime)
            {
                Console.WriteLine($"[RoomMaster.TryStartNewRoundNow] Ignoring - not in Ready or ShowTime state");
                return; // Ignora se c'è un round in corso
            }

            bool everyBodyIsReady = _roomPlayers.Values.All(p => p.IsReady);
            Console.WriteLine($"[RoomMaster.TryStartNewRoundNow] Players: {_roomPlayers.Count}, All ready: {everyBodyIsReady}");

            foreach (var p in _roomPlayers.Values)
            {
                Console.WriteLine($"  - {p.NickName ?? "Unknown"}: IsReady={p.IsReady}");
            }

            if (everyBodyIsReady && _roomPlayers.Count > 0)
            {
                Console.WriteLine($"[RoomMaster.TryStartNewRoundNow] All players ready! Starting round...");
                _state = RoomMasterState.Ready;
            }
        }
    }

    private void StoreDistributionBoard()
    {
        if (_board == null) return;

        _gameSerial++;

        // Converte il board BigBoggler in DTO per il client
        var dtoBoard = new DtoModels.Board
        {
            LocaleID = _board.LocaleID,
            GameSerial = _gameSerial,
            WordCount = 0 // Per ora 0, calcoleremo dopo con Solve()
        };

        // Converte i dadi - DiceArray è una PROPERTY indexer in BigBoggler
        var dices = new List<DtoModels.Dice>();
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                var bigBogDice = _board.DiceArray[i, j]; // Usa indexer []
                var dice = new DtoModels.Dice
                {
                    Letter = bigBogDice.SelectedString,
                    Rotation = bigBogDice.FaceRotation,
                    Index = i * 5 + j,
                    Row = i,
                    Column = j
                };
                dices.Add(dice);
            }
        }
        dtoBoard.DicesVector = dices.ToArray();

        _distributionBoard = dtoBoard;
    }

    private void CheckPlayers()
    {
        if (_state == RoomMasterState.Ready)
        {
            // Controlla se ci sono abbastanza giocatori E tutti sono ready
            if (_roomPlayers.Count >= MIN_PLAYERS)
            {
                bool allReady = _roomPlayers.Values.All(p => p.IsReady);

                if (allReady && _roomPlayers.Count > 0)
                {
                    _state = RoomMasterState.SendingBoard;

                    // Crea nuovo board usando BigBoggler
                    _board = new BigBoggler_Common.Board(BOARD_RANK, "it-IT");
                    _board.Shake();
                    StoreDistributionBoard();

                    _state = RoomMasterState.KeepReady;
                    _ = Task.Run(async () => 
                    {
                        if (NewMatchKeepReady != null)
                            await NewMatchKeepReady.Invoke();
                    });

                    _roundTimer.Run();
                }
            }
        }
        else
        {
            if (_roundTimer.IsRunning && _roomPlayers.Count < MIN_PLAYERS)
            {
                _roundTimer.Reset();
                EndRound();
            }
        }
    }

    private void EndRound()
    {
        _state = RoomMasterState.PauseAfterRound;

        // Ferma timer discard se ancora attivo
        _discardAllowedTimer.Stop();
        _discardAllowed = false;

        // Reset ready state - i giocatori devono cliccare "Sono pronto" prima del prossimo round
        foreach (var player in _roomPlayers.Values)
        {
            player.IsReady = false;
            player.WantsDiscard = false;
        }

        _ = Task.Run(async () => 
        {
            if (RoundTerminate != null)
                await RoundTerminate.Invoke();

            // Notifica ai client che lo stato ready è cambiato (refresh checkbox)
            if (ScoreChange != null)
                await ScoreChange.Invoke();
        });

        _preValidationTimer.Start();
    }

    private async Task ValidateAndSendResults()
    {
        Console.WriteLine("[ValidateAndSendResults] START - Setting state to ValidatingWordLists");
        _state = RoomMasterState.ValidatingWordLists;

        Console.WriteLine("[ValidateAndSendResults] Calling MarkDuplicatedWords...");
        MarkDuplicatedWords();
        Console.WriteLine("[ValidateAndSendResults] MarkDuplicatedWords completed");

        Console.WriteLine("[ValidateAndSendResults] Calling UpdateScores...");
        await UpdateScores();
        Console.WriteLine("[ValidateAndSendResults] UpdateScores completed");

        Console.WriteLine("[ValidateAndSendResults] Invoking ValidatedResults event...");
        if (ValidatedResults != null)
        {
            Console.WriteLine("[ValidateAndSendResults] Calling ValidatedResults.Invoke()");
            await ValidatedResults.Invoke();
            Console.WriteLine("[ValidateAndSendResults] ValidatedResults.Invoke() completed");
        }
        else
        {
            Console.WriteLine("[ValidateAndSendResults] ValidatedResults is NULL!");
        }

        Console.WriteLine("[ValidateAndSendResults] Setting state to ShowTime");
        _state = RoomMasterState.ShowTime;

        Console.WriteLine("[ValidateAndSendResults] Starting ShowTimeTimer");
        _showTimeTimer.Start();

        Console.WriteLine("[ValidateAndSendResults] COMPLETED");
    }

    private void MarkDuplicatedWords()
    {
        // Implementazione semplificata - confronto testo parole
        try
        {
            Console.WriteLine($"[MarkDuplicatedWords] START - Players: {_roomPlayers.Count}");

            foreach (var player1 in _roomPlayers.Values)
            {
                if (player1.WordList?.Items == null)
                {
                    Console.WriteLine($"[MarkDuplicatedWords] Player {player1.NickName ?? "Unknown"} has no words");
                    continue;
                }

                Console.WriteLine($"[MarkDuplicatedWords] Processing player {player1.NickName ?? "Unknown"} with {player1.WordList.Items.Length} words");

                foreach (var word1 in player1.WordList.Items)
                {
                    if (word1 == null) continue;
                    word1.Duplicated = false;

                    var wordText1 = GetWordText(word1);
                    if (string.IsNullOrEmpty(wordText1)) continue;

                    foreach (var player2 in _roomPlayers.Values)
                    {
                        if (player2.ID == player1.ID) continue;
                        if (player2.WordList?.Items == null) continue;

                        var found = player2.WordList.Items.Any(w => w != null && GetWordText(w) == wordText1);
                        if (found)
                        {
                            word1.Duplicated = true;
                            break;
                        }
                    }
                }
            }

            Console.WriteLine("[MarkDuplicatedWords] COMPLETED");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in MarkDuplicatedWords: {ex.Message}");
        }
    }

    private string GetWordText(DtoModels.Word word)
    {
        if (word.DicePath == null || word.DicePath.Count == 0)
            return string.Empty;

        return string.Join("", word.DicePath.Where(d => d != null).Select(d => d.Letter ?? ""));
    }

    private string GetWordText(BigBoggler.Models.WordBase word)
    {
        if (word == null)
            return string.Empty;

        return word.Text;
    }

    private async Task UpdateScores()
    {
        // Copilot comment: Implementazione semplificata - punteggio base per lunghezza

        try
        {
            Console.WriteLine($"[UpdateScores] START - Processing {_roomPlayers.Count} players");

            foreach (var player in _roomPlayers.Values)
            {
                if (player.WordList?.Items == null)
                {
                    Console.WriteLine($"[UpdateScores] Player {player.NickName ?? "Unknown"} has no words");
                    continue;
                }

                Console.WriteLine($"[UpdateScores] Player {player.NickName} has {player.WordList.Items.Length} words");

                var wordsList = player.WordList.Items
                    .Where(w => w != null && !w.Duplicated)
                    .ToList();
                player.WordList.Items = wordsList.ToArray();

                Console.WriteLine($"[UpdateScores] Player {player.NickName ?? "Unknown"} after removing duplicates: {wordsList.Count} words");

                int score = 0;
                foreach (var word in player.WordList.Items)
                {
                    if (word == null) continue;

                    var length = word.DicePath?.Count ?? 0;
                    score += length switch
                    {
                        3 => 1,
                        4 => 1,
                        5 => 2,
                        6 => 3,
                        7 => 5,
                        >= 8 => 11,
                        _ => 0
                    };
                }

                player.Score += score;
                Console.WriteLine($"[UpdateScores] Player {player.NickName ?? "Unknown"} scored {score} points, total: {player.Score}");
            }

            Console.WriteLine("[UpdateScores] Invoking ScoreChange event...");
            if (ScoreChange != null)
                await ScoreChange.Invoke();

            Console.WriteLine("[UpdateScores] COMPLETED");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in UpdateScores: {ex.Message}");
        }


        //TO DO old VB version as a reference for implementation.\\
        //For p As Integer = 0 To _rooomPlayers.Count - 1
        //    Dim player As KeyValuePair(Of String, BigBoggler_Common.Player) = _rooomPlayers.ElementAt(p)
        //    For i = 0 To player.Value.WordList.Count - 1
        //        Try
        //            If player.Value.WordList.Values(i).Duplicated Then
        //                If player.Value.WordList.Values(i).Duplicated Then player.Value.WordList.Remove(player.Value.WordList.Keys(i))
        //            End If
        //        Catch ex As Exception

        //        End Try

        //    Next
        //    Dim score As Integer = player.Value.WordList.GetTotalScore(False)
        //    player.Value.GameScore += score
        //    'player.Value.AbsoluteScore += score
        //Next

        //RaiseEvent ScoreChange()

    }

    private void PreStartDelayTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        _state = RoomMasterState.RunningRound;

        _ = Task.Run(async () => 
        {
            if (RoundStart != null)
                await RoundStart.Invoke();
        });

        _roundStartTimeUTC = DateTime.UtcNow;

        // Abilita il discard per i primi 15 secondi
        _discardAllowed = true;
        _discardAllowedTimer.Start();

        _roundTimer.Run();
    }

    private async void PreValidationTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        Console.WriteLine("[RoomMaster] PreValidationTimer_Elapsed - Calling ValidateAndSendResults()");
        await ValidateAndSendResults();
    }

    private void ShowTimeTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        Console.WriteLine("[RoomMaster] ShowTimeTimer_Elapsed");
        if (_state == RoomMasterState.ShowTime)
        {
            _state = RoomMasterState.Ready;
        }
    }

    private void CheckPlayersCycleTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        CheckPlayers();
        CheckDiscard();
    }

    private void CheckDiscard()
    {
        // Controlla solo durante RunningRound e se il discard è ancora permesso
        if (_state == RoomMasterState.RunningRound && _discardAllowed)
        {
            bool allWantDiscard = _roomPlayers.Values.All(p => p.WantsDiscard);

            if (allWantDiscard && _roomPlayers.Count > 0)
            {
                Console.WriteLine("[RoomMaster.CheckDiscard] All players want to discard - regenerating board");

                // Ferma i timer del round corrente
                _roundTimer.Reset();
                _discardAllowedTimer.Stop();
                _discardAllowed = false;

                // Reset stati WantsDiscard e mantieni IsReady=true
                foreach (var player in _roomPlayers.Values)
                {
                    player.WantsDiscard = false;
                    player.IsReady = true; // Rimangono pronti
                    player.WordList = new DtoModels.WordList { Items = Array.Empty<DtoModels.Word>() }; // Reset wordlist
                }

                // Genera nuova board (seriale incrementa)
                _board = new BigBoggler_Common.Board(BOARD_RANK, "it-IT");
                _board.Shake();
                StoreDistributionBoard();

                // Notifica i client che la board è stata scartata
                _ = Task.Run(async () =>
                {
                    if (BoardDiscarded != null)
                        await BoardDiscarded.Invoke();
                });

                // Torna a KeepReady e riavvia il round
                _state = RoomMasterState.KeepReady;
                _ = Task.Run(async () =>
                {
                    if (NewMatchKeepReady != null)
                        await NewMatchKeepReady.Invoke();
                });

                _preStartDelayTimer.Start();
            }
        }
    }

    private void DiscardAllowedTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        Console.WriteLine("[RoomMaster] DiscardAllowedTimer_Elapsed - Discard no longer allowed");
        _discardAllowed = false;

        // Reset tutti i WantsDiscard a false (tempo scaduto)
        foreach (var player in _roomPlayers.Values)
        {
            player.WantsDiscard = false;
        }

        // Notifica ai client per aggiornare UI
        _ = Task.Run(async () =>
        {
            if (ScoreChange != null)
                await ScoreChange.Invoke();
        });
    }
}
