using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Scene-owned review navigation, separate from the match's gameplay HUD.</summary>
public sealed class MatchReviewToolbar : MonoBehaviour
{
    private AugmentMatch match;
    private TMP_FontAsset font;
    private TMP_Text current, counter;
    private Slider timeline;
    private Button first, previous, next, last;
    private CanvasGroup group;
    private int shownIndex = -2, shownCount = -1;

    public void Initialize(AugmentMatch owner)
    {
        if (match != null) return;
        match = owner;
        foreach (var rect in FindObjectsByType<RectTransform>(FindObjectsInactive.Include))
        {
            if (rect.name == "상단 배젤" || rect.name == "항복 버튼") rect.gameObject.SetActive(false);
            var reward = rect.GetComponent<Scrollbar>();
            if (reward != null && reward.direction == Scrollbar.Direction.LeftToRight) rect.gameObject.SetActive(false);
        }
        var assets = Resources.Load<MultiplayerMenuAssets>("MultiplayerMenuAssets");
        font = assets != null ? assets.Font : TMP_Settings.defaultFontAsset;
        var root = new GameObject("ReviewToolbar", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        root.GetComponent<Canvas>().sortingOrder = 260;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = .5f;
        var panel = Image("ReviewPanel", root.transform, Vector2.zero, new Vector2(1140, 96), new Color(.14f, .10f, .065f, .98f));
        panel.rectTransform.anchorMin = panel.rectTransform.anchorMax = new Vector2(.5f, 1f);
        panel.rectTransform.pivot = new Vector2(.5f, 1f);
        panel.rectTransform.anchoredPosition = new Vector2(0, -10);
        RoundedUI.Apply(panel, 20f);
        var shadow = panel.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, .22f); shadow.effectDistance = new Vector2(0, -4);
        Label("Title", panel.transform, new Vector2(-476, -22), new Vector2(140, 30), "대국 복기", 25, new Color(1, .83f, .48f));
        current = Label("CurrentRecord", panel.transform, new Vector2(-72, -22), new Vector2(640, 30), "", 19, new Color(1, .94f, .80f));
        current.alignment = TextAlignmentOptions.MidlineLeft;
        current.overflowMode = TextOverflowModes.Ellipsis;
        Button("Result", panel.transform, new Vector2(350, -23), new Vector2(116, 32), "결과 화면", () => SceneManager.LoadSceneAsync(MatchReviewSession.ResultScene));
        Button("Home", panel.transform, new Vector2(482, -23), new Vector2(116, 32), "메인 화면", owner.ReturnFromReview);
        first = Button("First", panel.transform, new Vector2(-503, -68), new Vector2(84, 34), "처음", () => Jump(0));
        previous = Button("Previous", panel.transform, new Vector2(-407, -68), new Vector2(84, 34), "이전", () => Jump(match.ReviewIndex - 1));
        next = Button("Next", panel.transform, new Vector2(407, -68), new Vector2(84, 34), "다음", () => Jump(match.ReviewIndex + 1));
        last = Button("Last", panel.transform, new Vector2(503, -68), new Vector2(84, 34), "마지막", () => Jump(MatchReviewSession.Frames.Count - 1));
        // A generous invisible hit area keeps the slim timeline easy to click and drag.
        var track = Image("Timeline", panel.transform, new Vector2(-45, -68), new Vector2(560, 34), Color.clear);
        track.rectTransform.anchorMin = track.rectTransform.anchorMax = new Vector2(.5f, 1f);
        track.rectTransform.anchoredPosition = new Vector2(-45, -68);
        var rail = Image("Rail", track.transform, Vector2.zero, new Vector2(536, 4), new Color(.36f, .31f, .23f));
        rail.raycastTarget = false;
        RoundedUI.Apply(rail, 2f);
        var fill = Image("Progress", rail.transform, Vector2.zero, Vector2.zero, new Color(.88f, .69f, .38f));
        fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
        fill.raycastTarget = false;
        RoundedUI.Apply(fill, 2f);
        var handle = Image("Handle", rail.transform, Vector2.zero, new Vector2(16, 16), new Color(1, .92f, .75f));
        handle.raycastTarget = false;
        RoundedUI.Apply(handle, 8f);
        var thumbShadow = handle.gameObject.AddComponent<Shadow>();
        thumbShadow.effectColor = new Color(0, 0, 0, .28f);
        thumbShadow.effectDistance = new Vector2(0, -2);
        var center = Image("ThumbCenter", handle.transform, Vector2.zero, new Vector2(6, 6), new Color(.65f, .44f, .19f));
        center.raycastTarget = false;
        RoundedUI.Apply(center, 3f);
        timeline = track.gameObject.AddComponent<Slider>();
        timeline.wholeNumbers = true; timeline.minValue = 0;
        timeline.fillRect = fill.rectTransform; timeline.handleRect = handle.rectTransform; timeline.targetGraphic = handle;
        var sliderColors = timeline.colors;
        sliderColors.highlightedColor = new Color(1f, .96f, .84f);
        sliderColors.pressedColor = new Color(.94f, .76f, .46f);
        sliderColors.disabledColor = new Color(.6f, .57f, .5f, .5f);
        sliderColors.fadeDuration = .12f;
        timeline.colors = sliderColors;
        timeline.onValueChanged.AddListener(value => Jump(Mathf.RoundToInt(value)));
        counter = Label("Counter", panel.transform, new Vector2(302, -68), new Vector2(110, 30), "", 18, new Color(1, .86f, .61f));
        TitleFadeIn.ConfigurePopup(root, panel.rectTransform);
        group = root.GetComponent<CanvasGroup>();
        Refresh();
    }

