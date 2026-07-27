using UnityEngine;
using UnityEngine.SceneManagement;

namespace Wake.Core
{
    [DisallowMultipleComponent]
    public sealed class GameSystemsBootstrap : MonoBehaviour
    {
        [SerializeField] private string lobbySceneName = "Lobby Scene";

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (!SceneManager.GetSceneByName(lobbySceneName).isLoaded)
            {
                SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Additive);
            }
        }
    }
}
