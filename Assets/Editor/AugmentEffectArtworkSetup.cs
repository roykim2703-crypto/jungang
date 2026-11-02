using System;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class AugmentEffectArtworkSetup
{
    private const string Output = "Assets/Resources/AugmentEffectArtworkCatalog.asset";
    private const string Root = "Assets/Image/증강 효과 그림/";

    [MenuItem("Tools/Cards/Sync Augment Effect Artwork")]
    public static void Sync()
    {
        EnsureGolemBody();
        var catalog = AssetDatabase.LoadAssetAtPath<AugmentEffectArtworkCatalog>(Output);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<AugmentEffectArtworkCatalog>();
            AssetDatabase.CreateAsset(catalog, Output);
        }

        Undo.RecordObject(catalog, "Sync augment effect artwork");
        catalog.Fist = First("ChatGPT 이미지 2026년 11월 1일 오후 04_49_50.png");
        catalog.MeteorRock = First("ChatGPT 이미지 2026년 11월 1일 오후 04_49_57.png");
        catalog.MeteorImpact = First("ChatGPT 이미지 2026년 11월 1일 오후 04_50_01.png");
        catalog.BlackHole = First("ChatGPT 이미지 2026년 11월 1일 오후 04_50_07.png");
        catalog.Hammer = First("제목 없음.png");
        catalog.CatapultFrames = All("투석기_3프레임_중간동작_투명.png");
        var golemSprites=All("기계 골렘 픽셀아트 부품 시트.png");
        catalog.GolemParts = golemSprites.Where(s=>s.name!="GolemBody").ToArray();
        catalog.GolemBody = golemSprites.FirstOrDefault(s=>s.name=="GolemBody");
        if (catalog.CatapultFrames.Length < 3)
            throw new InvalidOperationException("투석기 이미지에는 3개의 프레임이 필요합니다.");
        if (catalog.GolemParts.Length < 15)
            throw new InvalidOperationException("거신병 부품 이미지에는 15개 이상의 조각이 필요합니다.");
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        Debug.Log($"증강 효과 이미지 연결 완료: 망치 {(catalog.Hammer!=null?"연결":"누락")}, 투석기 {catalog.CatapultFrames.Length}프레임, 거신병 {catalog.GolemParts.Length}파트");
    }

    private static void EnsureGolemBody()
    {
        string path=Root+"기계 골렘 픽셀아트 부품 시트.png";
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(importer==null)throw new InvalidOperationException("거신병 이미지가 없습니다.");
        var factories=new SpriteDataProviderFactories();factories.Init();
        var provider=factories.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
        var rects=provider.GetSpriteRects().ToList();
        if(rects.Any(r=>r.name=="GolemBody"))return;
        var body=new SpriteRect{name="GolemBody",rect=new Rect(42,238,432,550),alignment=SpriteAlignment.Center,pivot=new Vector2(.5f,.5f),spriteID=GUID.Generate()};
        rects.Add(body);provider.SetSpriteRects(rects.ToArray());
        var names=provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if(names!=null)
        {
            var pairs=names.GetNameFileIdPairs().ToList();pairs.Add(new SpriteNameFileIdPair(body.name,body.spriteID));names.SetNameFileIdPairs(pairs);
        }
        provider.Apply();importer.SaveAndReimport();
    }

    private static Sprite First(string file) => All(file).FirstOrDefault();
    private static Sprite[] All(string file) => AssetDatabase.LoadAllAssetsAtPath(Root + file)
        .OfType<Sprite>().OrderBy(SpriteIndex).ToArray();
    private static int SpriteIndex(Sprite sprite)
    {
        string suffix = sprite.name.Split('_').Last();
        return int.TryParse(suffix, out int value) ? value : 0;
    }
}
