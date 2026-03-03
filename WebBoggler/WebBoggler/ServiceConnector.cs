using System;
using System.Net.WebSockets;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebBoggler
{
    class ServiceConnector
    {

        ////website
        //private const string WCFSERVICE_URL = @"http://webboggler.xidea.it/gameserver/ServiceWebBoggler.svc";
        //private const string WEBSOCKETSERVICE_URL = @"ws://webboggler.xidea.it/gameserver/ServiceWebSocket.svc";

        ////website dev
        //private const string WCFSERVICE_URL = @"http://webbogglerdev.xidea.it/gameserver/ServiceWebBoggler.svc";
        //private const string WEBSOCKETSERVICE_URL = @"ws://webbogglerdev.xidea.it/gameserver/ServiceWebSocket.svc";

        //local
        private const string WCFSERVICE_URL = @"http://localhost:8734/ServiceWebBoggler.svc";
        private const string WEBSOCKETSERVICE_URL = @"ws://localhost:8734/ServiceWebSocket.svc";

        ////localIIS
        //private const string WCFSERVICE_URL = @"http://localhost/ServiceWebBoggler.svc";
        //private const string WEBSOCKETSERVICE_URL = @"ws://localhost/ServiceWebSocket.svc";

        internal WebBogglerServer.ServiceWebBogglerClient ConnectService()
        {

            var binding = new BasicHttpBinding();
            binding.MaxReceivedMessageSize = 512000; //WordList di soluzione è più largo del default di 64535
            var  client = new WebBogglerServer.ServiceWebBogglerClient (binding, new EndpointAddress(new Uri(WCFSERVICE_URL)));
            

			return client; 
        }

        internal CSHTML5.Extensions.WebSockets.WebSocket ConnectWebSocket()
        {
            var webSocket = new CSHTML5.Extensions.WebSockets.WebSocket();
            var serviceUri = new Uri(WEBSOCKETSERVICE_URL);
            // fire-and-forget (backwards compatible)
            webSocket.ConnectAsync(serviceUri, new System.Threading.CancellationToken());
            return webSocket;
        }

        // Async connect that will await the underlying ConnectAsync on the shim and
        // return the connected websocket instance. Caller can await this to ensure
        // connection is established before registering handlers.
        internal async Task<CSHTML5.Extensions.WebSockets.ClientWebSocket> ConnectWebSocketAsync(CancellationToken cancellationToken = default)
        {
            var webSocket = new CSHTML5.Extensions.WebSockets.ClientWebSocket();
            var serviceUri = new Uri(WEBSOCKETSERVICE_URL);
            await webSocket.ConnectAsync(serviceUri, cancellationToken).ConfigureAwait(false);
            return webSocket;
        }

    }
    public class MyWebSocketClient
    {
        private System.Net.WebSockets.ClientWebSocket _socket;
        private CancellationTokenSource _cts;

        // Ecco i tuoi vecchi eventi!
        public event Action<string> OnMessageReceived;
        public event Action OnConnected;
        public event Action<string> OnError;

        public async Task ConnectAsync(string url)
        {
            _socket = new System.Net.WebSockets.ClientWebSocket();
            _cts = new CancellationTokenSource();

            try
            {
                await _socket.ConnectAsync(new Uri(url), _cts.Token);
                OnConnected?.Invoke(); // Scatena l'evento di connessione

                // Fai partire il loop di ascolto in background
                _ = ReceiveLoop();
            }
            catch (Exception ex) { OnError?.Invoke(ex.Message); }
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[1024 * 4];
            while (_socket.State == System.Net.WebSockets.WebSocketState.Open)
            {
                var result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    // Ecco il tuo vecchio OnMessage!
                    OnMessageReceived?.Invoke(message);
                }
            }
        }

        public async Task SendAsync(string message)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            await _socket.SendAsync(new ArraySegment<byte>(bytes), System.Net.WebSockets.WebSocketMessageType.Text, true, _cts.Token);
        }
    }
}
