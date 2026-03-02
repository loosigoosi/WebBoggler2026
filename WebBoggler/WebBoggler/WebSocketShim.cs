using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;

namespace CSHTML5.Extensions.WebSockets
{
    // Minimal compatibility types used by the existing Desk handlers
    public class OnMessageEventArgs : EventArgs
    {
        public object Data { get; set; }
    }

    public class OnErrorEventArgs : EventArgs
    {
        public string Message { get; set; }
    }

    // A small shim that exposes the same surface the old CSHTML5 websocket client
    // expected: events OnOpen, OnMessage, OnClose, OnError and a Send(string) method.
    // Internally uses System.Net.WebSockets.ClientWebSocket for platforms that support it.
    public class ClientWebSocket
    {
        private System.Net.WebSockets.ClientWebSocket _inner;
        private CancellationTokenSource _cts;
        private readonly SynchronizationContext _syncContext;

        public event EventHandler OnOpen;
        public event EventHandler<OnMessageEventArgs> OnMessage;
        public event EventHandler OnClose;
        public event EventHandler<OnErrorEventArgs> OnError;

        public ClientWebSocket()
        {
            // Capture the sync context from the creating thread (UI thread when created in UI)
            _syncContext = SynchronizationContext.Current;
        }

        // Connect with simple retry/backoff and marshal events back to the captured SynchronizationContext
        public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken, int maxAttempts = 3)
        {
            int attempt = 0;
            Exception lastEx = null;

            while (attempt < maxAttempts && !cancellationToken.IsCancellationRequested)
            {
                attempt++;
                try
                {
                    _inner = new System.Net.WebSockets.ClientWebSocket();
                    _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    await _inner.ConnectAsync(uri, _cts.Token).ConfigureAwait(false);
                    PostInvoke(() => OnOpen?.Invoke(this, EventArgs.Empty));
                    _ = Task.Run(ReceiveLoop);
                    return;
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                    // small backoff
                    await Task.Delay(500 * attempt).ConfigureAwait(false);
                }
            }

            // failed all attempts
            PostInvoke(() => OnError?.Invoke(this, new OnErrorEventArgs { Message = lastEx?.Message ?? "Could not connect" }));
        }

        // Backwards-compatible signature used elsewhere
        public Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
            => ConnectAsync(uri, cancellationToken, 3);

        private void PostInvoke(Action a)
        {
            if (_syncContext != null)
            {
                try { _syncContext.Post(_ => a(), null); }
                catch { a(); }
            }
            else
            {
                try { a(); } catch { }
            }
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[4096];
            try
            {
                while (_inner != null && _inner.State == WebSocketState.Open)
                {
                    var seg = new ArraySegment<byte>(buffer);
                    var result = await _inner.ReceiveAsync(seg, _cts.Token).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        try { await _inner.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None).ConfigureAwait(false); } catch { }
                        PostInvoke(() => OnClose?.Invoke(this, EventArgs.Empty));
                        break;
                    }

                    int count = result.Count;
                    // handle fragmented messages
                    while (!result.EndOfMessage)
                    {
                        if (count >= buffer.Length)
                        {
                            // enlarge buffer
                            Array.Resize(ref buffer, buffer.Length * 2);
                        }
                        seg = new ArraySegment<byte>(buffer, count, buffer.Length - count);
                        result = await _inner.ReceiveAsync(seg, _cts.Token).ConfigureAwait(false);
                        count += result.Count;
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, count);
                    PostInvoke(() => OnMessage?.Invoke(this, new OnMessageEventArgs { Data = message }));
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                PostInvoke(() => OnError?.Invoke(this, new OnErrorEventArgs { Message = ex.Message }));
            }
            finally
            {
                PostInvoke(() => OnClose?.Invoke(this, EventArgs.Empty));
            }
        }

        // Compatible send method used throughout the code
        public void Send(string message)
        {
            // fire-and-forget
            _ = SendAsync(message);
        }

        public async Task SendAsync(string message)
        {
            if (_inner == null || _inner.State != WebSocketState.Open) return;
            var bytes = Encoding.UTF8.GetBytes(message);
            var seg = new ArraySegment<byte>(bytes);
            try
            {
                await _inner.SendAsync(seg, WebSocketMessageType.Text, true, _cts.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                PostInvoke(() => OnError?.Invoke(this, new OnErrorEventArgs { Message = ex.Message }));
            }
        }

        // allow graceful close if needed
        public async Task CloseAsync()
        {
            try
            {
                _cts?.Cancel();
                if (_inner != null && (_inner.State == WebSocketState.Open || _inner.State == WebSocketState.CloseReceived))
                {
                    await _inner.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).ConfigureAwait(false);
                }
            }
            catch { }
            finally
            {
                PostInvoke(() => OnClose?.Invoke(this, EventArgs.Empty));
            }
        }
    }

    // Backwards-compatible alias used by older code: WebSocket
    public class WebSocket : ClientWebSocket
    {
        // No extra members; kept for API compatibility with existing Desk.cs
    }
}
