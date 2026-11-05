using System;
using System.Reflection;
using UnityEngine;

/// <summary>Targeted checks for preview-only selection, confirmation and stale selection.</summary>
public static class MobilePlacementChecks
{
    private static void Require(bool condition,string message)
    {if(!condition)throw new InvalidOperationException(message);}
    public static string Run()
    {
        var match=AugmentMatch.Instance;
        Require(Application.isPlaying&&match!=null,"Open a local Game scene in Play Mode.");
        Require(!AugmentMultiplayer.Instance.Active,"Use a local match.");
        var property=typeof(AugmentMatch).GetProperty(nameof(AugmentMatch.UsesPlacementConfirmation));
        bool original=match.UsesPlacementConfirmation;
        try
        {
            property.SetValue(match,true);
            match.ResetMatch(12345);match.RequestSelection(true,0);match.RequestSelection(false,0);
            var guide=UnityEngine.Object.FindAnyObjectByType<가이드_돌_코드>();
            var pointer=typeof(AugmentMatch).GetMethod("HandlePointer",BindingFlags.Instance|BindingFlags.NonPublic);
            void Tap(int x,int y)
            {
                var world=guide.centerPos+new Vector3((x-9)*guide.gap,(9-y)*guide.gap,0);
                pointer.Invoke(match,new object[]{world,true,false,false});
            }
            int revision=match.Revision,history=match.History.Count;
            Tap(0,9);
            Require(match.CanConfirmPlacement&&match.Board.At(0,9)==0,"Tap must only select a position.");
            Tap(1,9);
            Require(match.PendingPlacementCell==new Vector2Int(1,9),"Second tap must move the preview.");
            Require(match.Revision==revision&&match.History.Count==history,"Selection changed match state.");
            Require(match.ConfirmPlacement()&&match.Board.At(1,9)==1,"Confirmation did not place the stone.");
            Require(!match.ConfirmPlacement()&&match.History.Count==history+1,"Double confirmation placed another stone.");
            Tap(0,10);Require(match.HasPendingPlacement,"White selection missing.");
            Require(match.Apply(new AugmentMatch.Command{kind="place",side=2,x=2,y=9,revision=match.Revision}),"State-change setup failed.");
            Require(!match.HasPendingPlacement&&!match.CanConfirmPlacement,"Stale selection remained after state changed.");
            var stars=match.transform.Find("BoardStarPoints");
            Require(stars!=null&&stars.childCount==9,"Nine star points were not created.");
            foreach(Transform star in stars)Require(star.GetComponent<Collider2D>()==null,"Star point must not intercept input.");
            return "PASS: nine star points; preview and reselection do not place; confirmation places once; state changes clear selection.";
        }
        finally{property.SetValue(match,original);}
    }
}
