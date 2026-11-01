using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class HomeTutorialSetup
{
    [MenuItem("Tools/Home/Setup Tutorial")]
    public static void Build()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != "Assets/Scenes/Home.unity")
            throw new System.InvalidOperationException("Open Home in Edit Mode first.");
        var menu = Object.FindAnyObjectByType<HomeMenu>();
        var tutorial = menu.GetComponent<HomeTutorial>();
        if (tutorial == null) tutorial = Undo.AddComponent<HomeTutorial>(menu.gameObject);
        var so = new SerializedObject(tutorial);
        so.FindProperty("frameImage").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Image/타이틀 화면/설명창 배경 .png");
        so.FindProperty("font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/서울남산 장체L SDF.asset");
        var pages = so.FindProperty("tutorialPages");
        // Re-running setup must preserve authored tutorial content.
        if (pages.arraySize == 0)
        {
            pages.arraySize = 2;
            string[] images = { "오목판", "증강 예시" };
            string[] descriptions = {
                "빈 칸을 선택해 돌을 놓으세요.\n인스펙터에서 이 설명과 컷씬 이미지를 수정할 수 있습니다.",
                "증강으로 다양한 효과를 활용해 보세요.\n컷씬을 여러 장 넣으면 흰색 페이드와 함께 순서대로 반복됩니다."
            };
            for (int i = 0; i < pages.arraySize; i++)
            {
                var page = pages.GetArrayElementAtIndex(i);
                page.FindPropertyRelative("title").stringValue = "튜토리얼 장면 " + (i + 1);
                page.FindPropertyRelative("description").stringValue = descriptions[i];
                var frames = page.FindPropertyRelative("cutscenes");
                frames.arraySize = 1;
                frames.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAllAssetsAtPath("Assets/Image/타이틀 화면/" + images[i] + ".png")
                    .OfType<Sprite>().OrderByDescending(s => s.rect.width * s.rect.height).FirstOrDefault();
            }
        }
        so.ApplyModifiedProperties();
        var menuSO = new SerializedObject(menu);
        menuSO.FindProperty("tutorial").objectReferenceValue = tutorial;
        menuSO.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = menu.gameObject;
    }
}
