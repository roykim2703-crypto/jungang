using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>A compact card expands into a readable card without intercepting clicks.</summary>
public sealed class CardDetails : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private const float PreviewWidth = 400f;
    private static CardDetails current;
    private TMP_Text sourceTitle;
    private string title, description, grade;
    private Sprite artwork, frame;
    private GameObject overlay;
    private RectTransform preview;
    private CanvasGroup opacity;
    private Vector2 startPosition, endPosition;
    private float progress, startScale, finalScale;
    private bool used;

    public void Bind(TMP_Text titleText, string body, string value, Sprite picture, Sprite background,bool isUsed=false)
    {
        bool changed = title != titleText.text || description != body || grade != value || artwork != picture||used!=isUsed;
        sourceTitle = titleText;
        title = titleText.text; description = body; grade = value; artwork = picture; frame = background;
        used=isUsed;
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
        preview.sizeDelta = new Vector2(PreviewWidth, 480);
        var frameRect = MakeRect("Frame", preview);
        frameRect.anchorMin = Vector2.zero; frameRect.anchorMax = Vector2.one;
        frameRect.offsetMin = frameRect.offsetMax = Vector2.zero;
        var face = frameRect.gameObject.AddComponent<Image>();
        face.sprite = frame; face.color = frame == null ? new Color(1f, .96f, .86f) : Color.white;
        face.raycastTarget = false;
        RoundedUI.Apply(face, 22f);
        var shadow = frameRect.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, .4f); shadow.effectDistance = new Vector2(5, -5);
        TMP_Text heading = MakeText("Title", title, 24, TextAlignmentOptions.Center);
        TMP_Text body = MakeText("Description", description, 18, TextAlignmentOptions.TopLeft);
        TMP_Text value = MakeText("Grade", grade, 20, TextAlignmentOptions.Center);
        float titleHeight = Mathf.Max(42, heading.GetPreferredValues(title, PreviewWidth * .84f, 0).y + 8);
        float bodyHeight = Mathf.Max(96, body.GetPreferredValues(description, PreviewWidth * .78f, 0).y + 12);
        float gradeHeight = Mathf.Max(24, value.GetPreferredValues(grade, PreviewWidth * .72f, 0).y + 8);
        // Keep every element inside its printed slot as the card grows for long descriptions.
        float height = Mathf.Max(PreviewWidth * 169f / 100f, titleHeight / .13f, bodyHeight / .23f, gradeHeight / .08f);
        preview.sizeDelta = new Vector2(PreviewWidth, height);
        Place(heading.rectTransform, height * .055f, height * .13f, PreviewWidth * .84f);
        var window = MakeRect("ArtworkWindow", preview);
        Place(window, height * .22f, height * .34f, PreviewWidth * .80f);
        window.SetSiblingIndex(frame == null ? 1 : 0);
        var paper = window.gameObject.AddComponent<Image>();
        paper.color = new Color(1f, .98f, .93f); paper.raycastTarget = false;
        RoundedUI.Apply(paper, 14f);
        window.gameObject.AddComponent<RectMask2D>();
        var imageRect = MakeRect("Artwork", window);
        imageRect.anchorMin = imageRect.anchorMax = imageRect.pivot = new Vector2(.5f, .5f);
        imageRect.anchoredPosition = Vector2.zero;
        Image picture = imageRect.gameObject.AddComponent<Image>();
        RoundedUI.Apply(picture, 14f);
        picture.sprite = artwork; picture.raycastTarget = false; picture.enabled = artwork != null;
        if (artwork != null)
        {
            var fit = imageRect.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = artwork.rect.width / Mathf.Max(1, artwork.rect.height);
        }
        Place(body.rectTransform, height * .57f, height * .23f, PreviewWidth * .78f);
        Place(value.rectTransform, height * .845f, height * .08f, PreviewWidth * .72f);

        Canvas originalCanvas = GetComponentInParent<Canvas>();
        Camera camera = originalCanvas != null && originalCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? originalCanvas.worldCamera : null;
        var corners = new Vector3[4]; ((RectTransform)transform).GetWorldCorners(corners);
        Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        Vector2 topRight = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
        startPosition = (bottomLeft + topRight) * .5f;
        finalScale = Mathf.Min(1f, (Screen.height - 24f) / height, (Screen.width - 24f) / PreviewWidth);
        startScale = Mathf.Min(finalScale, Mathf.Max(.1f, (topRight.y - bottomLeft.y) / height));
        float halfWidth = PreviewWidth * .5f * finalScale, halfHeight = height * finalScale * .5f;
        float x = startPosition.x > Screen.width * .5f ? bottomLeft.x - halfWidth - 12 : topRight.x + halfWidth + 12;
        endPosition = new Vector2(Mathf.Clamp(x, halfWidth + 12, Screen.width - halfWidth - 12),
            Mathf.Clamp(startPosition.y, halfHeight + 12, Screen.height - halfHeight - 12));
        CardUsedAppearance.Apply(preview.gameObject,used);
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
