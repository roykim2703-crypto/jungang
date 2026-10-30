using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class HomeMenu : MonoBehaviour
{
    [SerializeField] private string gameScenePath = "Assets/Scenes/Game.unity";
    [SerializeField] private UnityEvent settingsRequested = new UnityEvent();
    [SerializeField] private UnityEvent howToPlayRequested = new UnityEvent();
    private bool loading;

    public void StartGame()
    {
        MultiplayerMenu.Open();
    }

    public void StartLocalGame()
    {
        if (loading) return;
        if (!Application.CanStreamedLevelBeLoaded(gameScenePath))
        {
            Debug.LogError("Game scene is missing from the build scene list.", this);
            return;
        }
        loading = true;
        Time.timeScale = 1f;
        SceneManager.LoadSceneAsync(gameScenePath);
    }

    public void OpenSettings()
    {
        Debug.Log("HomeMenu.OpenSettings called (settings UI pending).", this);
        settingsRequested.Invoke();
    }

    public void OpenHowToPlay()
    {
        Debug.Log("HomeMenu.OpenHowToPlay called (instructions UI pending).", this);
        howToPlayRequested.Invoke();
    }
}
