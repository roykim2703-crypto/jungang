using System.Runtime.InteropServices;
using UnityEngine;

public static class MobileGameInput
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern int Jungang_IsMobileBrowser();
#endif
    public static bool RequiresPlacementConfirmation
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return Jungang_IsMobileBrowser()!=0;
#else
            return Application.isMobilePlatform;
#endif
        }
    }
}
