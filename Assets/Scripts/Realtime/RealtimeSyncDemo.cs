using System.Collections.Generic;
using UnityEngine;

namespace EL4S.Realtime
{
    [RequireComponent(typeof(RealtimeConnection))]
    public class RealtimeSyncDemo : MonoBehaviour
    {
        [SerializeField] private Transform localAvatar;
        [SerializeField] private GameObject peerAvatarPrefab;
        [SerializeField] private float sendInterval = 0.1f;

        private RealtimeConnection _connection;
        private readonly Dictionary<string, Transform> _peerAvatars = new Dictionary<string, Transform>();
        private float _sendTimer;

        private void Awake()
        {
            _connection = GetComponent<RealtimeConnection>();
            _connection.Joined += OnJoined;
            _connection.PeerJoined += OnPeerJoined;
            _connection.PeerLeft += OnPeerLeft;
            _connection.PeerStateReceived += OnPeerState;
        }

        private void OnDestroy()
        {
            if (_connection == null) return;
            _connection.Joined -= OnJoined;
            _connection.PeerJoined -= OnPeerJoined;
            _connection.PeerLeft -= OnPeerLeft;
            _connection.PeerStateReceived -= OnPeerState;
        }

        private void OnJoined(string clientId, string[] existingPeers)
        {
            foreach (var peerId in existingPeers)
            {
                OnPeerJoined(peerId);
            }
        }

        private void OnPeerJoined(string peerId)
        {
            if (_peerAvatars.ContainsKey(peerId) || peerAvatarPrefab == null) return;

            var avatar = Instantiate(peerAvatarPrefab);
            avatar.name = $"Peer_{peerId}";
            _peerAvatars[peerId] = avatar.transform;
        }

        private void OnPeerLeft(string peerId)
        {
            if (!_peerAvatars.TryGetValue(peerId, out var avatar)) return;

            Destroy(avatar.gameObject);
            _peerAvatars.Remove(peerId);
        }

        private void OnPeerState(string peerId, PeerState state)
        {
            if (!_peerAvatars.TryGetValue(peerId, out var avatar))
            {
                OnPeerJoined(peerId);
                if (!_peerAvatars.TryGetValue(peerId, out avatar)) return;
            }

            avatar.position = new Vector3(state.x, state.y, state.z);
            avatar.rotation = Quaternion.Euler(0f, state.rotY, 0f);
        }

        private void Update()
        {
            if (localAvatar == null || !_connection.IsConnected) return;

            _sendTimer += Time.deltaTime;
            if (_sendTimer < sendInterval) return;
            _sendTimer = 0f;

            _connection.SendState(new PeerState
            {
                x = localAvatar.position.x,
                y = localAvatar.position.y,
                z = localAvatar.position.z,
                rotY = localAvatar.eulerAngles.y,
            });
        }
    }
}
