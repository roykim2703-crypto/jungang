using System.Collections.Generic;
using UnityEngine;

/// <summary>Shared grid aiming math, independent of input devices and networking.</summary>
public static class AugmentAim
{
    public static bool Directional(int id) => id == 3 || id == 9 || id == 13 || id == 17;
    public static bool Drag(Vector2 origin, Vector2 pointer, bool reverse, out Vector2Int direction, out int power)
    {
        Vector2 delta = reverse ? origin - pointer : pointer - origin;
        direction = Vector2Int.zero; power = 1;
        if (delta.magnitude < .3f) return false;
        int octant = Mathf.RoundToInt(Mathf.Atan2(delta.y, delta.x) / (Mathf.PI / 4));
        direction = new Vector2Int(Mathf.RoundToInt(Mathf.Cos(octant * Mathf.PI / 4)), Mathf.RoundToInt(Mathf.Sin(octant * Mathf.PI / 4)));
        power = Mathf.Clamp(Mathf.RoundToInt(delta.magnitude), 1, 3);
        return true;
    }
    public static List<Vector2Int> Cells(int id, Vector2Int origin, Vector2Int direction, int power)
    {
        var cells = new List<Vector2Int>();
        if (id == 13)
        {
            if (direction == Vector2Int.zero) direction = Vector2Int.right;
            Vector2Int start = origin;
            while (AugmentBoard.Inside(start.x - direction.x, start.y - direction.y)) start -= direction;
            for (var p = start; AugmentBoard.Inside(p.x, p.y); p += direction) cells.Add(p);
            return cells;
        }
        if (id == 3 || id == 9) origin += direction * Mathf.Clamp(power, 1, 3);
        else if (id == 17) origin += direction;
        int min = id == 1 || id == 15 || id == 19 ? -1 : id == 11 ? 1 - Mathf.Clamp(power, 1, 5) : 0;
        int max = id == 6 ? 1 : -min;
        for (int y = min; y <= max; y++) for (int x = min; x <= max; x++)
        {
            var cell = origin + new Vector2Int(x, y);
            if (AugmentBoard.Inside(cell.x, cell.y)) cells.Add(cell);
        }
        return cells;
    }
}
