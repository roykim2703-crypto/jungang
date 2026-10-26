using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>A compact card expands into a readable card without intercepting clicks.</summary>
public sealed class CardDetails : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private static CardDetails current;
    private TMP_Text sourceTitle;
    private string title, description, grade;
    private Sprite artwork, frame;
    private GameObject overlay;
    private RectTransform preview;
    private CanvasGroup opacity;
    private Vector2 startPosition, endPosition;
    private float progress, startScale, finalScale;

    public void Bind(TMP_Text titleText, string body, string value, Sprite picture, Sprite background)
    {
        bool changed = title != titleText.text || description != body || grade != value || artwork != picture;
        sourceTitle = titleText;
        title = titleText.text; description = body; grade = value; artwork = picture; frame = background;
        if (changed && overlay != null) { Hide(); Show(); }
    }

    public void OnPointerEnter(PointerEventData data) => Show();
    public void OnPointerExit(PointerEventData data) => Hide();
    private void OnDisable() => Hide();
    private void OnDestroy() => Hide();

    private void Show()
    {
        if (sourceTitle == null || overlay != null) return;
        if (current != null && current != this) current.Hide();
        current = this;
        overlay = new GameObject("ExpandedCardPreview", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        Canvas canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 1000;
        opacity = overlay.GetComponent<CanvasGroup>();
        opacity.blocksRaycasts = false; opacity.interactable = false; opacity.alpha = 0;
        preview = MakeRect("Card", overlay.transform);
        preview.sizeDelta = new Vector2(320, 480);
        var face = preview.gameObject.AddComponent<Image>();
        face.sprite = frame; face.color = frame == null ? new Color(1f, .96f, .86f) : Color.white;
        face.raycastTarget = false;
        var shadow = preview.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, .4f); shadow.effectDistance = new Vector2(5, -5);
        TMP_Text heading = MakeText("Title", title, 24, TextAlignmentOptions.Center);
        TMP_Text body = MakeText("Description", description, 18, TextAlignmentOptions.TopLeft);
        TMP_Text value = MakeText("Grade", grade, 20, TextAlignmentOptions.Center);
        float titleHeight = Mathf.Max(42, heading.GetPreferredValues(title, 268, 0).y + 8);
        float bodyHeight = Mathf.Max(96, body.GetPreferredValues(description, 268, 0).y + 12);
        float gradeHeight = Mathf.Max(24, value.GetPreferredValues(grade, 268, 0).y + 8);
        float height = titleHeight + bodyHeight + 194 + gradeHeight;
        preview.sizeDelta = new Vector2(320, height);
        Place(heading.rectTransform, 24, titleHeight, 268);
        var imageRect = MakeRect("Artwork", preview);
        Place(imageRect, 36 + titleHeight, 128, 244);
        Image picture = imageRect.gameObject.AddComponent<Image>();
        picture.sprite = artwork; picture.preserveAspect = true; picture.raycastTarget = false; picture.enabled = artwork != null;
        Place(body.rectTransform, 180 + titleHeight, bodyHeight, 268);
        Place(value.rectTransform, height - gradeHeight - 6, gradeHeight, 268);

        Canvas originalCanvas = GetComponentInParent<Canvas>();
        Camera camera = originalCanvas != null && originalCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? originalCanvas.worldCamera : null;
        var corners = new Vector3[4]; ((RectTransform)transform).GetWorldCorners(corners);
        Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        Vector2 topRight = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
        startPosition = (bottomLeft + topRight) * .5f;
        finalScale = Mathf.Min(1f, (Screen.height - 24f) / height, (Screen.width - 24f) / 320f);
        startScale = Mathf.Min(finalScale, Mathf.Max(.1f, (topRight.y - bottomLeft.y) / height));
        float halfWidth = 160 * finalScale, halfHeight = height * finalScale * .5f;
        float x = startPosition.x > Screen.width * .5f ? bottomLeft.x - halfWidth - 12 : topRight.x + halfWidth + 12;
        endPosition = new Vector2(Mathf.Clamp(x, halfWidth + 12, Screen.width - halfWidth - 12),
            Mathf.Clamp(startPosition.y, halfHeight + 12, Screen.height - halfHeight - 12));
        progress = 0; Animate();
    }

    private void Update()
    {
        if (preview == null) return;
        progress = Mathf.Min(1, progress + Time.unscaledDeltaTime / .14f); Animate();
    }
    private void Animate()
    {
        float t = Mathf.SmoothStep(0, 1, progress);
        preview.position = Vector2.Lerp(startPosition, endPosition, t);
        preview.localScale = Vector3.one * Mathf.Lerp(startScale, finalScale, t); opacity.alpha = t;
    }
    private void Hide()
    {
        if (overlay != null) { overlay.SetActive(false); Destroy(overlay); }
        overlay = null; preview = null; if (current == this) current = null;
    }
    private static RectTransform MakeRect(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); return rect;
    }
    private TMP_Text MakeText(string name, string content, float fontSize, TextAlignmentOptions alignment)
    {
        var text = MakeRect(name, preview).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = sourceTitle.font; text.fontSize = fontSize; text.enableAutoSizing = false;
        text.margin = Vector4.zero; text.textWrappingMode = TextWrappingModes.Normal;
        text.alignment = alignment; text.color = new Color(.12f, .09f, .06f);
        text.raycastTarget = false; text.text = content; text.overflowMode = TextOverflowModes.Overflow; return text;
    }
    private static void Place(RectTransform rect, float top, float height, float width)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1); rect.pivot = new Vector2(.5f, 1);
        rect.anchoredPosition = new Vector2(0, -top); rect.sizeDelta = new Vector2(width, height);
    }
}
