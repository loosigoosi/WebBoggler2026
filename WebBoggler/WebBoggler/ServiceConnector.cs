using System;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace WebBoggler
{
	class ServiceConnector
	{

		////website
		//private const string WCFSERVICE_URL = @"http://webboggler.xidea.it/gameserver/ServiceWebBoggler.svc";
		//private const string SIGNALR_HUB_URL = @"http://webboggler.xidea.it/gamehub";

		////website dev
		//private const string WCFSERVICE_URL = @"http://webbogglerdev.xidea.it/gameserver/ServiceWebBoggler.svc";
		//private const string SIGNALR_HUB_URL = @"http://webbogglerdev.xidea.it/gamehub";

		//local
			private const string WCFSERVICE_URL = @"http://localhost:8733/gameserver/ServiceWebBoggler.svc";
			private const string SIGNALR_HUB_URL = @"http://localhost:5170/gamehub";

			// Proprietà pubblica per accedere all'URL nei messagebox
			internal static string SignalRHubUrl => SIGNALR_HUB_URL;

		////localIIS
		//private const string WCFSERVICE_URL = @"http://localhost/ServiceWebBoggler.svc";
		//private const string SIGNALR_HUB_URL = @"http://localhost/gamehub";

		internal WebBogglerServer.ServiceWebBogglerClient ConnectService()
		{

			var binding = new BasicHttpBinding();
			binding.MaxReceivedMessageSize = 512000; //WordList di soluzione è più largo del default di 64535
			var  client = new WebBogglerServer.ServiceWebBogglerClient (binding, new EndpointAddress(new Uri(WCFSERVICE_URL)));


			return client; 
		}

		internal async Task<SignalRGameClient> ConnectSignalRAsync(CancellationToken cancellationToken = default)
		{
			var client = new SignalRGameClient();
			await client.ConnectAsync(SIGNALR_HUB_URL, cancellationToken);
			return client;
		}

	}

	public class SignalRGameClient
	{
		private HubConnection? _connection;
		private readonly SynchronizationContext? _syncContext;

		// Events compatible with the old WebSocket shim
		public event EventHandler? OnOpen;
		public event EventHandler<MessageEventArgs>? OnMessage;
		public event EventHandler? OnClose;
		public event EventHandler<ErrorEventArgs>? OnError;

		public SignalRGameClient()
		{
			_syncContext = SynchronizationContext.Current;
		}

		public async Task ConnectAsync(string hubUrl, CancellationToken cancellationToken = default)
		{
			try
			{
				_connection = new HubConnectionBuilder()
					.WithUrl(hubUrl)
					.WithAutomaticReconnect()
					.Build();

				// Register server-to-client message handlers
				_connection.On<string>("Message", (message) =>
				{
					PostInvoke(() => OnMessage?.Invoke(this, new MessageEventArgs { Data = message }));
				});

				_connection.On("Registered", () =>
				{
					PostInvoke(() => OnMessage?.Invoke(this, new MessageEventArgs { Data = "REGISTERED" }));
				});

				_connection.On<string>("ClientId", (clientId) =>
				{
					PostInvoke(() => OnMessage?.Invoke(this, new MessageEventArgs { Data = clientId }));
				});

				_connection.On("GetBoard", () =>
				{
					PostInvoke(() => OnMessage?.Invoke(this, new MessageEventArgs { Data = "GET_BOARD" }));
				});

				_connection.On("StartRound", () =>
				{
					PostInvoke(() => OnMessage?.Invoke(this, new MessageEventArgs { Data = "START_ROUND" }));
				});

				_connection.On("EndRound", () =>
				{
					PostInvoke(() => OnMessage?.Invoke(this, new MessageEventArgs { Data = "END_ROUND" }));
				});

				_connection.On("ShowTime", () =>
				{
					System.Diagnostics.Debug.WriteLine("[ServiceConnector] ShowTime event received from server");
					try
					{
						PostInvoke(() =>
						{
							System.Diagnostics.Debug.WriteLine("[ServiceConnector] PostInvoke executing for ShowTime");
							OnMessage?.Invoke(this, new MessageEventArgs { Data = "SHOW_TIME" });
							System.Diagnostics.Debug.WriteLine("[ServiceConnector] OnMessage invoked for ShowTime");
						});
					}
					catch (Exception ex)
					{
						System.Diagnostics.Debug.WriteLine($"[ServiceConnector] ERROR in ShowTime handler: {ex.Message}");
					}
				});

				_connection.On("UpdatePlayers", () =>
				{
					PostInvoke(() => OnMessage?.Invoke(this, new MessageEventArgs { Data = "UPDATE_PLAYERS" }));
				});

				_connection.Closed += async (error) =>
				{
					PostInvoke(() => OnClose?.Invoke(this, EventArgs.Empty));
					await Task.CompletedTask;
				};

				await _connection.StartAsync(cancellationToken);
				PostInvoke(() => OnOpen?.Invoke(this, EventArgs.Empty));
			}
			catch (Exception ex)
			{
				PostInvoke(() => OnError?.Invoke(this, new ErrorEventArgs { Message = ex.Message }));
				throw;
			}
		}

		private void PostInvoke(Action action)
		{
			if (_syncContext != null)
			{
				try { _syncContext.Post(_ => action(), null); }
				catch { action(); }
			}
			else
			{
				try { action(); } catch { }
			}
		}

		// Send methods (Client-to-Server)
		public void Send(string message)
		{
			_ = SendAsync(message);
		}

		public async Task SendAsync(string message)
		{
			if (_connection == null || _connection.State != HubConnectionState.Connected)
				return;

			try
			{
				switch (message.ToUpper())
				{
					case "REGISTER":
						await _connection.InvokeAsync("Register");
						break;
					case "REMOVE":
						await _connection.InvokeAsync("Remove");
						break;
					case "READY":
						await _connection.InvokeAsync("Ready");
						break;
					case "NOTREADY":
						await _connection.InvokeAsync("NotReady");
						break;
					default:
						await _connection.InvokeAsync("Echo", message);
						break;
				}
			}
			catch (Exception ex)
			{
				PostInvoke(() => OnError?.Invoke(this, new ErrorEventArgs { Message = ex.Message }));
			}
		}

		public async Task DisconnectAsync()
		{
			if (_connection != null)
			{
				await _connection.StopAsync();
				await _connection.DisposeAsync();
			}
		}

		// Game logic methods (typed SignalR calls)
		public async Task<WebBogglerServer.Board?> GetBoardAsync(string localeID)
		{
			if (_connection == null || _connection.State != HubConnectionState.Connected)
				return null;

			try
			{
				// Call the SignalR hub method and map the response to the WCF proxy type
				var board = await _connection.InvokeAsync<WebBogglerServer.Board>("GetBoard", localeID);
				return board;
			}
			catch (Exception ex)
			{
				PostInvoke(() => OnError?.Invoke(this, new ErrorEventArgs { Message = ex.Message }));
				return null;
			}
		}

		public async Task<WebBogglerServer.GameInfo?> ObserveAsync()
		{
			if (_connection == null || _connection.State != HubConnectionState.Connected)
				return null;

			try
			{
				var gameInfo = await _connection.InvokeAsync<WebBogglerServer.GameInfo>("Observe");
				return gameInfo;
			}
			catch (Exception ex)
			{
				PostInvoke(() => OnError?.Invoke(this, new ErrorEventArgs { Message = ex.Message }));
				return null;
			}
		}

		public async Task<bool> JoinAsync(string clientID, string userName)
		{
			if (_connection == null || _connection.State != HubConnectionState.Connected)
				return false;

			try
			{
				return await _connection.InvokeAsync<bool>("Join", clientID, userName);
			}
			catch (Exception ex)
			{
				PostInvoke(() => OnError?.Invoke(this, new ErrorEventArgs { Message = ex.Message }));
				return false;
			}
		}

		public async Task<bool> LeaveAsync(string clientID)
		{
			if (_connection == null || _connection.State != HubConnectionState.Connected)
				return false;

			try
			{
				return await _connection.InvokeAsync<bool>("Leave", clientID);
			}
			catch (Exception ex)
			{
				PostInvoke(() => OnError?.Invoke(this, new ErrorEventArgs { Message = ex.Message }));
				return false;
			}
		}

		public async Task<bool> CheckWordAsync(string word)
		{
			if (_connection == null || _connection.State != HubConnectionState.Connected)
				return false;

			try
			{
				return await _connection.InvokeAsync<bool>("CheckWord", word);
			}
			catch (Exception ex)
			{
				PostInvoke(() => OnError?.Invoke(this, new ErrorEventArgs { Message = ex.Message }));
				return false;
			}
		}

		public async Task SendWordListAsync(WebBogglerServer.WordList wordList, string clientID)
		{
			System.Diagnostics.Debug.WriteLine($"[ServiceConnector.SendWordListAsync] START - clientID: {clientID}");
			System.Diagnostics.Debug.WriteLine($"[ServiceConnector.SendWordListAsync] wordList items: {wordList?.Items?.Length ?? 0}");

			if (_connection == null || _connection.State != HubConnectionState.Connected)
			{
				System.Diagnostics.Debug.WriteLine($"[ServiceConnector.SendWordListAsync] ERROR: Connection not available! State: {_connection?.State}");
				return;
			}

			try
			{
				System.Diagnostics.Debug.WriteLine($"[ServiceConnector.SendWordListAsync] Calling InvokeAsync('SendWordList')...");
				await _connection.InvokeAsync("SendWordList", wordList, clientID);
				System.Diagnostics.Debug.WriteLine($"[ServiceConnector.SendWordListAsync] InvokeAsync completed successfully");
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"[ServiceConnector.SendWordListAsync] EXCEPTION: {ex.Message}");
				System.Diagnostics.Debug.WriteLine($"[ServiceConnector.SendWordListAsync] Stack: {ex.StackTrace}");
				PostInvoke(() => OnError?.Invoke(this, new ErrorEventArgs { Message = ex.Message }));
			}
		}

		public async Task<WebBogglerServer.Players?> GetPlayersAsync(string clientID)
		{
			if (_connection == null || _connection.State != HubConnectionState.Connected)
				return null;

			try
			{
				System.Diagnostics.Debug.WriteLine($"[GetPlayersAsync] Calling server with clientID: {clientID}");
				var result = await _connection.InvokeAsync<WebBogglerServer.Players>("GetPlayers", clientID).ConfigureAwait(false);
				System.Diagnostics.Debug.WriteLine($"[GetPlayersAsync] Received response, Items count: {result?.Items?.Length ?? 0}");

				if (result?.Items != null && result.Items.Length > 0)
				{
					System.Diagnostics.Debug.WriteLine($"[GetPlayersAsync] First player: {result.Items[0]?.NickName}, WordList items: {result.Items[0]?.WordList?.Items?.Length ?? 0}");
				}

				return result;
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"[GetPlayersAsync] ERROR: {ex.Message}\n{ex.StackTrace}");
				PostInvoke(() => OnError?.Invoke(this, new ErrorEventArgs { Message = ex.Message }));
				return null;
			}
		}

		public async Task<WebBogglerServer.WordList?> GetSolutionAsync()
		{
			if (_connection == null || _connection.State != HubConnectionState.Connected)
				return null;

			try
			{
				System.Diagnostics.Debug.WriteLine("[GetSolutionAsync] Calling server...");
				var result = await _connection.InvokeAsync<WebBogglerServer.WordList>("GetSolution").ConfigureAwait(false);
				System.Diagnostics.Debug.WriteLine($"[GetSolutionAsync] Received response, Items count: {result?.Items?.Length ?? 0}");
				return result;
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"[GetSolutionAsync] ERROR: {ex.Message}\n{ex.StackTrace}");
				PostInvoke(() => OnError?.Invoke(this, new ErrorEventArgs { Message = ex.Message }));
				return null;
			}
		}
	}

	// Event args compatible with the old WebSocket shim
	public class MessageEventArgs : EventArgs
	{
		public object? Data { get; set; }
	}

	public class ErrorEventArgs : EventArgs
	{
		public string Message { get; set; } = string.Empty;
	}
}
