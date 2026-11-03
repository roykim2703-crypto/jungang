using UnityEditor;
using UnityEngine;

public static class PopupAnimationChecks
{
    [MenuItem("Tools/UI/Validate Popup Entrance In Play Mode")]
    public static void Run()
    {
        if (!Application.isPlaying) throw new System.InvalidOperationException("Enter Play Mode first.");
        var root = new GameObject("PopupEntranceCheck", typeof(RectTransform), typeof(CanvasGroup));
        var panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasGroup));
        panel.transform.SetParent(root.transform, false);
        var rect = (RectTransform)panel.transform;
        rect.anchoredPosition = new Vector2(20, 40);
        root.SetActive(false);
        TitleFadeIn.ConfigurePopup(root, rect);
        bool previousRunInBackground = Application.runInBackground;
        Application.runInBackground = true;
        int initialFrame = Time.frameCount;
        float previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        root.SetActive(true);
        bool start = root.GetComponent<CanvasGroup>().alpha == 0f && rect.anchoredPosition.y == 4f
            && root.GetComponent<CanvasGroup>().blocksRaycasts && !root.GetComponent<CanvasGroup>().interactable;
        Debug.Log("Popup initial: alpha="+root.GetComponent<CanvasGroup>().alpha+" y="+rect.anchoredPosition.y+" start="+start);
        double began = EditorApplication.timeSinceStartup;
        int cycle = 0;
        EditorApplication.CallbackFunction check = null;
        check = () =>
        {
            if (Application.isPlaying && EditorApplication.timeSinceStartup - began < .8) return;
            var group = root != null ? root.GetComponent<CanvasGroup>() : null;
            bool complete = Application.isPlaying && group != null && Mathf.Approximately(group.alpha, 1f)
                && group.interactable && Vector2.Distance(rect.anchoredPosition, new Vector2(20, 40)) < .01f;
            if (Application.isPlaying && !complete && EditorApplication.timeSinceStartup - began < 5.0) return;
            if (start && complete && cycle++ == 0)
            {
                root.SetActive(false); root.SetActive(true);
                start = group.alpha == 0f && rect.anchoredPosition.y == 4f;
                began = EditorApplication.timeSinceStartup;
                return;
            }
            EditorApplication.update -= check;
            Time.timeScale = previousTimeScale;
            Application.runInBackground = previousRunInBackground;
            string state = " alpha="+(group!=null?group.alpha:-1)+" frames="+(Time.frameCount-initialFrame)+" position="+rect.anchoredPosition+" interact="+(group!=null&&group.interactable);
            Object.Destroy(root);
            if (!start || !complete) Debug.LogError("Popup entrance validation failed: start="+start+" complete="+complete+state);
            else Debug.Log("Popup entrance passed: fade, upward motion, input blocking, timeScale=0, reopen, final position.");
        };
        EditorApplication.update += check;
    }
}
