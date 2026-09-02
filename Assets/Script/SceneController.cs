using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Loads a scene by its name in Build Settings.
    /// </summary>
    public void LoadSceneByName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[SceneController] Scene name is empty!");
            return;
        }
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Quits the application (works in standalone builds and stops playmode in editor).
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[SceneController] Quitting game...");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
