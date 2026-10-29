using System;

[Serializable]
public sealed class MeteorCharge
{
    public const double Duration = 3;
    public bool active;
    public int side, x, y, clicks;
    public double deadline;
    public static int Power(int clicks) => clicks <= 12 ? 2 : clicks <= 20 ? 3 : clicks <= 32 ? 4 : 5;
    public int Size => Power(clicks) * 2 - 1;
    public double Remaining(double now) => active ? Math.Max(0, deadline - now) : 0;
    public void Start(int player, int column, int row, double now)
    { active = true; side = player; x = column; y = row; clicks = 0; deadline = now + Duration; }
    public bool Tap(int player, double now)
    {
        if (!active || player != side || now >= deadline) return false;
        clicks = Math.Min(60, clicks + 1); return true;
    }
}
