using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>Keeps the finished match and its move-by-move boards while result/review scenes change.</summary>
public static class MatchReviewSession
{
    public const string ResultScene="Assets/Scenes/Result.unity";
    [Serializable] public sealed class Frame { public string label; public string snapshot; }
    private static readonly List<Frame> frames=new List<Frame>();
    public static IReadOnlyList<Frame> Frames=>frames;
    public static int Winner { get; private set; }
    public static int ViewerSide { get; private set; }
    public static bool ReviewRequested { get; private set; }
    public static bool HasResult=>Winner!=0&&frames.Count>0;

    public static void BeginMatch()
    {
        if(ReviewRequested)return;
        frames.Clear();Winner=0;ViewerSide=0;
    }
    public static void Record(string label,string snapshot)
    {
        if(string.IsNullOrEmpty(snapshot))return;
        frames.Add(new Frame{label=string.IsNullOrEmpty(label)?$"{frames.Count+1}수":label,snapshot=snapshot});
    }
    public static void Complete(int winner,int viewerSide,string finalSnapshot)
    {
        Winner=winner;ViewerSide=viewerSide;
        if(frames.Count==0)Record("최종 결과",finalSnapshot);
        else if(frames[frames.Count-1].snapshot!=finalSnapshot)Record("대국 종료",finalSnapshot);
    }
    public static string Heading
    {
        get
        {
            if(Winner==3)return "무승부";
            if(ViewerSide==0)return "승리";
            return Winner==ViewerSide?"승리":"패배";
        }
    }
    public static string Detail=>Winner==3?"양쪽이 비겼습니다":$"{(Winner==1?"흑":"백")} 승리";
    public static void RequestReview(){if(frames.Count==0)return;ReviewRequested=true;SceneManager.LoadSceneAsync(AugmentMultiplayer.GameScene);}
    public static bool TryBeginReview(out string snapshot)
    {
        snapshot=null;if(!ReviewRequested||frames.Count==0)return false;
        ReviewRequested=false;snapshot=frames[frames.Count-1].snapshot;return true;
    }
    public static string SnapshotAt(int index)=>index>=0&&index<frames.Count?frames[index].snapshot:null;
    public static void Clear(){frames.Clear();Winner=ViewerSide=0;ReviewRequested=false;}
}
