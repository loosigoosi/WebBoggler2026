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
        lock (_lockObject)
        {
            if (_connectionToClientId.TryGetValue(Context.ConnectionId, out var clientId))
            {
                _roomMaster.SetPlayerReadyState(clientId, true);
                _roomMaster.TryStartNewRoundNow();
            }
        }
        // Notifica tutti i client dell'aggiornamento stato giocatori
        await Clients.All.SendAsync("UpdatePlayers");
    }

    public async Task NotReady()
    {
        lock (_lockObject)
        {
            if (_connectionToClientId.TryGetValue(Context.ConnectionId, out var clientId))
            {
                _roomMaster.SetPlayerReadyState(clientId, false);
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
        var result = _roomMaster.AddRoundPlayer(clientID, userName);

        if (result)
        {
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
        _roomMaster.AddWordList(wordList, clientID);
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
}

