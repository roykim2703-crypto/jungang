using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class HomeTutorial : MonoBehaviour
{
    [Serializable]
    public sealed class TutorialPage
    {
        public string title = "튜토리얼 장면";
        [TextArea(3, 8)] public string description;
        [Tooltip("한 장이면 고정, 여러 장이면 순서대로 반복 재생합니다. 빈 항목은 건너뜁니다.")]
        public Sprite[] cutscenes = new Sprite[0];
    }

    [Header("튜토리얼 내용")]
    [SerializeField] private Texture2D frameImage;
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private TutorialPage[] tutorialPages = new TutorialPage[0];
    [Header("컷씬 전환 시간 (초)")]
    [SerializeField, Min(0.1f)] private float cutsceneInterval = 3f;
    [SerializeField, Min(0.01f)] private float whiteFadeDuration = 0.4f;

    private GameObject window;
    private Image artwork;
    private Image whiteFade;
    private AspectRatioFitter artworkAspect;
    private Button previous;
    private Button next;
    private Button backdrop;
    private GameObject previousSelection;
    private Coroutine playback;
    private int pageIndex;

    public void Open()
    {
        if (window != null && window.activeSelf) return;
        previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (window == null) CreateWindow();
        window.SetActive(true);
        ShowPage(0);
        if (next.interactable) next.Select();
        else backdrop.Select();
    }

    public void Close()
    {
        StopPlayback();
        if (window != null) window.SetActive(false);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(previousSelection);
    }

    public void PreviousPage() { ShowPage(pageIndex - 1); }
    public void NextPage() { ShowPage(pageIndex + 1); }

    private void Update()
    {
        if (window == null || !window.activeSelf) return;
        if (Input.GetKeyDown(KeyCode.Escape)) Close();
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) PreviousPage();
        else if (Input.GetKeyDown(KeyCode.RightArrow)) NextPage();
    }

    private void OnDisable() { Close(); }
    private void OnDestroy() { if (window != null) Destroy(window); }

    private void ShowPage(int index)
    {
        int count = tutorialPages == null ? 0 : tutorialPages.Length;
        pageIndex = Mathf.Clamp(index, 0, Mathf.Max(0, count - 1));
        StopPlayback();
        whiteFade.color = new Color(1f, 1f, 1f, 0f);
        TutorialPage page = count > 0 ? tutorialPages[pageIndex] : null;
        previous.interactable = pageIndex > 0;
        next.interactable = pageIndex + 1 < count;
        previous.gameObject.SetActive(previous.interactable);
        next.gameObject.SetActive(next.interactable);
        Sprite[] frames = page?.cutscenes;
        int first = FindFrame(frames, -1);
        SetArtwork(first >= 0 ? frames[first] : null);
        artwork.enabled = first >= 0;
        if (first >= 0 && FindFrame(frames, first) != first)
            playback = StartCoroutine(PlayCutscenes(frames, first));
    }

    private static int FindFrame(Sprite[] frames, int current)
    {
        if (frames == null || frames.Length == 0) return -1;
        for (int step = 1; step <= frames.Length; step++)
        {
            int index = (current + step) % frames.Length;
            if (frames[index] != null) return index;
        }
        return -1;
    }

    private IEnumerator PlayCutscenes(Sprite[] frames, int current)
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, cutsceneInterval));
            yield return FadeWhite(0f, 1f);
            current = FindFrame(frames, current);
            if (current < 0) yield break;
            SetArtwork(frames[current]);
            yield return FadeWhite(1f, 0f);
        }
    }

    private IEnumerator FadeWhite(float from, float to)
    {
        float duration = Mathf.Max(0.01f, whiteFadeDuration);
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            whiteFade.color = new Color(1f, 1f, 1f, Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }
        whiteFade.color = new Color(1f, 1f, 1f, to);
    }

    private void StopPlayback()
    {
        if (playback != null) StopCoroutine(playback);
        playback = null;
    }

    private void CreateWindow()
    {
        // Independent canvas keeps title fades from affecting the modal.
        window = new GameObject("TutorialWindow", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(window, gameObject.scene);
        var canvas = window.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = window.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        var dim = MakeImage("Dim", window.transform, new Color(0f, 0f, 0f, 0.85f));
        Place(dim.rectTransform, Vector2.zero, Vector2.one);
        dim.raycastTarget = true;
        backdrop = dim.gameObject.AddComponent<Button>();
        backdrop.targetGraphic = dim;
        backdrop.transition = Selectable.Transition.None;
        backdrop.navigation = new Navigation { mode = Navigation.Mode.None };
        backdrop.onClick.AddListener(Close);

        // Picture and arrows are siblings of the backdrop: picture clicks cannot close it.
        var bounds = new GameObject("PictureBounds", typeof(RectTransform));
        bounds.transform.SetParent(window.transform, false);
        Place((RectTransform)bounds.transform, new Vector2(0.12f, 0.08f), new Vector2(0.88f, 0.92f));
        artwork = MakeImage("Cutscene", bounds.transform, Color.white);
        artwork.raycastTarget = true;
        artworkAspect = artwork.gameObject.AddComponent<AspectRatioFitter>();
        artworkAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        artworkAspect.aspectRatio = 1.5f;
        whiteFade = MakeImage("WhiteFade", artwork.transform, new Color(1f, 1f, 1f, 0f));
        Place(whiteFade.rectTransform, Vector2.zero, Vector2.one);
        previous = MakeArrow("Previous", window.transform, 0.055f, false, PreviousPage);
        next = MakeArrow("Next", window.transform, 0.945f, true, NextPage);
    }

    private void SetArtwork(Sprite sprite)
    {
        artwork.sprite = sprite;
        if (sprite != null) artworkAspect.aspectRatio = sprite.rect.width / sprite.rect.height;
    }

    private static Image MakeImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Button MakeArrow(string name, Transform parent, float anchorX, bool pointsRight,
        UnityEngine.Events.UnityAction action)
    {
        var image = MakeImage(name, parent, new Color(0.22f, 0.14f, 0.08f, 0.9f));
        image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(anchorX, 0.5f);
        image.rectTransform.sizeDelta = new Vector2(90f, 100f);
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        // Geometry avoids depending on arrow glyphs in the Korean font atlas.
        for (int i = 0; i < 2; i++)
        {
            var stroke = MakeImage("ArrowStroke", image.transform, new Color(1f, 0.9f, 0.66f));
            stroke.rectTransform.sizeDelta = new Vector2(34f, 8f);
            stroke.rectTransform.anchoredPosition = new Vector2(0f, i == 0 ? 10f : -10f);
            float angle = i == 0 ? -45f : 45f;
            stroke.rectTransform.localRotation = Quaternion.Euler(0f, 0f, pointsRight ? angle : -angle);
        }
        return button;
    }

    private static void Place(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
