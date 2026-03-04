using Microsoft.AspNetCore.SignalR;
using WebBoggler.SignalRServer.Models;
using WebBoggler.SignalRServer.Services;

namespace WebBoggler.SignalRServer;

public class GameHub : Hub
{
    private static readonly Random _random = new();
    private static readonly Dictionary<string, string> _connectionToClientId = new();
    private static readonly object _lockObject = new();
    private readonly RoomMaster _roomMaster;

    public GameHub(RoomMaster roomMaster)
    {
        _roomMaster = roomMaster;
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        lock (_lockObject)
        {
            if (_connectionToClientId.TryGetValue(Context.ConnectionId, out var clientId))
            {
                _roomMaster.RemoveRoundPlayer(clientId);
                _connectionToClientId.Remove(Context.ConnectionId);
            }
        }

        await Clients.All.SendAsync("UpdatePlayers");
        await base.OnDisconnectedAsync(exception);
    }

    public async Task Register()
    {
        var clientId = _random.Next().ToString();

        lock (_lockObject)
        {
            _connectionToClientId[Context.ConnectionId] = clientId;
        }

        await Clients.Caller.SendAsync("Registered");
        await Clients.Caller.SendAsync("ClientId", "#" + clientId.Trim());
    }

    public async Task Remove()
    {
        lock (_lockObject)
        {
            if (_connectionToClientId.TryGetValue(Context.ConnectionId, out var clientId))
            {
                _roomMaster.RemoveRoundPlayer(clientId);
                _connectionToClientId.Remove(Context.ConnectionId);
            }
        }

        await Clients.All.SendAsync("UpdatePlayers");
    }

    public async Task Ready()
    {
        Console.WriteLine($"[GameHub.Ready] Called by ConnectionId: {Context.ConnectionId}");

        lock (_lockObject)
        {
            if (_connectionToClientId.TryGetValue(Context.ConnectionId, out var clientId))
            {
                Console.WriteLine($"[GameHub.Ready] Setting clientId {clientId} to READY");
                _roomMaster.SetPlayerReadyState(clientId, true);
                _roomMaster.TryStartNewRoundNow();
            }
            else
            {
                Console.WriteLine($"[GameHub.Ready] ERROR: ConnectionId {Context.ConnectionId} not found in mapping!");
            }
        }
        // Notifica tutti i client dell'aggiornamento stato giocatori
        await Clients.All.SendAsync("UpdatePlayers");
    }

    public async Task NotReady()
    {
        Console.WriteLine($"[GameHub.NotReady] Called by ConnectionId: {Context.ConnectionId}");

        lock (_lockObject)
        {
            if (_connectionToClientId.TryGetValue(Context.ConnectionId, out var clientId))
            {
                Console.WriteLine($"[GameHub.NotReady] Setting clientId {clientId} to NOT READY");
                _roomMaster.SetPlayerReadyState(clientId, false);
            }
            else
            {
                Console.WriteLine($"[GameHub.NotReady] ERROR: ConnectionId {Context.ConnectionId} not found in mapping!");
            }
        }
        // Notifica tutti i client dell'aggiornamento stato giocatori
        await Clients.All.SendAsync("UpdatePlayers");
    }

    public async Task Echo(string message)
    {
        var response = $"Got message {message} at {DateTime.Now.ToLongTimeString()}";
        await Clients.Caller.SendAsync("Message", response);
    }

    public async Task<Board> GetBoard(string localeID)
    {
        var board = _roomMaster.Board;
        if (board is null)
        {
            board = new Board
            {
                LocaleID = localeID,
                DicesVector = new Dice[25],
                WordCount = 0,
                GameSerial = 0
            };

            for (int i = 0; i < 25; i++)
            {
                board.DicesVector[i] = new Dice
                {
                    Index = i,
                    Letter = "A",
                    Rotation = 0,
                    Row = i / 5,
                    Column = i % 5
                };
            }
        }

        return await Task.FromResult(board);
    }

    public async Task<GameInfo> Observe()
    {
        var roundElapsedTimeMS = 0;
        if (_roomMaster.State == RoomMaster.RoomMasterState.RunningRound)
        {
            roundElapsedTimeMS = (int)(DateTime.UtcNow - _roomMaster.RoundStartTimeUTC).TotalMilliseconds;
        }

        var gameInfo = new GameInfo
        {
            ServerTimeUTC = DateTime.UtcNow.ToString("o"),
            RoomState = _roomMaster.State.ToString(),
            RoundStartTime = _roomMaster.RoundStartTimeUTC,
            RoundElapsedTimeMS = roundElapsedTimeMS,
            RoundDurationMS = _roomMaster.RoundDurationMS,
            DeadTimeAmountMS = _roomMaster.DeadTimeAmountMS
        };

        return await Task.FromResult(gameInfo);
    }