    private void Update()
    {
        if (match == null || !match.IsReviewMode) return;
        if (shownIndex != match.ReviewIndex || shownCount != MatchReviewSession.Frames.Count) Refresh();
        if (AudioSettings.IsOpen || group == null || !group.interactable) return;
        var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (selected != null && (selected.GetComponent<TMP_InputField>() != null || selected.GetComponent<Slider>() != null)) return;
        if (Input.GetKeyDown(KeyCode.LeftArrow)) Jump(match.ReviewIndex - 1);
        else if (Input.GetKeyDown(KeyCode.RightArrow)) Jump(match.ReviewIndex + 1);
        else if (Input.GetKeyDown(KeyCode.Home)) Jump(0);
        else if (Input.GetKeyDown(KeyCode.End)) Jump(MatchReviewSession.Frames.Count - 1);
    }

    private void Jump(int index)
    {
        int count = MatchReviewSession.Frames.Count;
        if (count == 0) return;
        index = Mathf.Clamp(index, 0, count - 1);
        if (index != match.ReviewIndex) match.ShowReviewMove(index);
        Refresh();
    }

    private void Refresh()
    {
        int count = MatchReviewSession.Frames.Count, index = match.ReviewIndex;
        shownIndex = index; shownCount = count;
        current.text = index >= 0 && index < count ? MatchReviewSession.Frames[index].label : "저장된 기록이 없습니다";
        counter.text = count == 0 ? "0 / 0" : $"{index + 1} / {count}";
        timeline.maxValue = Mathf.Max(1, count - 1);
        timeline.SetValueWithoutNotify(Mathf.Max(0, index));
        timeline.interactable = count > 1;
        first.interactable = previous.interactable = index > 0;
        last.interactable = next.interactable = count > 0 && index < count - 1;
    }

    private static Image Image(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        image.rectTransform.anchoredPosition = position; image.rectTransform.sizeDelta = size;
        image.color = color; return image;
    }
    private TMP_Text Label(string name, Transform parent, Vector2 position, Vector2 size, string text, float fontSize, Color color)
    {
        var label = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        label.transform.SetParent(parent, false);
        label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(.5f, 1f);
        label.rectTransform.anchoredPosition = position; label.rectTransform.sizeDelta = size;
        label.text = text; label.font = font; label.fontSize = fontSize; label.color = color;
        label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap; return label;
    }
    private Button Button(string name, Transform parent, Vector2 position, Vector2 size, string text, UnityEngine.Events.UnityAction action)
    {
        var image = Image(name, parent, position, size, new Color(.34f, .25f, .14f));
        image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(.5f, 1f);
        image.rectTransform.anchoredPosition = position;
        RoundedUI.Apply(image, 10f);
        var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(action);
        var colors = button.colors; colors.highlightedColor = new Color(1, .88f, .64f); colors.pressedColor = new Color(.68f, .58f, .40f); colors.disabledColor = new Color(.55f, .55f, .55f, .45f); button.colors = colors;
        var label = Label("Label", image.transform, Vector2.zero, size, text, 17, new Color(1, .93f, .78f));
        label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(.5f, .5f);
        return button;
    }
}
