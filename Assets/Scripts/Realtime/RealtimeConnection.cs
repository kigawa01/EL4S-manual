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
        [SerializeField] private string roomId = "default";

        public event Action<string, string[]> Joined;
        public event Action<string> PeerJoined;
        public event Action<string> PeerLeft;
        public event Action<string, PeerState> PeerStateReceived;

        public string ClientId { get; private set; }
        public bool IsConnected => _socket != null && _socket.State == WebSocketState.Open;

        private ClientWebSocket _socket;
        private CancellationTokenSource _cts;
        private readonly ConcurrentQueue<string> _incoming = new ConcurrentQueue<string>();
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

        private async void Start()
        {
            ClientId = Guid.NewGuid().ToString("N");
            _cts = new CancellationTokenSource();
            _socket = new ClientWebSocket();

            try
            {
                await _socket.ConnectAsync(new Uri(serverUrl), _cts.Token);
                await SendJson(new JoinMessage { type = "join", roomId = roomId, clientId = ClientId });
                _ = ReceiveLoop(_cts.Token);
            }
            catch (Exception e)
            {
                Debug.LogError($"[RealtimeConnection] connect failed: {e}");
            }
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            var buffer = new byte[8192];
            var messageBuilder = new StringBuilder();

            try
            {
                while (_socket.State == WebSocketState.Open && !token.IsCancellationRequested)
                {
                    var result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", token);
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
            var type = JsonUtility.FromJson<TypeOnly>(json)?.type;
            switch (type)
            {
                case "joined":
                    var joined = JsonUtility.FromJson<JoinedMessage>(json);
                    Joined?.Invoke(joined.clientId, joined.peers ?? Array.Empty<string>());
                    break;
                case "peer-joined":
                    var peerJoined = JsonUtility.FromJson<PeerJoinedMessage>(json);
                    PeerJoined?.Invoke(peerJoined.clientId);
                    break;
                case "peer-left":
                    var peerLeft = JsonUtility.FromJson<PeerLeftMessage>(json);
                    PeerLeft?.Invoke(peerLeft.clientId);
                    break;
                case "state":
                    var state = JsonUtility.FromJson<StateOutMessage>(json);
                    PeerStateReceived?.Invoke(state.clientId, state.payload);
                    break;
                case "error":
                    var error = JsonUtility.FromJson<ErrorMessage>(json);
                    Debug.LogWarning($"[RealtimeConnection] server error: {error.message}");
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

        [Serializable] private class TypeOnly { public string type; }
        [Serializable] private class JoinMessage { public string type; public string roomId; public string clientId; }
        [Serializable] private class JoinedMessage { public string type; public string clientId; public string[] peers; }
        [Serializable] private class PeerJoinedMessage { public string type; public string clientId; }
        [Serializable] private class PeerLeftMessage { public string type; public string clientId; }
        [Serializable] private class StateInMessage { public string type; public PeerState payload; }
        [Serializable] private class StateOutMessage { public string type; public string clientId; public PeerState payload; }
        [Serializable] private class ErrorMessage { public string type; public string message; }
    }
}