    public async Task<bool> Join(string clientID, string userName)
    {
        Console.WriteLine($"[GameHub.Join] Called by ConnectionId: {Context.ConnectionId}, client sent clientID: {clientID}, userName: {userName}");

        // IMPORTANTE: Ignora il clientID inviato dal client, usa quello mappato dal ConnectionId
        string? mappedClientId = null;

        lock (_lockObject)
        {
            if (_connectionToClientId.TryGetValue(Context.ConnectionId, out mappedClientId))
            {
                Console.WriteLine($"[GameHub.Join] Using mapped clientID: {mappedClientId} instead of {clientID}");
            }
            else
            {
                Console.WriteLine($"[GameHub.Join] ERROR: ConnectionId {Context.ConnectionId} not found in mapping!");
            }
        }

        if (mappedClientId == null)
        {
            return await Task.FromResult(false);
        }

        var result = _roomMaster.AddRoundPlayer(mappedClientId, userName);
        Console.WriteLine($"[GameHub.Join] AddRoundPlayer result: {result}");

        if (result)
        {
            // Controlla se serve resettare il gioco (secondo giocatore)
            _roomMaster.OnPlayerJoined();

            await Clients.All.SendAsync("UpdatePlayers");
        }

        return await Task.FromResult(result);
    }

    public async Task<bool> Leave(string clientID)
    {
        _roomMaster.RemoveRoundPlayer(clientID);
        await Clients.All.SendAsync("UpdatePlayers");
        return await Task.FromResult(true);
    }

    public async Task<bool> CheckWord(string word)
    {
        // Usa il Lexicon di RoomMaster per validare la parola
        return await Task.FromResult(_roomMaster.CheckWord(word));
    }

    public async Task SendWordList(WordList wordList, string clientID)
    {
        Console.WriteLine($"[GameHub.SendWordList] Called by ConnectionId: {Context.ConnectionId}, client sent clientID: {clientID}");

        // Usa il mapped ID invece di quello inviato dal client
        string? mappedClientId = null;

        lock (_lockObject)
        {
            if (_connectionToClientId.TryGetValue(Context.ConnectionId, out mappedClientId))
            {
                Console.WriteLine($"[GameHub.SendWordList] Using mapped clientID: {mappedClientId}, wordList has {wordList.Items?.Length ?? 0} words");
            }
            else
            {
                Console.WriteLine($"[GameHub.SendWordList] ERROR: ConnectionId {Context.ConnectionId} not found in mapping!");
            }
        }

        if (mappedClientId != null)
        {
            _roomMaster.AddWordList(wordList, mappedClientId);
            Console.WriteLine($"[GameHub.SendWordList] WordList added successfully for player {mappedClientId}");
        }

        await Task.CompletedTask;
    }

    public async Task<Players> GetPlayers(string clientID)
    {
        var players = _roomMaster.GetRoomPlayers(clientID);
        return await Task.FromResult(players);
    }

    public async Task<WordList> GetSolution()
    {
        var solution = _roomMaster.GetSolution();
        if (solution == null)
        {
            solution = new WordList { Items = Array.Empty<Word>() };
        }
        return await Task.FromResult(solution);
    }

    public async Task ProposeDiscard(bool wantsDiscard)
    {
        Console.WriteLine($"[GameHub.ProposeDiscard] Called by ConnectionId: {Context.ConnectionId}, wantsDiscard: {wantsDiscard}");

        lock (_lockObject)
        {
            if (_connectionToClientId.TryGetValue(Context.ConnectionId, out var clientId))
            {
                Console.WriteLine($"[GameHub.ProposeDiscard] Setting clientId {clientId} WantsDiscard={wantsDiscard}");
                _roomMaster.SetPlayerDiscardState(clientId, wantsDiscard);
            }
            else
            {
                Console.WriteLine($"[GameHub.ProposeDiscard] ERROR: ConnectionId {Context.ConnectionId} not found in mapping!");
            }
        }

        // Notifica tutti i client dell'aggiornamento
        await Clients.All.SendAsync("UpdatePlayers");
    }

    public async Task<bool> IsDiscardAllowed()
    {
        return await Task.FromResult(_roomMaster.DiscardAllowed);
    }
}

