using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class AudioSettings : MonoBehaviour
{
    private GameObject window, previousSelection;
    private TMP_FontAsset font;
    private Button close;
    private Slider musicSlider, stoneSlider;
    private TMP_Text musicValue, stoneValue;
    private Texture2D roundedTexture;
    private Sprite roundedSprite;
    private static AudioSettings instance;
    public static bool IsOpen => instance != null && instance.window != null && instance.window.activeSelf;

    public static void Open()
    {
        var audio = GameAudio.Instance;
        if (instance == null) instance = audio.gameObject.AddComponent<AudioSettings>();
        instance.Show();
    }
    private void Show()
    {
        if (IsOpen) return;
        previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (window == null) Build();
        musicSlider.SetValueWithoutNotify(GameAudio.Instance.MusicVolume);
        stoneSlider.SetValueWithoutNotify(GameAudio.Instance.StoneVolume);
        RefreshValues();
        window.SetActive(true);
        close.Select();
    }
    public void Close()
    {
        if (window == null || !window.activeSelf) return;
        GameAudio.Instance.Save();
        window.SetActive(false);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(previousSelection);
    }
    private void Update() { if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Close(); }
    private void OnDestroy()
    {
        if (instance == this) instance = null;
        if (roundedSprite != null) Destroy(roundedSprite);
        if (roundedTexture != null) Destroy(roundedTexture);
    }
    private void RefreshValues()
    {
        musicValue.text = Mathf.RoundToInt(musicSlider.value * 100f) + "%";
        stoneValue.text = Mathf.RoundToInt(stoneSlider.value * 100f) + "%";
    }
    private void Build()
    {
        var assets = Resources.Load<MultiplayerMenuAssets>("MultiplayerMenuAssets");
        font = assets != null ? assets.Font : TMP_Settings.defaultFontAsset;
        window = new GameObject("AudioSettingsWindow", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        window.transform.SetParent(transform, false);
        var canvas = window.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 500;
        var scaler = window.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
        var dim = Image("Backdrop", window.transform, Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0.75f));
        dim.rectTransform.anchorMin = Vector2.zero; dim.rectTransform.anchorMax = Vector2.one;
        dim.rectTransform.offsetMin = dim.rectTransform.offsetMax = Vector2.zero;
        var outside = dim.gameObject.AddComponent<Button>(); outside.transition = Selectable.Transition.None; outside.onClick.AddListener(Close);
        var panel = Image("Panel", window.transform, Vector2.zero, new Vector2(680, 440), new Color(0.16f, 0.1f, 0.06f));
        RoundCorners(panel);
        Label("Title", panel.transform, new Vector2(0, 160), new Vector2(560, 60), "소리 설정", 34);
        musicSlider = Row(panel.transform, "Music", "배경음악", 65, out musicValue);
        stoneSlider = Row(panel.transform, "Stone", "돌 놓기 효과음", -35, out stoneValue);
        musicSlider.onValueChanged.AddListener(value => { GameAudio.Instance.SetMusicVolume(value); RefreshValues(); });
        stoneSlider.onValueChanged.AddListener(value => { GameAudio.Instance.SetStoneVolume(value); RefreshValues(); });
        Button("Preview", panel.transform, new Vector2(-145, -150), "효과음 듣기", () => GameAudio.Instance.PlayStone());
        close = Button("Close", panel.transform, new Vector2(145, -150), "닫기", Close);
        window.SetActive(false);
    }
    private Slider Row(Transform parent, string name, string label, float y, out TMP_Text value)
    {
        Label(name + "Label", parent, new Vector2(-170, y + 27), new Vector2(260, 40), label, 23);
        value = Label(name + "Value", parent, new Vector2(245, y + 27), new Vector2(90, 40), "", 23);
        var track = Image(name + "Slider", parent, new Vector2(0, y - 14), new Vector2(560, 18), new Color(0.35f, 0.27f, 0.18f));
        var fill = Image("Fill", track.transform, Vector2.zero, Vector2.zero, new Color(0.83f, 0.62f, 0.28f));
        fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
        var handle = Image("Handle", track.transform, Vector2.zero, new Vector2(24, 36), new Color(1f, 0.91f, 0.68f));
        var slider = track.gameObject.AddComponent<Slider>();
        slider.minValue = 0; slider.maxValue = 1;
        slider.fillRect = fill.rectTransform; slider.handleRect = handle.rectTransform; slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }
    private static Image Image(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        image.rectTransform.sizeDelta = size; image.rectTransform.anchoredPosition = position;
        image.color = color; return image;
    }
    private TMP_Text Label(string name, Transform parent, Vector2 position, Vector2 size, string text, float fontSize)
    {
        var label = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        label.transform.SetParent(parent, false); label.rectTransform.sizeDelta = size; label.rectTransform.anchoredPosition = position;
        label.font = font; label.fontSize = fontSize; label.text = text; label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, 0.92f, 0.76f); label.raycastTarget = false; return label;
    }
    private Button Button(string name, Transform parent, Vector2 position, string label, UnityEngine.Events.UnityAction action)
    {
        var image = Image(name, parent, position, new Vector2(240, 52), new Color(0.38f, 0.26f, 0.13f));
        RoundCorners(image);
        image.pixelsPerUnitMultiplier = 1.5f;
        var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(action);
        Label("Label", image.transform, Vector2.zero, new Vector2(230, 48), label, 23);
        return button;
    }

    private void RoundCorners(Image image)
    {
        if (roundedSprite == null)
        {
            const int size = 64;
            const float radius = 20f;
            roundedTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            roundedTexture.name = "Settings rounded corners";
            roundedTexture.wrapMode = TextureWrapMode.Clamp;
            roundedTexture.filterMode = FilterMode.Bilinear;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(radius - (x + 0.5f), x + 0.5f - (size - radius), 0f);
                    float dy = Mathf.Max(radius - (y + 0.5f), y + 0.5f - (size - radius), 0f);
                    float alpha = Mathf.Clamp01(radius + 0.5f - Mathf.Sqrt(dx * dx + dy * dy));
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            roundedTexture.SetPixels(pixels);
            roundedTexture.Apply(false, true);
            roundedSprite = Sprite.Create(roundedTexture, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
        }
        image.sprite = roundedSprite;
        image.type = UnityEngine.UI.Image.Type.Sliced;
    }
}
