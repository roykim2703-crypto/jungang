using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class AugmentArtworkSetup
{
    [Serializable] private class CatalogData { public CardManager.CardData[] cards; }
    private const string Root = "Assets/Image/증강 그림/";
    private const string CatalogPath = "Assets/Resources/AugmentArtworkCatalog.asset";

    // Names deliberately differ in the source sheet and image filenames.
    private static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>
    {
        { "개꿀잼 상자", "꿀잼박스" },
        { "돌 겹치기", "겹쳐두기" },
        { "바둑 아니였음?", "바둑" },
        { "회색?", "회색돌" },
        { "Stone", "돌" },
        { "언제부터 \"흑돌\"을 두었다고 생각한거지?", "언제부터흑돌을두고있다고생각한거지" },
        { "판 뒤집기", "동전던지기" }
    };

    private static string Normalize(string value) =>
        new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

    [MenuItem("Tools/Cards/Sync Artwork By Card Name")]
    public static void Sync()
    {
        var images = AssetDatabase.FindAssets("t:Sprite", new[] { Root.TrimEnd('/') })
            .Select(AssetDatabase.GUIDToAssetPath).Distinct()
            .SelectMany(path => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()).ToArray();
        TextAsset source = Resources.Load<TextAsset>("AugmentCatalog");
        if (source == null) throw new InvalidOperationException("AugmentCatalog.json이 없습니다.");
        var definitions = JsonUtility.FromJson<CatalogData>(source.text.TrimStart('\uFEFF'));
        var entries = new List<AugmentArtworkCatalog.Entry>();
        foreach (var card in definitions.cards)
        {
            string expected = Aliases.TryGetValue(card.DisplayName, out string alias) ? alias : card.DisplayName;
            Sprite[] matches = images.Where(sprite => Normalize(sprite.name) == Normalize(expected)).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException($"{card.DisplayName}: 이미지 '{expected}'가 정확히 하나 필요합니다 (발견 {matches.Length}).");
            entries.Add(new AugmentArtworkCatalog.Entry { CardName = card.DisplayName, Artwork = matches[0] });
        }

        var catalog = AssetDatabase.LoadAssetAtPath<AugmentArtworkCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<AugmentArtworkCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }
        Undo.RecordObject(catalog, "Match augment artwork by name");
        catalog.Entries = entries.ToArray();
        EditorUtility.SetDirty(catalog);
        // Update authored catalogs too, so the Inspector and runtime agree.
        foreach (var managerInScene in UnityEngine.Object.FindObjectsByType<CardManager>(FindObjectsInactive.Include))
        {
            Undo.RecordObject(managerInScene, "Match augment artwork by name");
            AugmentCatalog.Populate(managerInScene);
            EditorUtility.SetDirty(managerInScene);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(managerInScene.gameObject.scene);
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"증강 {entries.Count}개의 이름과 이미지를 연결했습니다.");
    }
}
