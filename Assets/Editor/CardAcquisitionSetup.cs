using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class CardAcquisitionSetup
{
    [MenuItem("Tools/Cards/Setup Acquisition Canvas")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Game.unity") throw new InvalidOperationException("Open Game scene first.");
        if (UnityEngine.Object.FindObjectsByType<CardAcquisitionUI>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0)
        {
            Debug.Log("CardAcquisitionCanvas already exists.");
            return;
        }

        var manager = UnityEngine.Object.FindAnyObjectByType<CardManager>();
        var placement = UnityEngine.Object.FindAnyObjectByType<수_놓기>();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefeb/카드_0.prefab");
        if (manager == null || placement == null || prefab == null) throw new InvalidOperationException("Missing manager, placement or card prefab.");
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/CardUI SDF.asset");
        if (font == null)
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NanumGothic.ttf");
            if (source == null) throw new InvalidOperationException("Korean source font is missing.");
            font = TMP_FontAsset.CreateFontAsset(source, 48, 5, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
            font.name = "CardUI SDF";
            font.isMultiAtlasTexturesEnabled = true;
            AssetDatabase.CreateAsset(font, "Assets/Fonts/CardUI SDF.asset");
            foreach (var texture in font.atlasTextures) AssetDatabase.AddObjectToAsset(texture, font);
            AssetDatabase.AddObjectToAsset(font.material, font);
        }

        Undo.RecordObject(manager, "Configure card rewards");
        manager.StonesPerReward = 20;
        manager.EarlyPhaseLastStone = 20;
        manager.MiddlePhaseLastStone = 40;
        // Only seed the original empty catalog. Never replace authored card data.
        if (manager.CardList.Count == 1 && string.IsNullOrWhiteSpace(manager.CardList[0].explanation)
            && string.IsNullOrWhiteSpace(manager.CardList[0].DisplayName) && manager.CardList[0].ItemImage == null)
        {
            for (int i = 0; i < 18; i++)
            {
                var card = i == 0 ? manager.CardList[0] : new CardManager.CardData { Name = i };
                card.DisplayName = $"샘플 카드 {i + 1:00}";
                card.Value = 1 + i / 6;
                card.AvailableFrom = i < 12 ? CardManager.AcquisitionPhase.Early : CardManager.AcquisitionPhase.Middle;
                card.AvailableUntil = i < 6 ? CardManager.AcquisitionPhase.Early : i < 12 ? CardManager.AcquisitionPhase.Middle : CardManager.AcquisitionPhase.Late;
                card.explanation = i < 6 ? "초반에 획득하는 카드" : i < 12 ? "초반과 중반에 획득하는 카드" : "중반과 후반에 획득하는 카드";
                if (i > 0) manager.CardList.Add(card);
            }
        }

        var root = new GameObject("CardAcquisitionCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(root, "Create card acquisition canvas");
        root.layer = 5;
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var ui = root.AddComponent<CardAcquisitionUI>();
        ui.CardPrefab = prefab;
        var dim = Rect("Dimmer", root.transform, Vector2.zero, Vector2.zero);
        dim.anchorMin = Vector2.zero; dim.anchorMax = Vector2.one;
        dim.offsetMin = dim.offsetMax = Vector2.zero;
        dim.gameObject.AddComponent<Image>().color = new Color(0.02f, 0.025f, 0.025f, 0.9f);
        var panel = Rect("RewardPanel", root.transform, Vector2.zero, new Vector2(1060, 620));
        panel.gameObject.AddComponent<Image>().color = new Color(0.94f, 0.86f, 0.70f, 0.98f);
        var outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(6f, -6f);
        outline.useGraphicAlpha = true;
        var darkText = new Color(0.12f, 0.09f, 0.06f);
        ui.Heading = Label("Heading", panel, new Vector2(0, 254), new Vector2(980, 52), "흑 카드 선택", 34, font, darkText);
        ui.BlackStatus = Label("BlackStatus", panel, new Vector2(0, 190), new Vector2(940, 38), "흑 · 카드 1장을 선택하세요", 23, font, darkText);
        ui.WhiteStatus = Label("WhiteStatus", panel, new Vector2(0, 190), new Vector2(940, 38), "백 · 카드 1장을 선택하세요", 23, font, darkText);
        var blackRow = Rect("BlackCards", panel, new Vector2(0, -42), new Vector2(900, 360));
        var whiteRow = Rect("WhiteCards", panel, new Vector2(0, -42), new Vector2(900, 360));
        ui.BlackGroup = blackRow.gameObject;
        ui.WhiteGroup = whiteRow.gameObject;
        ui.BlackSlots = new CardAcquisitionUI.CardSlot[3];
        ui.WhiteSlots = new CardAcquisitionUI.CardSlot[3];
        for (int i = 0; i < 3; i++)
        {
            ui.BlackSlots[i] = Slot(prefab, blackRow, i, font);
            ui.WhiteSlots[i] = Slot(prefab, whiteRow, i, font);
        }
        Label("Help", panel, new Vector2(0, -286), new Vector2(960, 32), "흑이 먼저, 다음에 백이 선택하면 대국이 이어집니다", 18, font, darkText);

        var serializedManager = new SerializedObject(manager);
        serializedManager.FindProperty("acquisitionUI").objectReferenceValue = ui;
        serializedManager.ApplyModifiedProperties();
        var serializedPlacement = new SerializedObject(placement);
        serializedPlacement.FindProperty("cardManager").objectReferenceValue = manager;
        serializedPlacement.ApplyModifiedProperties();
        root.SetActive(false);
        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(ui);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = root;
        Debug.Log("Created CardAcquisitionCanvas with six connected card prefab instances.");
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = 5;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        return rect;
    }

    private static TMP_Text Label(string name, Transform parent, Vector2 position, Vector2 size, string text, float fontSize, TMP_FontAsset font, Color color)
    {
        var label = Rect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
        ConfigureText(label, position, size, fontSize, font);
        label.text = text; label.color = color;
        return label;
    }

    private static void ConfigureText(TMP_Text text, Vector2 position, Vector2 size, float fontSize, TMP_FontAsset font)
    {
        var rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity;
        rect.anchoredPosition3D = position; rect.sizeDelta = size;
        text.font = font; text.fontSize = fontSize;
        text.enableAutoSizing = true; text.fontSizeMin = fontSize * 0.7f; text.fontSizeMax = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.18f, 0.14f, 0.10f);
        text.raycastTarget = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.margin = Vector4.zero;
    }

    private static CardAcquisitionUI.CardSlot Slot(GameObject prefab, Transform parent, int index, TMP_FontAsset font)
    {
        var holder = Rect($"Slot_{index + 1}", parent, new Vector2((index - 1) * 270, 0), new Vector2(210, 352));
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, holder);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localScale = Vector3.one;
        instance.transform.localRotation = Quaternion.identity;
        instance.GetComponent<SpriteRenderer>().enabled = false;
        var innerCanvas = instance.GetComponentInChildren<Canvas>(true);
        innerCanvas.overrideSorting = false;
        var innerRect = (RectTransform)innerCanvas.transform;
        innerRect.anchorMin = innerRect.anchorMax = new Vector2(0.5f, 0.5f);
        innerRect.localPosition = Vector3.zero; innerRect.localScale = Vector3.one;
        innerRect.sizeDelta = new Vector2(210, 352);
        innerCanvas.GetComponent<CanvasScaler>().enabled = false;
        var images = instance.GetComponentsInChildren<Image>(true);
        var face = images.First(t => t.name == "카드");
        face.transform.SetAsFirstSibling();
        face.rectTransform.anchorMin = face.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        face.rectTransform.localPosition = Vector3.zero; face.rectTransform.localScale = Vector3.one;
        face.rectTransform.sizeDelta = new Vector2(210, 352);
        face.preserveAspect = true; face.raycastTarget = true;
        var button = face.gameObject.AddComponent<Button>();
        button.targetGraphic = face;
        var colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.91f, 0.69f);
        colors.pressedColor = new Color(0.85f, 0.73f, 0.52f);
        colors.disabledColor = new Color(0.6f, 0.6f, 0.6f);
        button.colors = colors;
        var artwork = images.First(t => t.name == "그림");
        artwork.rectTransform.anchorMin = artwork.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        artwork.rectTransform.localPosition = new Vector3(0, 38, 0); artwork.rectTransform.localScale = Vector3.one;
        artwork.rectTransform.sizeDelta = new Vector2(155, 109);
        artwork.preserveAspect = true; artwork.raycastTarget = false;
        artwork.enabled = false;
        var texts = instance.GetComponentsInChildren<TMP_Text>(true);
        var title = texts.First(t => t.name == "이름");
        var description = texts.First(t => t.name == "설명");
        var value = texts.First(t => t.name == "등급");
        ConfigureText(title, new Vector2(0, 137), new Vector2(176, 40), 23, font);
        ConfigureText(description, new Vector2(0, -65), new Vector2(163, 77), 20, font);
        ConfigureText(value, new Vector2(0, -139), new Vector2(130, 28), 18, font);
        title.text = "카드 이름"; description.text = "카드 설명"; value.text = "등급";
        var timing = Label("AcquisitionTiming", holder, new Vector2(0, -190), new Vector2(248, 24), "", 16, font, new Color(0.16f, 0.12f, 0.08f));
        foreach (var component in instance.GetComponentsInChildren<Component>(true))
            if (component != null) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        return new CardAcquisitionUI.CardSlot { Button=button, Title=title, Description=description, Value=value, Timing=timing, Artwork=artwork };
    }
}
