using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>Opt-in integration checks against the running Game scene, including its real card UI.</summary>
public static class GameFlowSmokeChecks
{
    private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    private static AugmentMatch match;
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    private static bool Send(string kind,int side=1,int x=9,int y=9,int id=0,int power=1)=>
        match.Apply(new AugmentMatch.Command{kind=kind,side=side,x=x,y=y,id=id,dx=0,dy=-1,power=power,revision=match.Revision});
    private static void SelectBoth()
    {
        Require(Send("select",1,id:0),"black selection failed");
        Require(Send("select",2,id:0),"white selection failed");
    }
    private static void Prepare(params int[] cards)
    {
        match.ResetMatch(12345);SelectBoth();
        var state=match.Cards.Capture();state.black=cards;state.blackUsed=new bool[cards.Length];state.used=false;
        match.Cards.Restore(state);
    }
    [MenuItem("Tools/Tests/Run Game Flow Checks (Play Mode)")]
    public static void RunMenu()=>Debug.Log(Run());
    public static string Run()
    {
        if(!EditorApplication.isPlaying||AugmentMatch.Instance==null)throw new InvalidOperationException("Run in Play Mode with Game scene open.");
        if(AugmentMultiplayer.Instance.Active)throw new InvalidOperationException("Use a local test match.");
        match=AugmentMatch.Instance;var results=new List<string>();
        void Check(string name,Action test)
        {
            try{test();results.Add("PASS "+name);}catch(Exception e){results.Add("FAIL "+name+": "+e.Message);}
        }
        Check("Initial selection pauses the clock",()=>
        {
            match.ResetMatch(12345);match.Clock.Reset(60);
            typeof(AugmentMatch).GetMethod("UpdateClock",Private).Invoke(match,null);
            Require(match.Cards.IsChoosing&&match.Clock.blackSeconds==60,"clock ticked during draft");
            Require(!Send("place"),"placement accepted during draft");
            Require(!Send("select",2),"white selected before black");SelectBoth();
        });
        Check("Four rewards at 0/30/60/90: Early/Middle/Middle/Late",()=>
        {
            match.ResetMatch(12345);var phases=new List<CardManager.AcquisitionPhase>();var counts=new List<int>();
            for(int i=0;i<=95;i++)
            {
                if(match.Cards.IsChoosing){counts.Add(match.Cards.PlacedStoneCount);phases.Add(match.Cards.GetPhase(match.Cards.PlacedStoneCount));SelectBoth();}
                if(i<95)match.Cards.NotifyStonePlaced();
            }
            Require(string.Join(",",counts)=="0,30,60,90","wrong reward intervals");
            Require(string.Join(",",phases)=="Early,Middle,Middle,Late","wrong phases");
            Require(match.Cards.BlackInventory.Count==4&&match.Cards.WhiteInventory.Count==4&&!match.Cards.HasMoreRewards,"wrong inventory count");
        });
        Check("Wrong-turn, duplicate, stale and invalid commands do not advance state",()=>
        {
            Prepare();Require(!match.Apply(null),"null accepted");Require(!Send("place",3),"invalid side accepted");
            Require(!Send("place",2),"wrong turn accepted");Require(Send("place",1),"black place failed");int revision=match.Revision;
            Require(!Send("place",2),"occupied cell accepted");
            Require(!match.Apply(new AugmentMatch.Command{kind="place",side=2,x=10,y=9,revision=revision-1}),"stale accepted");
            Require(match.Revision==revision&&match.Board.turn==2,"invalid command changed turn");
        });
        Check("One card per turn and gray trap resolves on opponent placement",()=>
        {
            Prepare(8,12);Require(Send("ability",id:8),"gray ability failed");Require(!Send("ability",id:12),"second ability accepted");
            Require(Send("place",x:0,y:0)&&Send("place",2,x:1,y:0),"placements failed");
            Require(match.Board.At(1,0)==4&&match.Board.grayNext==0&&match.Board.noSix==1,"gray trap did not resolve");
            Require(match.History[1].Contains("회색"),"gray move mislabeled");
        });
        Check("Converted opponent stone is recorded with its actual color",()=>
        {
            Prepare(12);Require(Send("ability",id:12),"convert failed");
            Require(Send("place",x:0,y:0)&&Send("place",2,x:1,y:0),"placements failed");
            Require(match.Board.At(1,0)==1&&match.History[1].Contains("흑"),"converted stone/history mismatch");
        });
        Check("Replay recalls all four cells of a large stone and preserves turn",()=>
        {
            Prepare(5);match.Board.Ability(6,8,8,1,0,1,(a,b)=>a);match.Board.cells[0]=1;
            Require(Send("ability",id:5)&&match.Board.replay==5,"wrong replay count");
            for(int i=0;i<5;i++){Require(Send("place",x:i*2,y:2),"replay placement failed");Require(match.Board.turn==(i<4?1:2),"replay turn changed too early/late");}
            Require(match.History.Count==5&&match.Board.replay==0,"wrong replay history");
        });
        Check("Out-of-board catapult fails without consuming a card",()=>
        {
            Prepare(9);int revision=match.Revision;
            Require(!Send("ability",x:9,y:18,id:9,power:5),"outside shot accepted");
            Require(!match.Cards.BlackInventory[0].Used&&match.Revision==revision,"failed shot consumed card/state");
            Require(Send("ability",x:9,y:18,id:9,power:1)&&match.Cards.BlackInventory[0].Used,"valid shot failed");
        });
        Check("Winning ability consumes its card and creates a result",()=>
        {
            Prepare(2);for(int x=0;x<5;x++)match.Board.cells[9*19+x]=1;
            Require(Send("ability",x:5,y:9,id:2),"winning ability rejected");
            Require(match.Board.winner==1&&match.Cards.BlackInventory[0].Used&&MatchReviewSession.HasResult,"winning ability not committed atomically");
        });
        Check("Meteor charge cancellation is owner-only and does not consume card",()=>
        {
            Prepare(11);Require(Send("meteor-start"),"meteor start failed");
            Require(!Send("place")&&!Send("meteor-tap",2)&&!Send("meteor-cancel",2),"meteor accepted another input/owner");
            Require(Send("meteor-tap")&&Send("meteor-cancel"),"owner cannot tap/cancel");
            Require(!match.Meteor.active&&!match.Cards.BlackInventory[0].Used,"cancel consumed meteor");
        });
        Check("Clock-only snapshots preserve aiming and older snapshots cannot roll back",()=>
        {
            Prepare(9);typeof(AugmentMatch).GetField("selected",Private).SetValue(match,9);typeof(AugmentMatch).GetField("dragging",Private).SetValue(match,true);
            match.Restore(match.Capture());
            Require((int)typeof(AugmentMatch).GetField("selected",Private).GetValue(match)==9&&(bool)typeof(AugmentMatch).GetField("dragging",Private).GetValue(match),"clock sync cancelled aim");
            string old=match.Capture();Require(Send("place",x:1,y:1),"place failed");int revision=match.Revision;match.Restore(old);
            Require(match.Revision==revision&&match.Board.At(1,1)==1,"stale snapshot rolled back board");
        });
        Check("Peer snapshots hide traps without mutating the host",()=>
        {
            Prepare(8);Require(Send("ability",id:8),"trap failed");match.Board.cells[0]=1;match.Board.hidden[0]=1;
            var peer=JsonUtility.FromJson<AugmentMatch.Snapshot>(match.CaptureForSide(2));
            Require(peer.board.grayNext==0&&peer.board.cells[0]==0&&!peer.cards.blackUsed[0],"private data leaked");
            Require(match.Board.grayNext==2&&match.Board.cells[0]==1&&match.Cards.BlackInventory[0].Used,"projection mutated host");
        });
        Check("Hidden stone is translucent to owner, absent to opponent, then revealed on failed placement",()=>
        {
            Prepare(16);Require(Send("ability",id:16),"hidden stone failed");
            Require(match.History.Count==0&&MatchReviewSession.Frames.Count==0,"hidden placement leaked into history");
            var g=UnityEngine.Object.FindAnyObjectByType<가이드_돌_코드>();
            var target=new Vector3(g.centerPos.x,g.centerPos.y,0);
            SpriteRenderer FindPiece()
            {
                var visuals=(List<GameObject>)typeof(AugmentMatch).GetField("visuals",Private).GetValue(match);
                foreach(var visual in visuals)if(visual!=null&&Vector3.Distance(visual.transform.position,target)<.01f)return visual.GetComponent<SpriteRenderer>();
                return null;
            }
            var ownPiece=FindPiece();Require(ownPiece!=null&&Mathf.Abs(ownPiece.color.a-.45f)<.01f,"owner stone is not translucent");
            Require(Send("place",x:0,y:0),"owner placement failed");
            Require(FindPiece()==null&&match.Board.PreviewLegal(9,9,2,false,match.ViewingSide),"opponent can see the hidden stone or ban");
            int history=match.History.Count,stones=match.Cards.PlacedStoneCount;
            Require(Send("place",2),"hidden collision not accepted");
            Require(match.Board.At(9,9)==1&&match.Board.hidden[9*19+9]==0&&match.Board.turn==1,"collision did not reveal/pass turn");
            Require(match.History.Count==history&&match.Cards.PlacedStoneCount==stones,"failed placement counted as a new stone");
            var revealed=FindPiece();Require(revealed!=null&&revealed.color.a==1f,"revealed stone still invisible/translucent");
            var peer=JsonUtility.FromJson<AugmentMatch.Snapshot>(match.CaptureForSide(2));
            Require(peer.board.cells[9*19+9]==1&&peer.hiddenStoneRevealed,"reveal did not reach peer snapshot");
        });
        Check("Timeout must be expired and on the active side",()=>
        {
            Prepare();Require(!Send("timeout"),"unexpired timeout accepted");match.Clock.whiteSeconds=0;Require(!Send("timeout",2),"inactive timeout accepted");
            match.Clock.blackSeconds=0;Require(Send("timeout")&&match.Board.winner==2,"expired timeout did not finish match");
        });
        Check("Surrender freezes the finished board",()=>
        {
            Prepare();Require(Send("surrender",2)&&match.Board.winner==1,"surrender failed");int revision=match.Revision;
            Require(!Send("place")&&match.Revision==revision,"finished match accepted input");
        });
        match.ResetMatch(12345);
        return string.Join("\n",results);
    }
}
