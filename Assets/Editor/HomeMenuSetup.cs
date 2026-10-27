using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class HomeMenuSetup
{
    [MenuItem("Tools/Home/Setup Title Buttons")]
    public static void Build()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != "Assets/Scenes/Home.unity")
            throw new InvalidOperationException("Open Home in Edit Mode first.");
        var canvas = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Canvas>(true)).First();
        var menu = canvas.GetComponent<HomeMenu>();
        if (menu == null) menu = Undo.AddComponent<HomeMenu>(canvas.gameObject);
        Button[] buttons = {
            CreateButton(canvas.transform, "StartGameButton", "게임 시작 버튼", 0, menu.StartGame),
            CreateButton(canvas.transform, "SettingsButton", "설정 버튼", 1, menu.OpenSettings),
            CreateButton(canvas.transform, "HowToPlayButton", "게임 방법 버튼", 2, menu.OpenHowToPlay)
        };
        for (int i = 0; i < buttons.Length; i++)
        {
            var navigation = buttons[i].navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = buttons[(i + 2) % 3];
            navigation.selectOnDown = buttons[(i + 1) % 3];
            buttons[i].navigation = navigation;
        }
        var eventSystem = UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
        Undo.RecordObject(eventSystem, "Set home menu selection");
        eventSystem.firstSelectedGameObject = buttons[0].gameObject;
        var scenes = EditorBuildSettings.scenes.ToList();
        int gameIndex = scenes.FindIndex(s => s.path == "Assets/Scenes/Game.unity");
        if (gameIndex < 0) scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/Game.unity", true));
        else scenes[gameIndex].enabled = true;
        EditorBuildSettings.scenes = scenes.ToArray();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static Button CreateButton(Transform parent, string name, string imageName, int index, UnityAction action)
    {
        // 자동 슬라이스로 생긴 작은 테두리 조각 대신 버튼 본체를 사용합니다.
        var sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Image/타이틀 화면/" + imageName + ".png").OfType<Sprite>().OrderByDescending(s => s.rect.width * s.rect.height).First();
        var existing = parent.Find(name);
        var go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(CanvasGroup));
        if (existing == null) Undo.RegisterCreatedObjectUndo(go, "Create home button");
        else Undo.RecordObjects(go.GetComponents<Component>(), "Update home button");
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;
        var group = go.GetComponent<CanvasGroup>();
        group.alpha = 1f;
        group.interactable = group.blocksRaycasts = true;
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.26f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -10f - index * 52f);
        rect.sizeDelta = new Vector2(220f, 220f * sprite.rect.height / sprite.rect.width);
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = true;
        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.94f, 0.8f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.75f, 0.65f, 0.5f);
        button.colors = colors;
        while (button.onClick.GetPersistentEventCount() > 0)
            UnityEventTools.RemovePersistentListener(button.onClick, 0);
        UnityEventTools.AddPersistentListener(button.onClick, action);
        var fade = go.GetComponent<TitleFadeIn>();
        if (fade == null) fade = Undo.AddComponent<TitleFadeIn>(go);
        var serialized = new SerializedObject(fade);
        serialized.FindProperty("startDelay").floatValue = 0.55f + index * 0.12f;
        serialized.FindProperty("duration").floatValue = 0.55f;
        serialized.FindProperty("moveDistance").floatValue = 30f;
        serialized.ApplyModifiedProperties();
        return button;
    }
}
