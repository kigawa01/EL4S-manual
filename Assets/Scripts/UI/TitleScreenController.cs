using EL4S.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EL4S.UI
{
    public class TitleScreenController : MonoBehaviour
    {
        [SerializeField] private RealtimeConnection connection;
        [SerializeField] private InputField roomCodeInput;
        [SerializeField] private Button joinButton;
        [SerializeField] private Text statusText;
        [SerializeField] private string firstPlayerScene = "Player1";
        [SerializeField] private string secondPlayerScene = "Player2";

        private void Awake()
        {
            if (RealtimeConnection.Instance != null)
            {
                connection = RealtimeConnection.Instance;
            }

            joinButton.onClick.AddListener(OnJoinClicked);
            connection.Joined += OnJoined;
            connection.ConnectionFailed += OnConnectionFailed;
        }

        private void OnDestroy()
        {
            joinButton.onClick.RemoveListener(OnJoinClicked);
            connection.Joined -= OnJoined;
            connection.ConnectionFailed -= OnConnectionFailed;
        }

        private void OnJoinClicked()
        {
            var roomCode = roomCodeInput.text.Trim();
            if (string.IsNullOrEmpty(roomCode))
            {
                statusText.text = "合言葉を入力してください";
                return;
            }

            joinButton.interactable = false;
            statusText.text = "接続中...";
            connection.Connect(roomCode);
        }

        private void OnJoined(string clientId, string[] existingPeers)
        {
            if (existingPeers.Length >= 2)
            {
                statusText.text = "このルームは満員です";
                joinButton.interactable = true;
                connection.Disconnect();
                return;
            }

            var nextScene = existingPeers.Length == 0 ? firstPlayerScene : secondPlayerScene;
            statusText.text = existingPeers.Length == 0
                ? "参加しました。相手を待っています..."
                : "相手が見つかりました";
            SceneManager.LoadScene(nextScene);
        }

        private void OnConnectionFailed(string message)
        {
            joinButton.interactable = true;
            statusText.text = $"接続エラー: {message}";
        }
    }
}
