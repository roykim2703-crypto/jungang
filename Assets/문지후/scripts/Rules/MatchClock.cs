using System;
using UnityEngine;

[Serializable]
public sealed class MatchClock
{
    public const float DefaultSeconds = 300f;
    public float blackSeconds = DefaultSeconds;
    public float whiteSeconds = DefaultSeconds;

    public void Reset(float seconds = DefaultSeconds)
    {
        blackSeconds = whiteSeconds = Mathf.Max(1f, seconds);
    }

    public bool Tick(int side, float deltaSeconds)
    {
        if (deltaSeconds <= 0f) return false;
        if (side == 1)
        {
            blackSeconds = Mathf.Max(0f, blackSeconds - deltaSeconds);
            return blackSeconds <= 0f;
        }
        if (side == 2)
        {
            whiteSeconds = Mathf.Max(0f, whiteSeconds - deltaSeconds);
            return whiteSeconds <= 0f;
        }
        return false;
    }

    public float Remaining(int side) => side == 1 ? blackSeconds : whiteSeconds;
}
