using System.Collections.Concurrent;
using System.Timers;
using Microsoft.AspNetCore.SignalR;
using WebBoggler.SignalRServer.Models;
using Timer = System.Timers.Timer;

namespace WebBoggler.SignalRServer.Services;

public class RoomMaster
{
    private readonly ConcurrentDictionary<string, Player> _roomPlayers = new();
    private Board? _board;
    private Board? _distributionBoard;
    private WordList? _solutionWordList;
    private static long _gameSerial = 0;

    private readonly object _syncObject = new();
    private readonly IHubContext<GameHub> _hubContext;

    public event Func<Task>? RoundStart;
    public event Func<Task>? RoundTerminate;
    public event Func<Task>? ValidatedResults;
    public event Func<Task>? NewMatchKeepReady;
    public event Func<Task>? ScoreChange;

    private const int BOARD_RANK = 5;
    private const int MIN_PLAYERS = 0;

    private const int GAME_PRE_START_DELAY = 3000;
#if DEBUG
    private const int GAME_ROUND_DURATION_MS = 60000;
#else
    private const int GAME_ROUND_DURATION_MS = 180000;
#endif
    private const int GAME_PRE_VALIDATION_INTERVAL_MS = 3500;
    private const int GAME_SHOWTIME_PAUSE_MS = 60000;
    private const int CYCLE_INTERVAL_MS = 250;

    private readonly Timer _hourglassTimer;
    private readonly Timer _preStartDelayTimer;
    private readonly Timer _preValidationTimer;
    private readonly Timer _showTimeTimer;
    private readonly Timer _checkPlayersCycleTimer;

    private static DateTime _roundStartTimeUTC;
    private RoomMasterState _state = RoomMasterState.Ready;

    public enum RoomMasterState
    {
        Ready,
        SendingBoard,
        KeepReady,
        RunningRound,
        PauseAfterRound,
        ValidatingWordLists,
        ShowTime
    }

    public RoomMaster(IHubContext<GameHub> hubContext, long startingBoardSerial = 0)
    {
        _hubContext = hubContext;
        _gameSerial = startingBoardSerial;

        _board = CreateBoard("it-IT");
        _board.Shake();
        StoreDistributionBoard();

        _hourglassTimer = new Timer(GAME_ROUND_DURATION_MS) { AutoReset = false };
        _hourglassTimer.Elapsed += HourglassTimer_Elapsed;

        _preStartDelayTimer = new Timer(GAME_PRE_START_DELAY) { AutoReset = false };
        _preStartDelayTimer.Elapsed += PreStartDelayTimer_Elapsed;

        _preValidationTimer = new Timer(GAME_PRE_VALIDATION_INTERVAL_MS) { AutoReset = false };
        _preValidationTimer.Elapsed += PreValidationTimer_Elapsed;

        _showTimeTimer = new Timer(GAME_SHOWTIME_PAUSE_MS) { AutoReset = false };
        _showTimeTimer.Elapsed += ShowTimeTimer_Elapsed;

        _checkPlayersCycleTimer = new Timer(CYCLE_INTERVAL_MS) { AutoReset = true };
        _checkPlayersCycleTimer.Elapsed += CheckPlayersCycleTimer_Elapsed;
        _checkPlayersCycleTimer.Start();

        _state = RoomMasterState.Ready;
    }

    public RoomMasterState State => _state;
    public Board? Board => _distributionBoard;
    public int RoundDurationMS => GAME_ROUND_DURATION_MS;
    public int DeadTimeAmountMS => GAME_PRE_VALIDATION_INTERVAL_MS + GAME_SHOWTIME_PAUSE_MS + GAME_PRE_START_DELAY;
    public DateTime RoundStartTimeUTC => _roundStartTimeUTC;

    public Players GetRoomPlayers(string clientID)
    {
        var playersList = new List<Player>();

        foreach (var ply in _roomPlayers.Values)
        {
            var newPlayer = new Player
            {
                ID = ply.ID,
                NickName = ply.NickName,
                Score = ply.Score,
                WordList = ply.WordList,
                IsLocal = ply.ID == clientID
            };
            playersList.Add(newPlayer);
        }

        return new Players { Items = playersList.ToArray() };
    }

    public bool AddRoundPlayer(string playerID, string name, int gameScore = 0, long rank = 0)
    {
        var player = new Player
        {
            ID = playerID,
            NickName = name,
            Score = gameScore,
            IsReady = false,
            WordList = new WordList { Items = Array.Empty<Word>() }
        };

        return _roomPlayers.TryAdd(playerID, player);
    }

    public void RemoveRoundPlayer(string playerID)
    {
        _roomPlayers.TryRemove(playerID, out _);
    }

    public WordList? GetSolution()
    {
        return _solutionWordList;
    }

    public void AddWordList(WordList wordList, string playerID)
    {
        if (_roomPlayers.TryGetValue(playerID, out var player))
        {
            player.WordList = wordList;
        }
    }

    public void SetPlayerReadyState(string clientID, bool ready)
    {
        if (_roomPlayers.TryGetValue(clientID, out var player))
        {
            player.IsReady = ready;
        }
    }

