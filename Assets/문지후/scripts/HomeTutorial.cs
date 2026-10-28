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
    private TMP_Text description;
    private TMP_Text heading;
    private Button previous;
    private Button next;
    private Button close;
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
        close.Select();
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
        heading.text = count > 0 ? $"{pageIndex + 1} / {count}  ·  {page?.title}" : "게임 방법";
        description.text = page != null ? page.description : "인스펙터에서 튜토리얼 장면을 추가해 주세요.";
        previous.interactable = pageIndex > 0;
        next.interactable = pageIndex + 1 < count;
        Sprite[] frames = page?.cutscenes;
        int first = FindFrame(frames, -1);
        artwork.sprite = first >= 0 ? frames[first] : null;
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
            artwork.sprite = frames[current];
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
        var panel = new GameObject("Frame", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        panel.transform.SetParent(dim.transform, false);
        var rect = (RectTransform)panel.transform;
        rect.sizeDelta = new Vector2(1200f, 800f);
        rect.anchoredPosition = new Vector2(0f, 20f);
        panel.GetComponent<RawImage>().texture = frameImage;
        panel.GetComponent<RawImage>().raycastTarget = false;
        var viewport = MakeImage("CutsceneViewport", rect, new Color(0.035f, 0.025f, 0.02f));
        Place(viewport.rectTransform, new Vector2(0.024f, 0.25f), new Vector2(0.976f, 0.965f));
        viewport.gameObject.AddComponent<RectMask2D>();
        artwork = MakeImage("Cutscene", viewport.transform, Color.white);
        Place(artwork.rectTransform, Vector2.zero, Vector2.one);
        artwork.preserveAspect = true;
        whiteFade = MakeImage("WhiteFade", viewport.transform, new Color(1f, 1f, 1f, 0f));
        Place(whiteFade.rectTransform, Vector2.zero, Vector2.one);
        // Frame above the picture preserves the pixel corners around the opening.
        var border = new GameObject("FrameBorder", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        border.transform.SetParent(rect, false);
        Place((RectTransform)border.transform, Vector2.zero, Vector2.one);
        border.GetComponent<RawImage>().texture = frameImage;
        border.GetComponent<RawImage>().raycastTarget = false;
        description = MakeText("Description", rect, 28f, new Color(0.18f, 0.12f, 0.07f));
        Place(description.rectTransform, new Vector2(0.06f, 0.035f), new Vector2(0.94f, 0.235f));
        description.alignment = TextAlignmentOptions.Center;
        heading = MakeText("PageTitle", dim.transform, 27f, new Color(1f, 0.92f, 0.77f));
        heading.rectTransform.sizeDelta = new Vector2(1000f, 55f);
        heading.rectTransform.anchoredPosition = new Vector2(0f, 465f);
        previous = MakeButton("Previous", "이전", dim.transform, new Vector2(-260f, -440f), PreviousPage);
        next = MakeButton("Next", "다음", dim.transform, new Vector2(260f, -440f), NextPage);
        close = MakeButton("Close", "닫기", dim.transform, new Vector2(0f, -440f), Close);
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

    private TMP_Text MakeText(string name, Transform parent, float size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = size;
        text.enableAutoSizing = true;
        text.fontSizeMin = 16f;
        text.fontSizeMax = size;
        text.color = color;
        text.richText = false;
        text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.Center;
        return text;
    }

    private Button MakeButton(string name, string label, Transform parent, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        var image = MakeImage(name, parent, new Color(0.35f, 0.24f, 0.13f));
        image.rectTransform.sizeDelta = new Vector2(200f, 56f);
        image.rectTransform.anchoredPosition = position;
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        var text = MakeText("Label", image.transform, 26f, new Color(1f, 0.92f, 0.77f));
        Place(text.rectTransform, Vector2.zero, Vector2.one);
        text.text = label;
        return button;
    }

    private static void Place(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
