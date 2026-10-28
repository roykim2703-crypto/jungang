using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class HomeMenu : MonoBehaviour
{
    [SerializeField] private string gameScenePath = "Assets/Scenes/Game.unity";
    [SerializeField] private UnityEvent settingsRequested = new UnityEvent();
    [SerializeField] private UnityEvent howToPlayRequested = new UnityEvent();
    [SerializeField] private HomeTutorial tutorial;
    private bool loading;
    private GameObject loadingOverlay;
    private RectTransform loadingFill;
    private Text loadingLabel;

    public void StartGame()
    {
        if (loading) return;
        if (!Application.CanStreamedLevelBeLoaded(gameScenePath))
        {
            Debug.LogError("Game scene is missing from the build scene list.", this);
            return;
        }
        loading = true;
        Time.timeScale = 1f;
        CreateLoadingOverlay();
        StartCoroutine(LoadGame());
    }

    private IEnumerator LoadGame()
    {
        // Let the overlay render before scene loading starts doing work.
        yield return null;
        AsyncOperation operation;
        try
        {
            operation = SceneManager.LoadSceneAsync(gameScenePath);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception, this);
            Destroy(loadingOverlay);
            loading = false;
            yield break;
        }

        if (operation == null)
        {
            Debug.LogError("Could not start loading the game scene.", this);
            Destroy(loadingOverlay);
            loading = false;
            yield break;
        }

        operation.allowSceneActivation = false;
        float elapsed = 0f;
        while (operation.progress < 0.9f || elapsed < 0.35f)
        {
            // Unity reports 0.9 when loading is complete and activation is pending.
            SetLoadingProgress(Mathf.Clamp01(operation.progress / 0.9f));
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        SetLoadingProgress(1f);
        yield return null;
        operation.allowSceneActivation = true;
    }

    private void CreateLoadingOverlay()
    {
        loadingOverlay = new GameObject("LoadingOverlay", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        // Keep the overlay independent of title UI fades and CanvasGroups.
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(loadingOverlay, gameObject.scene);
        var canvas = loadingOverlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = short.MaxValue;
        var scaler = loadingOverlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var background = CreateImage("Background", loadingOverlay.transform, new Color(0.035f, 0.025f, 0.02f, 1f));
        background.rectTransform.anchorMin = Vector2.zero;
        background.rectTransform.anchorMax = Vector2.one;
        background.rectTransform.sizeDelta = Vector2.zero;
        background.raycastTarget = true;

        var track = CreateImage("ProgressTrack", background.transform, new Color(0.2f, 0.17f, 0.13f));
        track.rectTransform.sizeDelta = new Vector2(640f, 24f);
        track.rectTransform.anchoredPosition = new Vector2(0f, -50f);
        var fill = CreateImage("ProgressFill", track.transform, new Color(0.92f, 0.72f, 0.38f));
        loadingFill = fill.rectTransform;
        loadingFill.anchorMin = Vector2.zero;
        loadingFill.anchorMax = new Vector2(0f, 1f);
        loadingFill.offsetMin = loadingFill.offsetMax = Vector2.zero;

        var label = new GameObject("LoadingLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        label.transform.SetParent(background.transform, false);
        loadingLabel = label.GetComponent<Text>();
        loadingLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        loadingLabel.fontSize = 32;
        loadingLabel.alignment = TextAnchor.MiddleCenter;
        loadingLabel.color = new Color(1f, 0.92f, 0.77f);
        loadingLabel.raycastTarget = false;
        loadingLabel.rectTransform.sizeDelta = new Vector2(640f, 60f);
        SetLoadingProgress(0f);
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private void SetLoadingProgress(float progress)
    {
        loadingFill.anchorMax = new Vector2(progress, 1f);
        loadingLabel.text = $"Loading... {Mathf.RoundToInt(progress * 100f)}%";
    }

    public void OpenSettings()
    {
        if (loading) return;
        Debug.Log("HomeMenu.OpenSettings called (settings UI pending).", this);
        settingsRequested.Invoke();
    }

    public void OpenHowToPlay()
    {
        if (loading) return;
        if (tutorial != null) tutorial.Open();
        howToPlayRequested.Invoke();
    }
}
