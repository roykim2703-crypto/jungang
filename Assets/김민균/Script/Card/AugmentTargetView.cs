using System.Collections.Generic;
using UnityEngine;

/// <summary>Reused translucent cell boxes and a drag trajectory, with no raycast targets.</summary>
public sealed class AugmentTargetView : MonoBehaviour
{
    private readonly List<SpriteRenderer> boxes = new List<SpriteRenderer>();
    private int count;
    private SpriteRenderer Box(Vector3 position, Vector3 scale, Color color)
    {
        if (count == boxes.Count)
        {
            var go = new GameObject("RangeBox"); go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = AugmentEffectView.Square;
            renderer.sortingOrder = 90; boxes.Add(renderer);
        }
        var box = boxes[count++]; box.gameObject.SetActive(true);
        box.transform.position = position; box.transform.localScale = scale; box.color = color; return box;
    }
    public void Hide()
    {
        foreach (var box in boxes) if (box != null) box.gameObject.SetActive(false);
    }
    public void Show(List<Vector2Int> cells, Vector3 center, float gap, Color color, Vector2Int? origin, Vector2Int direction, int power, Vector3? pullPosition)
    {
        count = 0;
        foreach (var cell in cells)
        {
            Vector3 p = Position(cell, center, gap);
            Box(p, new Vector3(gap * .96f, gap * .96f, 1), new Color(color.r, color.g, color.b, .19f));
            float half = gap * .48f, edge = gap * .045f;
            Box(p + Vector3.left * half, new Vector3(edge, gap, 1), color);
            Box(p + Vector3.right * half, new Vector3(edge, gap, 1), color);
            Box(p + Vector3.up * half, new Vector3(gap, edge, 1), color);
            Box(p + Vector3.down * half, new Vector3(gap, edge, 1), color);
        }
        if (origin.HasValue)
        {
            Vector3 from = Position(origin.Value, center, gap);
            Box(from, Vector3.one * gap * .22f, Color.cyan);
            Vector3 to = from + new Vector3(direction.x, -direction.y, 0) * gap * power;
            for (int i = 1; i <= 12; i++)
                Box(Vector3.Lerp(from, to, i / 12f), Vector3.one * gap * (i == 12 ? .23f : .075f), Color.cyan);
            if (pullPosition.HasValue)
                for (int i = 1; i <= 8; i++) Box(Vector3.Lerp(from, pullPosition.Value, i / 8f), Vector3.one * gap * .065f, new Color(1, .65f, .15f));
        }
        for (int i = count; i < boxes.Count; i++) boxes[i].gameObject.SetActive(false);
    }
    private static Vector3 Position(Vector2Int cell, Vector3 center, float gap) =>
        new Vector3(center.x + (cell.x - 9) * gap, center.y - (cell.y - 9) * gap, -.2f);
}
