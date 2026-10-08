using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CardSideTabSetup
{
    [MenuItem("Tools/Cards/Setup Seed And Side Tab")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Game.unity") throw new InvalidOperationException("Open Game scene first.");

        Transform[] all = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
        Transform gibo = all.Single(t => t.name == "gibo");
        Transform sideTab = all.Single(t => t.name == "사이드 탭");
        Scrollbar rewardProgressBar = all.Single(t => t.name == "Scrollbar").GetComponent<Scrollbar>();
        var manager = UnityEngine.Object.FindAnyObjectByType<CardManager>();
        var matchController = UnityEngine.Object.FindAnyObjectByType<수_놓기>();
        var guideStone = UnityEngine.Object.FindAnyObjectByType<가이드_돌_코드>();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefeb/카드_0.prefab");
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/CardUI SDF.asset");
        if (manager == null || matchController == null || guideStone == null || rewardProgressBar == null || prefab == null || font == null)
            throw new InvalidOperationException("CardManager, match controller, guide stone, scrollbar, card prefab or font is missing.");

        var matchBridge = manager.GetComponent<CardMatchBridge>();
        if (matchBridge == null) matchBridge = Undo.AddComponent<CardMatchBridge>(manager.gameObject);
        var bridgeSerialized = new SerializedObject(matchBridge);
        bridgeSerialized.FindProperty("cardManager").objectReferenceValue = manager;
        bridgeSerialized.FindProperty("turnController").objectReferenceValue = matchController;
        bridgeSerialized.FindProperty("guideStone").objectReferenceValue = guideStone;
        bridgeSerialized.ApplyModifiedProperties();

        var ui = sideTab.GetComponent<CardSideTabUI>();
        if (ui == null) ui = Undo.AddComponent<CardSideTabUI>(sideTab.gameObject);
        var acquisitionUi = UnityEngine.Object.FindAnyObjectByType<CardAcquisitionUI>(FindObjectsInactive.Include);
        if (acquisitionUi != null)
        {
            foreach (Button cardButton in acquisitionUi.GetComponentsInChildren<Button>(true))
                cardButton.transition = Selectable.Transition.None;
            EditorUtility.SetDirty(acquisitionUi.gameObject);
        }

        Button oldSeedButton = gibo.GetComponent<Button>();
        if (oldSeedButton != null) Undo.DestroyObjectImmediate(oldSeedButton);

        Transform seedTransform = gibo.Find("SeedData");
        TMP_Text seedLabel;
        if (seedTransform == null)
        {
            var seedObject = new GameObject("SeedData", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(seedObject, "Create seed button label");
            seedObject.transform.SetParent(gibo, false);
            seedLabel = seedObject.GetComponent<TMP_Text>();
        }
        else seedLabel = seedTransform.GetComponent<TMP_Text>();
        ConfigureText(seedLabel, font, 9f);
        RectTransform seedRect = seedLabel.rectTransform;
        seedRect.anchorMin = new Vector2(0f, 1f);
        seedRect.anchorMax = new Vector2(1f, 1f);
        seedRect.pivot = new Vector2(0.5f, 1f);
        seedRect.anchoredPosition = new Vector2(0f, -4f);
        seedRect.sizeDelta = new Vector2(-12f, 22f);
        seedLabel.text = "SEED 0";
        seedLabel.textWrappingMode = TextWrappingModes.NoWrap;
        seedLabel.raycastTarget = true;
        Button seedButton = seedLabel.GetComponent<Button>();
        if (seedButton == null) seedButton = Undo.AddComponent<Button>(seedLabel.gameObject);
        seedButton.targetGraphic = seedLabel;
        seedButton.transition = Selectable.Transition.None;

        Transform viewportTransform = gibo.Find("MoveHistoryViewport");
        if (viewportTransform == null)
        {
            var viewportObject = new GameObject("MoveHistoryViewport", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            Undo.RegisterCreatedObjectUndo(viewportObject, "Create move history scroll view");
            viewportObject.transform.SetParent(gibo, false);
            viewportTransform = viewportObject.transform;
        }
        RectTransform viewportRect = (RectTransform)viewportTransform;
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.pivot = new Vector2(0.5f, 0.5f);
        viewportRect.offsetMin = new Vector2(5f, 7f);
        viewportRect.offsetMax = new Vector2(-5f, -30f);
        Image viewportImage = viewportTransform.GetComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.001f);
        viewportImage.raycastTarget = true;

        Transform oldHistory = gibo.Find("MoveHistory");
        if (oldHistory != null) oldHistory.SetParent(viewportTransform, false);
        TMP_Text moveHistoryLabel = GetOrCreateText(viewportTransform, "MoveHistory");
        ConfigureText(moveHistoryLabel, font, 10f);
        moveHistoryLabel.alignment = TextAlignmentOptions.TopLeft;
        moveHistoryLabel.textWrappingMode = TextWrappingModes.NoWrap;
        moveHistoryLabel.overflowMode = TextOverflowModes.Overflow;
        moveHistoryLabel.text = "기보";
        RectTransform historyRect = moveHistoryLabel.rectTransform;
        historyRect.anchorMin = new Vector2(0f, 1f);
        historyRect.anchorMax = new Vector2(1f, 1f);
        historyRect.pivot = new Vector2(0.5f, 1f);
        historyRect.anchoredPosition = Vector2.zero;
        historyRect.sizeDelta = new Vector2(-8f, 290f);

        ScrollRect moveHistoryScrollRect = viewportTransform.GetComponent<ScrollRect>();
        moveHistoryScrollRect.content = historyRect;
        moveHistoryScrollRect.viewport = viewportRect;
        moveHistoryScrollRect.horizontal = false;
        moveHistoryScrollRect.vertical = true;
        moveHistoryScrollRect.movementType = ScrollRect.MovementType.Clamped;
        moveHistoryScrollRect.inertia = true;
        moveHistoryScrollRect.scrollSensitivity = 22f;

        TMP_Text rewardProgressLabel = GetOrCreateText(rewardProgressBar.transform, "RewardProgressLabel");
        ConfigureText(rewardProgressLabel, font, 12f);
        rewardProgressLabel.text = $"다음 증강까지 {manager.StonesPerReward}수";
        rewardProgressLabel.textWrappingMode = TextWrappingModes.NoWrap;
        RectTransform progressLabelRect = rewardProgressLabel.rectTransform;
        progressLabelRect.anchorMin = Vector2.zero;
        progressLabelRect.anchorMax = Vector2.one;
        progressLabelRect.pivot = new Vector2(0.5f, 0.5f);
        progressLabelRect.offsetMin = new Vector2(5f, 2f);
        progressLabelRect.offsetMax = new Vector2(-5f, -2f);
        rewardProgressBar.interactable = false;
        rewardProgressBar.SetValueWithoutNotify(0f);
        ColorBlock progressColors = rewardProgressBar.colors;
        progressColors.disabledColor = progressColors.normalColor;
        progressColors.fadeDuration = 0f;
        rewardProgressBar.colors = progressColors;

        CardSideTabUI.CardButtonSlot[] black = new CardSideTabUI.CardButtonSlot[3];
        CardSideTabUI.CardButtonSlot[] white = new CardSideTabUI.CardButtonSlot[3];
        for (int i = 0; i < 3; i++)
        {
            black[i] = CreateSlot(sideTab, prefab, font, $"BlackCardButton_{i + 1}", new Vector2(-80f + 80f * i, 55f));
            white[i] = CreateSlot(sideTab, prefab, font, $"WhiteCardButton_{i + 1}", new Vector2(-80f + 80f * i, -91f));
        }

        var serialized = new SerializedObject(ui);
        serialized.FindProperty("cardManager").objectReferenceValue = manager;
        serialized.FindProperty("matchBridge").objectReferenceValue = matchBridge;
        serialized.FindProperty("seedButton").objectReferenceValue = seedButton;
        serialized.FindProperty("seedLabel").objectReferenceValue = seedLabel;
        serialized.FindProperty("moveHistoryLabel").objectReferenceValue = moveHistoryLabel;
        serialized.FindProperty("moveHistoryScrollRect").objectReferenceValue = moveHistoryScrollRect;
        serialized.FindProperty("rewardProgressBar").objectReferenceValue = rewardProgressBar;
        serialized.FindProperty("rewardProgressLabel").objectReferenceValue = rewardProgressLabel;
        AssignSlots(serialized.FindProperty("blackSlots"), black);
        AssignSlots(serialized.FindProperty("whiteSlots"), white);
        serialized.ApplyModifiedProperties();

        EditorUtility.SetDirty(gibo.gameObject);
        EditorUtility.SetDirty(sideTab.gameObject);
        EditorUtility.SetDirty(ui);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = sideTab.gameObject;
    }

    private static CardSideTabUI.CardButtonSlot CreateSlot(Transform parent, GameObject prefab, TMP_FontAsset font, string name, Vector2 position)
    {
        Transform existing = parent.Find(name);
        if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);
        var holder = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(holder, "Create side card button");
        holder.transform.SetParent(parent, false);
        var holderRect = holder.GetComponent<RectTransform>();
        holderRect.anchorMin = holderRect.anchorMax = holderRect.pivot = new Vector2(0.5f, 0.5f);
        holderRect.anchoredPosition = position;
        holderRect.sizeDelta = new Vector2(70f, 102f);

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, holder.transform);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;
        instance.GetComponent<SpriteRenderer>().enabled = false;
        var innerCanvas = instance.GetComponentInChildren<Canvas>(true);
        innerCanvas.overrideSorting = false;
        innerCanvas.GetComponent<CanvasScaler>().enabled = false;
        var innerRect = (RectTransform)innerCanvas.transform;
        innerRect.anchorMin = innerRect.anchorMax = innerRect.pivot = new Vector2(0.5f, 0.5f);
        innerRect.localPosition = Vector3.zero;
        innerRect.localScale = Vector3.one;
        innerRect.sizeDelta = new Vector2(70f, 102f);

        Image[] images = instance.GetComponentsInChildren<Image>(true);
        Image face = images.First(image => image.name == "카드");
        face.transform.SetAsFirstSibling();
        face.rectTransform.localPosition = Vector3.zero;
        face.rectTransform.localScale = Vector3.one;
        face.rectTransform.sizeDelta = new Vector2(68f, 100f);
        face.preserveAspect = true;
        face.raycastTarget = true;
        var button = face.GetComponent<Button>();
        if (button == null) button = face.gameObject.AddComponent<Button>();
        button.targetGraphic = face;
        button.transition = Selectable.Transition.None;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.9f, 0.65f, 1f);
        colors.pressedColor = new Color(0.82f, 0.69f, 0.49f, 1f);
        button.colors = colors;

        Image artwork = images.First(image => image.name == "그림");
        artwork.rectTransform.localPosition = new Vector3(0f, 8f, 0f);
        artwork.rectTransform.localScale = Vector3.one;
        artwork.rectTransform.sizeDelta = new Vector2(50f, 35f);
        artwork.preserveAspect = true;
        artwork.raycastTarget = false;
        artwork.enabled = false;

        TMP_Text[] texts = instance.GetComponentsInChildren<TMP_Text>(true);
        TMP_Text title = texts.First(text => text.name == "이름");
        ConfigureText(title, font, 10f);
        title.rectTransform.localPosition = new Vector3(0f, 39f, 0f);
        title.rectTransform.localScale = Vector3.one;
        title.rectTransform.sizeDelta = new Vector2(60f, 15f);
        title.text = "카드";
        texts.First(text => text.name == "설명").gameObject.SetActive(false);
        texts.First(text => text.name == "등급").gameObject.SetActive(false);

        holder.SetActive(false);
        return new CardSideTabUI.CardButtonSlot { Root = holder, Button = button, Title = title, Artwork = artwork };
    }

    private static TMP_Text GetOrCreateText(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return existing.GetComponent<TMP_Text>();
        var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(textObject, $"Create {name}");
        textObject.transform.SetParent(parent, false);
        return textObject.GetComponent<TMP_Text>();
    }

    private static void ConfigureText(TMP_Text text, TMP_FontAsset font, float fontSize)
    {
        text.font = font;
        text.fontSize = fontSize;
        text.enableAutoSizing = true;
        text.fontSizeMin = fontSize * 0.65f;
        text.fontSizeMax = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.12f, 0.09f, 0.06f, 1f);
        text.raycastTarget = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
    }

    private static void AssignSlots(SerializedProperty property, CardSideTabUI.CardButtonSlot[] slots)
    {
        property.arraySize = slots.Length;
        for (int i = 0; i < slots.Length; i++)
        {
            SerializedProperty slot = property.GetArrayElementAtIndex(i);
            slot.FindPropertyRelative("Root").objectReferenceValue = slots[i].Root;
            slot.FindPropertyRelative("Button").objectReferenceValue = slots[i].Button;
            slot.FindPropertyRelative("Title").objectReferenceValue = slots[i].Title;
            slot.FindPropertyRelative("Artwork").objectReferenceValue = slots[i].Artwork;
        }
    }
}