    public void TryStartNewRoundNow()
    {
        lock (_syncObject)
        {
            // Parte solo se siamo in attesa (Ready o ShowTime), NON durante un round in corso
            if (_state != RoomMasterState.Ready && _state != RoomMasterState.ShowTime)
            {
                return; // Ignora se c'è un round in corso
            }

            bool everyBodyIsReady = _roomPlayers.Values.All(p => p.IsReady);
            if (everyBodyIsReady && _roomPlayers.Count > 0)
            {
                _state = RoomMasterState.Ready;
            }
        }
    }

    private Board CreateBoard(string localeID)
    {
        var board = new Board
        {
            LocaleID = localeID,
            DicesVector = new Dice[25],
            GameSerial = _gameSerial
        };

        var random = new Random();
        string[] letters = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "L", "M", "N", "O", "P", "Qu", "R", "S", "T", "U", "V", "Z" };

        for (int i = 0; i < 25; i++)
        {
            board.DicesVector[i] = new Dice
            {
                Index = i,
                Letter = letters[random.Next(letters.Length)],
                Rotation = random.Next(4) * 90,
                Row = i / 5,
                Column = i % 5
            };
        }

        return board;
    }

    private void StoreDistributionBoard()
    {
        if (_board == null) return;

        _gameSerial++;
        _board.GameSerial = _gameSerial;

        _solutionWordList = new WordList { Items = Array.Empty<Word>() };

        _distributionBoard = _board;
    }

    private void CheckPlayers()
    {
        if (_state == RoomMasterState.Ready)
        {
            if (_roomPlayers.Count >= MIN_PLAYERS)
            {
                _state = RoomMasterState.SendingBoard;

                _board = CreateBoard("it-IT");
                _board.Shake();
                StoreDistributionBoard();

                _state = RoomMasterState.KeepReady;
                _ = Task.Run(async () => 
                {
                    if (NewMatchKeepReady != null)
                        await NewMatchKeepReady.Invoke();
                });

                _preStartDelayTimer.Start();
            }
        }
        else
        {
            if (_hourglassTimer.Enabled && _roomPlayers.Count < MIN_PLAYERS)
            {
                _hourglassTimer.Stop();
                EndRound();
            }
        }
    }

    private void EndRound()
    {
        _state = RoomMasterState.PauseAfterRound;

        _ = Task.Run(async () => 
        {
            if (RoundTerminate != null)
                await RoundTerminate.Invoke();
        });

        _preValidationTimer.Start();
    }

    private void ValidateAndSendResults()
    {
        _state = RoomMasterState.ValidatingWordLists;

        MarkDuplicatedWords();
        UpdateScores();

        _ = Task.Run(async () => 
        {
            if (ValidatedResults != null)
                await ValidatedResults.Invoke();
        });

        _state = RoomMasterState.ShowTime;
        _showTimeTimer.Start();
    }

    private void MarkDuplicatedWords()
    {
        try
        {
            foreach (var player1 in _roomPlayers.Values)
            {
                if (player1.WordList?.Items == null) continue;

                foreach (var word1 in player1.WordList.Items)
                {
                    if (word1 == null) continue; // Skip null words

                    word1.Duplicated = false;

                    foreach (var player2 in _roomPlayers.Values)
                    {
                        if (player2.ID == player1.ID) continue;
                        if (player2.WordList?.Items == null) continue;

                        var wordText1 = GetWordText(word1);
                        if (string.IsNullOrEmpty(wordText1)) continue; // Skip invalid words

                        var found = player2.WordList.Items.Any(w => w != null && GetWordText(w) == wordText1);

                        if (found)
                        {
                            word1.Duplicated = true;
                            break;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in MarkDuplicatedWords: {ex.Message}");
        }
    }

    private string GetWordText(Word word)
    {
        if (word.DicePath == null || word.DicePath.Count == 0) 
            return string.Empty;

        try
        {
            // Filtra eventuali null nel DicePath prima di fare il join
            return string.Join("", word.DicePath.Where(d => d != null).Select(d => d.Letter ?? ""));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetWordText: {ex.Message}");
            return string.Empty;
        }
    }

    private void UpdateScores()
    {
        try
        {
            foreach (var player in _roomPlayers.Values)
            {
                if (player.WordList?.Items == null) continue;

                // Filtra parole null e duplicati
                var wordsList = player.WordList.Items
                    .Where(w => w != null && !w.Duplicated)
                    .ToList();
                player.WordList.Items = wordsList.ToArray();

                int score = 0;
                foreach (var word in player.WordList.Items)
                {
                    if (word == null) continue; // Safety check

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
            }

            _ = Task.Run(async () => 
            {
                if (ScoreChange != null)
                    await ScoreChange.Invoke();
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in UpdateScores: {ex.Message}");
        }
    }

    private void PreStartDelayTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        _state = RoomMasterState.RunningRound;

        foreach (var player in _roomPlayers.Values)
        {
            player.IsReady = false;
        }

        _ = Task.Run(async () => 
        {
            if (RoundStart != null)
                await RoundStart.Invoke();
        });

        _roundStartTimeUTC = DateTime.UtcNow;
        _hourglassTimer.Start();
    }

    private void HourglassTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        EndRound();
    }

    private void PreValidationTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        ValidateAndSendResults();
    }

    private void ShowTimeTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        if (_state == RoomMasterState.ShowTime)
        {
            _state = RoomMasterState.Ready;
        }
    }

    private void CheckPlayersCycleTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        CheckPlayers();
    }
}
