using UnityEngine;
using UnityEngine.SceneManagement;

namespace EL4S.UI
{
    public class TitleScreenController : MonoBehaviour
    {
        [SerializeField] private string gameSceneName = "Player1";

        public void StartGame()
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }
}
