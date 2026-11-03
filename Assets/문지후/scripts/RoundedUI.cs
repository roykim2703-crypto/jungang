using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Clips the existing UI mesh so artwork and button colors are preserved.</summary>
[DisallowMultipleComponent]
public sealed class RoundedUI : BaseMeshEffect
{
    [SerializeField] private float radius = 14f;
    private readonly List<UIVertex> source = new List<UIVertex>();
    private readonly List<UIVertex> polygon = new List<UIVertex>();
    private readonly List<UIVertex> clipped = new List<UIVertex>();
    private readonly List<Vector2> boundary = new List<Vector2>();

    public static void Apply(Image image, float cornerRadius = 14f)
    {
        if (image == null) return;
        var effect = image.GetComponent<RoundedUI>();
        if (effect == null) effect = image.gameObject.AddComponent<RoundedUI>();
        effect.radius = cornerRadius;
        image.SetVerticesDirty();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= StyleScene;
        SceneManager.sceneLoaded += StyleScene;
    }

    private static void StyleScene(Scene scene, LoadSceneMode mode)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var button in root.GetComponentsInChildren<Button>(true))
                Apply(button.targetGraphic as Image);
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                // Only panel surfaces, never the board, stone icons or full-screen dimmers.
                switch (image.name)
                {
                    case "RewardPanel": case "사이드 탭": case "상단 배젤":
                    case "배경": case "surrenrder": case "surrender Background":
                    case "GetTime": case "카드": case "그림":
                    case "BlackCards": case "WhiteCards": case "gibo":
                    case "MoveHistoryViewport": case "RewardGaugeTrack":
                        Apply(image, 18f); break;
                }
            }
        }
    }

    public override void ModifyMesh(VertexHelper helper)
    {
        if (!IsActive() || radius <= 0f) return;
        var rect = graphic.rectTransform.rect;
        float r = Mathf.Min(radius, rect.width * .5f, rect.height * .5f);
        if (r <= 0f) return;
        boundary.Clear();
        for (int corner = 0; corner < 4; corner++)
        {
            var center = new Vector2(corner == 0 || corner == 3 ? rect.xMax - r : rect.xMin + r,
                corner < 2 ? rect.yMax - r : rect.yMin + r);
            for (int step = 0; step <= 6; step++)
            {
                float angle = (corner * 90f + step * 15f) * Mathf.Deg2Rad;
                boundary.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r);
            }
        }
        source.Clear();
        helper.GetUIVertexStream(source);
        helper.Clear();
        for (int triangle = 0; triangle < source.Count; triangle += 3)
        {
            polygon.Clear();
            polygon.Add(source[triangle]); polygon.Add(source[triangle + 1]); polygon.Add(source[triangle + 2]);
            for (int edge = 0; edge < boundary.Count && polygon.Count > 0; edge++)
            {
                Vector2 a = boundary[edge], b = boundary[(edge + 1) % boundary.Count];
                clipped.Clear();
                UIVertex previous = polygon[polygon.Count - 1];
                float previousDistance = Side(a, b, previous.position);
                foreach (var current in polygon)
                {
                    float distance = Side(a, b, current.position);
                    if ((distance >= 0f) != (previousDistance >= 0f))
                        clipped.Add(Interpolate(previous, current, previousDistance / (previousDistance - distance)));
                    if (distance >= 0f) clipped.Add(current);
                    previous = current; previousDistance = distance;
                }
                polygon.Clear(); polygon.AddRange(clipped);
            }
            int start = helper.currentVertCount;
            foreach (var vertex in polygon) helper.AddVert(vertex);
            for (int i = 1; i + 1 < polygon.Count; i++) helper.AddTriangle(start, start + i, start + i + 1);
        }
    }

    private static float Side(Vector2 a, Vector2 b, Vector3 point)
        => (b.x - a.x) * (point.y - a.y) - (b.y - a.y) * (point.x - a.x);

    private static UIVertex Interpolate(UIVertex a, UIVertex b, float t)
    {
        a.position = Vector3.LerpUnclamped(a.position, b.position, t);
        a.color = Color32.Lerp(a.color, b.color, t);
        a.uv0 = Vector4.LerpUnclamped(a.uv0, b.uv0, t);
        a.uv1 = Vector4.LerpUnclamped(a.uv1, b.uv1, t);
        a.uv2 = Vector4.LerpUnclamped(a.uv2, b.uv2, t);
        a.uv3 = Vector4.LerpUnclamped(a.uv3, b.uv3, t);
        return a;
    }
}
