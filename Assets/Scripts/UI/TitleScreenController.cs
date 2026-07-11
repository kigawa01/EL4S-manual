using EL4S.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EL4S.UI
{
    public class TitleScreenController : MonoBehaviour
    {
        [SerializeField] private RealtimeConnection connection;
        [SerializeField] private Button matchButton;
        [SerializeField] private Text statusText;
        [SerializeField] private string firstPlayerScene = "Player1";
        [SerializeField] private string secondPlayerScene = "Player2";

        private void Awake()
        {
            if (RealtimeConnection.Instance != null)
            {
                connection = RealtimeConnection.Instance;
            }

            matchButton.onClick.AddListener(OnMatchClicked);
            connection.Joined += OnJoined;
            connection.PeerJoined += OnPeerJoined;
            connection.ConnectionFailed += OnConnectionFailed;
        }

        private void OnDestroy()
        {
            matchButton.onClick.RemoveListener(OnMatchClicked);
            connection.Joined -= OnJoined;
            connection.PeerJoined -= OnPeerJoined;
            connection.ConnectionFailed -= OnConnectionFailed;
        }

        private void OnMatchClicked()
        {
            matchButton.interactable = false;
            statusText.text = "マッチング相手を探しています...";
            connection.AutoMatch();
        }

        private void OnJoined(string clientId, string[] existingPeers)
        {
            if (existingPeers.Length == 0)
            {
                statusText.text = "マッチング相手を探しています...";
                return;
            }

            statusText.text = "相手が見つかりました";
            SceneManager.LoadScene(secondPlayerScene);
        }

        private void OnPeerJoined(string peerId)
        {
            statusText.text = "相手が見つかりました";
            SceneManager.LoadScene(firstPlayerScene);
        }

        private void OnConnectionFailed(string message)
        {
            matchButton.interactable = true;
            statusText.text = message == "room is full"
                ? "このルームは満員です"
                : $"接続エラー: {message}";
        }
    }
}
