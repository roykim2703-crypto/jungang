using System;

[Serializable]
public sealed class ShotgunBurst
{
    public const double Duration=3;
    public bool active;
    public int side,hits;
    public double deadline;
    public double Remaining(double now)=>active?Math.Max(0,deadline-now):0;
    public void Start(int player,double now)
    {
        active=true;side=player;hits=0;deadline=now+Duration;
    }
    public bool Hit(int player,double now)
    {
        if(!active||player!=side||now>=deadline)return false;
        hits++;return true;
    }
}
