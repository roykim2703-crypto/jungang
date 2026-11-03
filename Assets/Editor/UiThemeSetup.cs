using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class UiThemeSetup
{
    private const string FontPath = "Assets/Fonts/서울남산 장체L SDF.asset";
    [MenuItem("Tools/UI/Apply Seoul Namsan Theme")]
    public static void Apply()
    {
        if (Application.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new System.InvalidOperationException("Save scene changes first.");
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null || font.sourceFontFile == null) throw new System.InvalidOperationException("Seoul Namsan font is missing.");
        font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        font.isMultiAtlasTexturesEnabled = true;
        EditorUtility.SetDirty(font);
        var settings = new SerializedObject(TMP_Settings.instance);
        settings.FindProperty("m_defaultFontAsset").objectReferenceValue = font;
        settings.ApplyModifiedPropertiesWithoutUndo();
        var menu = AssetDatabase.LoadAssetAtPath<MultiplayerMenuAssets>("Assets/Resources/MultiplayerMenuAssets.asset");
        menu.Font = font; EditorUtility.SetDirty(menu);
        foreach (string path in new[] { "Assets/Prefeb/카드_0.prefab", "Assets/Prefeb/상단 배젤_0.prefab" })
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try { Style(root, font); PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (var entry in EditorBuildSettings.scenes)
            {
                if (!entry.enabled) continue;
                var scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Single);
                foreach (var root in scene.GetRootGameObjects()) Style(root, font);
                foreach (var tutorial in Object.FindObjectsByType<HomeTutorial>(FindObjectsInactive.Include))
                {
                    var serialized = new SerializedObject(tutorial);
                    serialized.FindProperty("font").objectReferenceValue = font;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        AssetDatabase.SaveAssets();
        Debug.Log("Seoul Namsan theme applied to all build scenes, UI prefabs, and runtime font defaults.");
    }
    private static void Style(GameObject root, TMP_FontAsset font)
    {
        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            text.font = font;
            text.fontSharedMaterial = font.material;
            EditorUtility.SetDirty(text);
        }
        foreach (var text in root.GetComponentsInChildren<Text>(true))
        {
            text.font = font.sourceFontFile;
            EditorUtility.SetDirty(text);
        }
        foreach (var button in root.GetComponentsInChildren<Button>(true))
            RoundedUI.Apply(button.targetGraphic as Image, 18f);
        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            switch (image.name)
            {
                case "RewardPanel": case "사이드 탭": case "상단 배젤":
                case "배경": case "surrenrder": case "surrender Background":
                case "GetTime": case "카드": case "그림": case "gibo":
                case "BlackCards": case "WhiteCards": case "MoveHistoryViewport":
                case "RewardGaugeTrack":
                    RoundedUI.Apply(image, image.name == "RewardPanel" ? 28f : 18f); break;
            }
        }
    }
}
