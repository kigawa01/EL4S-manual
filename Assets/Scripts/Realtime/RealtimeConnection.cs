using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace EL4S.Realtime
{
    // Talks to the el4s-realtime WebSocket relay (Server/). Protocol mirrors
    // Server/src/protocol.ts: join / joined / peer-joined / peer-left / state / error.
    public class RealtimeConnection : MonoBehaviour
    {
        [SerializeField] private string serverUrl = "wss://el4s-realtime.kigawa.net/ws";

        public event Action<string, string[]> Joined;
        public event Action<string> PeerJoined;
        public event Action<string> PeerLeft;
        public event Action<string, PeerState> PeerStateReceived;
        public event Action<string> ConnectionFailed;

        public string ClientId { get; private set; }
        public bool IsConnected => _socket != null && _socket.State == WebSocketState.Open;

        private ClientWebSocket _socket;
        private CancellationTokenSource _cts;
        private readonly ConcurrentQueue<string> _incoming = new ConcurrentQueue<string>();
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

        private static RealtimeConnection _instance;
        public static RealtimeConnection Instance => _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public async void Connect(string targetRoomId)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _socket?.Dispose();

            ClientId = Guid.NewGuid().ToString("N");
            _cts = new CancellationTokenSource();
            _socket = new ClientWebSocket();
            var socket = _socket;
            var token = _cts.Token;

            try
            {
                await socket.ConnectAsync(new Uri(serverUrl), token);
                await SendJson(new JoinMessage { type = "join", roomId = targetRoomId, clientId = ClientId });
                _ = ReceiveLoop(socket, token);
            }
            catch (Exception e)
            {
                Debug.LogError($"[RealtimeConnection] connect failed: {e}");
                ConnectionFailed?.Invoke(e.Message);
            }
        }

        // Takes the socket/token as parameters (rather than reading the _socket/_cts
        // fields) so a loop started by an earlier Connect() keeps watching its own
        // connection even after a later Connect() call replaces those fields.
        private async Task ReceiveLoop(ClientWebSocket socket, CancellationToken token)
        {
            var buffer = new byte[8192];
            var messageBuilder = new StringBuilder();

            try
            {
                while (socket.State == WebSocketState.Open && !token.IsCancellationRequested)
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", token);
                        break;
                    }

                    messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    if (result.EndOfMessage)
                    {
                        _incoming.Enqueue(messageBuilder.ToString());
                        messageBuilder.Clear();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // expected on disconnect
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[RealtimeConnection] receive loop ended: {e.Message}");
            }
        }

        private void Update()
        {
            while (_incoming.TryDequeue(out var json))
            {
                Dispatch(json);
            }
        }

        private void Dispatch(string json)
        {
            var msg = JsonUtility.FromJson<IncomingMessage>(json);
            switch (msg?.type)
            {
                case "joined":
                    Joined?.Invoke(msg.clientId, msg.peers ?? Array.Empty<string>());
                    break;
                case "peer-joined":
                    PeerJoined?.Invoke(msg.clientId);
                    break;
                case "peer-left":
                    PeerLeft?.Invoke(msg.clientId);
                    break;
                case "state":
                    PeerStateReceived?.Invoke(msg.clientId, msg.payload);
                    break;
                case "error":
                    Debug.LogWarning($"[RealtimeConnection] server error: {msg.message}");
                    ConnectionFailed?.Invoke(msg.message);
                    break;
                default:
                    Debug.LogWarning($"[RealtimeConnection] unknown message: {json}");
                    break;
            }
        }

        public void SendState(PeerState state)
        {
            _ = SendJson(new StateInMessage { type = "state", payload = state });
        }

        public void Disconnect()
        {
            _cts?.Cancel();
            if (_socket != null && _socket.State == WebSocketState.Open)
            {
                _ = CloseQuietly(_socket);
            }
        }

        private static async Task CloseQuietly(ClientWebSocket socket)
        {
            try
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "client disconnect", CancellationToken.None);
            }
            catch
            {
                // socket already gone, nothing to clean up
            }
        }

        private async Task SendJson(object message)
        {
            if (_socket == null || _socket.State != WebSocketState.Open) return;

            var json = JsonUtility.ToJson(message);
            var bytes = Encoding.UTF8.GetBytes(json);

            await _sendLock.WaitAsync();
            try
            {
                await _socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
            }
            finally
            {
                _sendLock.Release();
            }
        }

        private async void OnDestroy()
        {
            _cts?.Cancel();
            if (_socket != null && _socket.State == WebSocketState.Open)
            {
                try
                {
                    await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "destroyed", CancellationToken.None);
                }
                catch
                {
                    // socket already gone, nothing to clean up
                }
            }

            _socket?.Dispose();
        }

        [Serializable] private class JoinMessage { public string type; public string roomId; public string clientId; }
        [Serializable] private class StateInMessage { public string type; public PeerState payload; }

        // Covers every server->client shape (joined/peer-joined/peer-left/state/error) in one
        // parse; JsonUtility leaves fields absent from the JSON at their default value.
        [Serializable]
        private class IncomingMessage
        {
            public string type;
            public string clientId;
            public string[] peers;
            public PeerState payload;
            public string message;
        }
    }
}
