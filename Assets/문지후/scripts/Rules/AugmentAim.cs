using System.Collections.Generic;
using UnityEngine;

/// <summary>Shared grid aiming math, independent of input devices and networking.</summary>
public static class AugmentAim
{
    public const int CatapultMaxPower = 5;
    public static int CatapultDistance(int power) => Mathf.Clamp(power,1,CatapultMaxPower)==CatapultMaxPower?22:Mathf.Clamp(power,1,CatapultMaxPower)*4;
    public static float CatapultCharge(float seconds)=>Mathf.PingPong(Mathf.Max(0,seconds)*2.5f,1f);
    public static int CatapultPower(float seconds)=>Mathf.Clamp(1+Mathf.FloorToInt(CatapultCharge(seconds)*CatapultMaxPower),1,CatapultMaxPower);
    public static bool CatapultDirection(Vector2 origin,Vector2 pointer,out Vector2Int direction)
    {
        Vector2 delta=pointer-origin;direction=Vector2Int.zero;if(delta.magnitude<.3f)return false;
        delta.Normalize();direction=new Vector2Int(Mathf.RoundToInt(delta.x*1000),Mathf.RoundToInt(delta.y*1000));return true;
    }
    public static Vector2Int CatapultLanding(Vector2Int origin,Vector2Int direction,int power)
    {
        Vector2 offset=PreciseDirection(direction)*CatapultDistance(power);
        return origin+new Vector2Int(Mathf.RoundToInt(offset.x),Mathf.RoundToInt(offset.y));
    }
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
    /// <summary>Slingshot input for flicking. Keeps the exact pull vector at 1/1000-cell precision.</summary>
    public static bool FlickPull(Vector2 origin, Vector2 pointer, out Vector2Int direction, out int power)
    {
        Vector2 pull=origin-pointer;direction=Vector2Int.zero;power=1;
        if(pull.magnitude<.3f)return false;
        Vector2 normalized=pull.normalized;
        direction=new Vector2Int(Mathf.RoundToInt(normalized.x*1000),Mathf.RoundToInt(normalized.y*1000));
        power=Mathf.Clamp(Mathf.CeilToInt(pull.magnitude),1,8);
        return true;
    }
    public static Vector2 PreciseDirection(Vector2Int direction)
    {
        Vector2 value=new Vector2(direction.x,direction.y);
        return value.sqrMagnitude>.001f?value.normalized:Vector2.zero;
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
        if(id==3)
        {
            Vector2 precise=PreciseDirection(direction);
            origin+=new Vector2Int(Mathf.RoundToInt(precise.x*Mathf.Clamp(power,1,8)),Mathf.RoundToInt(precise.y*Mathf.Clamp(power,1,8)));
        }
        else if(id==9)
        {
            origin=CatapultLanding(origin,direction,power);
            if(!AugmentBoard.Inside(origin.x,origin.y))return cells;
        }
        else if (id == 17) origin += direction;
        int min = id == 1 || id == 9 || id == 15 || id == 19 ? -1 : id == 11 ? 1 - Mathf.Clamp(power, 1, 5) : 0;
        int max = id == 6 ? 1 : -min;
        for (int y = min; y <= max; y++) for (int x = min; x <= max; x++)
        {
            var cell = origin + new Vector2Int(x, y);
            if (AugmentBoard.Inside(cell.x, cell.y)) cells.Add(cell);
        }
        return cells;
    }
}
