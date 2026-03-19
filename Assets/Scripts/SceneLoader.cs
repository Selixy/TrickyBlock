using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        SceneManager.LoadScene("Lobby", LoadSceneMode.Additive);
    }

    /// <summary>
    /// Charge la scène Game et décharge la scène Lobby
    /// </summary>
    public void LoadGameScene()
    {
        Debug.Log("[SceneLoader] Chargement de la scène Game et déchargement de Lobby");
        SceneManager.LoadScene("Game", LoadSceneMode.Additive);
        SceneManager.UnloadSceneAsync("Lobby");
    }

    /// <summary>
    /// Charge la scène Lobby et décharge la scène Game
    /// </summary>
    public void LoadLobbyScene()
    {
        Debug.Log("[SceneLoader] Chargement de la scène Lobby et déchargement de Game");
        SceneManager.LoadScene("Lobby", LoadSceneMode.Additive);
        SceneManager.UnloadSceneAsync("Game");
    }
}