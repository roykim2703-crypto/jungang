using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class MatchReviewUiChecks
{
    [MenuItem("Tools/Tests/Validate Review UI (Local Play Mode)")]
    public static void Run()
    {
        var match = AugmentMatch.Instance;
        if (!Application.isPlaying || match == null || match.IsReviewMode || AugmentMultiplayer.Instance.Active)
            throw new InvalidOperationException("Use a fresh local Game scene in Play Mode.");
        MatchReviewSession.BeginMatch();
        MatchReviewSession.Record("대국 시작", match.Capture());
        match.Board.cells[180] = 1;
        MatchReviewSession.Record("흑 · 중앙 착수", match.Capture());
        match.Board.cells[181] = 2;
        MatchReviewSession.Record("백 · 다음 착수", match.Capture());
        typeof(AugmentMatch).GetMethod("EnterReview", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(match, new object[] { MatchReviewSession.SnapshotAt(2) });
        var toolbar = match.GetComponent<MatchReviewToolbar>();
        var buttons = toolbar.GetComponentsInChildren<Button>();
        Button Find(string name) => buttons.First(b => b.name == name);
        Find("First").onClick.Invoke();
        Require(match.ReviewIndex == 0 && !Find("Previous").interactable && !Find("First").interactable, "first boundary");
        Find("Next").onClick.Invoke();
        Require(match.ReviewIndex == 1 && match.Board.cells[180] == 1 && match.Board.cells[181] == 0, "next snapshot");
        var slider = toolbar.GetComponentInChildren<Slider>();
        slider.value = 2;
        Require(match.ReviewIndex == 2 && match.Board.cells[181] == 2 && !Find("Next").interactable && !Find("Last").interactable, "timeline and last boundary");
        Require(toolbar.GetComponentsInChildren<TMP_Text>().First(t => t.name == "Counter").text == "3 / 3", "counter");
        Find("Previous").onClick.Invoke();
        Require(match.ReviewIndex == 1, "previous");
        Find("Last").onClick.Invoke();
        Require(match.ReviewIndex == 2, "last");
        Debug.Log("Review UI passed: first/previous/next/last, timeline, counter, snapshot restoration, disabled boundaries.");
        bool background = Application.runInBackground;
        Application.runInBackground = true;
        double start = EditorApplication.timeSinceStartup;
        bool captured = false;
        EditorApplication.CallbackFunction capture = null;
        capture = () =>
        {
            if (Application.isPlaying && !captured)
            {
                var group = toolbar.GetComponentInChildren<CanvasGroup>();
                if (group != null && !group.interactable && EditorApplication.timeSinceStartup - start < 8) return;
                foreach (var image in toolbar.GetComponentsInChildren<Image>())
                    if (image.name == "Timeline" || image.name == "Progress")
                        Debug.Log(image.name+" rect="+image.rectTransform.rect+" pos="+image.rectTransform.anchoredPosition+" color="+image.color);
                ScreenCapture.CaptureScreenshot("Temp/review-ui.png");
                captured = true; start = EditorApplication.timeSinceStartup;
                return;
            }
            if (Application.isPlaying && EditorApplication.timeSinceStartup - start < 1) return;
            EditorApplication.update -= capture;
            Application.runInBackground = background;
        };
        EditorApplication.update += capture;
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Review UI failed: " + message);
    }
}
