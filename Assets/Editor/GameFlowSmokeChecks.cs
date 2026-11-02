using System;
using System.Collections.Generic;
using System.Linq;
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
        void GradeState(int stones,int blackId=2,int whiteId=5)
        {
            Prepare();var state=match.Cards.Capture();state.stones=stones;state.round=4;
            state.black=Enumerable.Repeat(blackId,4).ToArray();state.white=Enumerable.Repeat(whiteId,4).ToArray();
            state.blackUsed=Enumerable.Repeat(true,4).ToArray();state.whiteUsed=Enumerable.Repeat(true,4).ToArray();
            match.Cards.Restore(state);
        }
        Check("Initial selection pauses the clock",()=>
        {
            match.ResetMatch(12345);match.Clock.Reset(60);
            typeof(AugmentMatch).GetMethod("UpdateClock",Private).Invoke(match,null);
            Require(match.Cards.IsChoosing&&match.Clock.blackSeconds==60,"clock ticked during draft");
            Require(!Send("place"),"placement accepted during draft");
            Require(!Send("select",2),"white selected before black");SelectBoth();
        });
        Check("Four rewards at 0/40/80/120: Early/Middle/Middle/Late",()=>
        {
            match.ResetMatch(12345);var phases=new List<CardManager.AcquisitionPhase>();var counts=new List<int>();
            for(int i=0;i<=125;i++)
            {
                if(match.Cards.IsChoosing){counts.Add(match.Cards.PlacedStoneCount);phases.Add(match.Cards.GetPhase(match.Cards.PlacedStoneCount));SelectBoth();}
                if(i<125)match.Cards.NotifyStonePlaced();
            }
            Require(string.Join(",",counts)=="0,40,80,120","wrong reward intervals");
            Require(string.Join(",",phases)=="Early,Middle,Middle,Late","wrong phases");
            Require(match.Cards.BlackInventory.Count==4&&match.Cards.WhiteInventory.Count==4&&!match.Cards.HasMoreRewards,"wrong inventory count");
            var ui=UnityEngine.Object.FindAnyObjectByType<CardSideTabUI>();
            Require(ui!=null,"side tab missing");ui.RefreshAll();
            foreach(string field in new[]{"blackSlots","whiteSlots"})
            {
                var slots=(CardSideTabUI.CardButtonSlot[])typeof(CardSideTabUI).GetField(field,Private).GetValue(ui);
                Require(slots.Length==4,field+" does not have four slots");
                foreach(var slot in slots)
                    Require(slot!=null&&slot.Root!=null&&slot.Root.activeInHierarchy&&slot.Button!=null&&slot.Artwork!=null&&slot.Artwork.enabled&&slot.Artwork.sprite!=null,field+" has an invisible or disconnected card");
            }
        });
        Check("Result scene is available for victory, defeat and review",()=>
        {
            Require(Application.CanStreamedLevelBeLoaded("Assets/Scenes/Result.unity"),"Result scene is missing from the build scene list");
        });
        Check("Effect artwork and animation frames survive asset reimport",()=>
        {
            var art=AugmentEffectView.Artwork;Require(art!=null,"effect catalog missing");
            foreach(var sprite in new[]{art.Fist,art.Hammer,art.MeteorRock,art.MeteorImpact,art.BlackHole,art.GolemBody})
                Require(sprite!=null&&sprite.texture!=null&&sprite.rect.width>0,"effect sprite missing");
            Require(art.CatapultFrames.Length==3&&art.CatapultFrames.All(s=>s!=null),"catapult animation frames missing");
            Require(art.GolemParts.Length>=15&&art.GolemParts.All(s=>s!=null),"golem summon parts missing");
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
            var guide=UnityEngine.Object.FindAnyObjectByType<가이드_돌_코드>();
            var target=new Vector3(guide.centerPos.x-8*guide.gap,guide.centerPos.y+9*guide.gap,0);
            var visuals=(List<GameObject>)typeof(AugmentMatch).GetField("visuals",Private).GetValue(match);
            var gray=visuals.Where(v=>v!=null&&Vector3.Distance(v.transform.position,target)<.01f).Select(v=>v.GetComponent<SpriteRenderer>()).FirstOrDefault();
            Require(gray!=null&&gray.sprite==guide.흰돌이미지&&gray.color==Color.gray,"gray stone is not rendered with tinted white artwork");
        });
        Check("Converted opponent stone is recorded with its actual color",()=>
        {
            Prepare(12);Require(Send("ability",id:12),"convert failed");
            Require(Send("place",x:0,y:0)&&Send("place",2,x:1,y:0),"placements failed");
            Require(match.Board.At(1,0)==1&&match.History[1].Contains("흑"),"converted stone/history mismatch");
        });
        Check("Yarinaoshi reserves six private cells and replaces the next two turns",()=>
        {
            Prepare(5);match.Board.cells[0]=match.Board.cells[2]=1;match.Board.order[0]=1;match.Board.order[2]=2;
            Require(Send("ability",id:5)&&match.Board.premoveSetup==1,"activation failed");
            Require(!Send("place",x:18,y:18),"setup bypassed");
            for(int i=0;i<6;i++)Require(Send("premove",x:3+i*2,y:5),"reservation failed");
            Require(match.History.Count==0,"reservations leaked into history");
            var peer=JsonUtility.FromJson<AugmentMatch.Snapshot>(match.CaptureForSide(2));
            Require(peer.board.blackPremoves.Length==0&&match.Board.blackPremoves.Length==6,"private queue leaked or host mutated");
            Require(Send("place",x:18,y:18),"activation turn placement failed");
            Require(Send("place",2,x:17,y:18)&&match.Board.turn==2&&match.Board.premoveIndex[1]==3,"first automatic turn failed");
            Require(match.History.Count==5&&match.Cards.PlacedStoneCount==5,"automatic stones missing from history/progress");
            Require(!Send("place",1,x:15,y:18),"extra manual turn accepted");
            Require(Send("place",2,x:16,y:18)&&match.Board.premoveIndex[1]==6,"second automatic turn failed");
            Require(match.History.Count==9&&match.Cards.PlacedStoneCount==9,"wrong final history/progress");
        });
        Check("Premove batches cannot skip a reward threshold",()=>
        {
            Prepare(5);match.Board.cells[0]=match.Board.cells[2]=1;
            match.Cards.NotifyStonesPlaced(38);
            Require(Send("ability",id:5),"activation failed");
            for(int i=0;i<6;i++)Require(Send("premove",x:3+i*2,y:5),"reservation failed");
            Require(Send("place",x:18,y:18)&&Send("place",2,x:17,y:18),"placements failed");
            Require(match.Cards.PlacedStoneCount==43&&match.Cards.IsChoosing&&match.Cards.RewardRoundCount==2,"batch skipped reward or lost stones");
        });
        Check("Occupied reservations skip a whole turn; black holes only consume their own stone",()=>
        {
            Prepare(5);match.Board.cells[0]=match.Board.cells[2]=1;
            Require(Send("ability",id:5),"activation failed");
            for(int i=0;i<6;i++)Require(Send("premove",x:3+i*2,y:5),"reservation failed");
            Require(Send("place",x:18,y:18),"manual placement failed");
            Require(Send("place",2,x:5,y:5),"blocking move failed");
            Require(match.Board.turn==2&&match.Board.premoveIndex[1]==3&&match.Board.At(3,5)==0&&match.Board.At(7,5)==0,"blocked group partially placed or allowed manual move");
            Require(match.Status.Contains("예약 묶음 취소")&&match.Cards.PlacedStoneCount==2,"blocked group feedback/progress wrong");
            match.Board.cells[5*19+11]=5;
            Require(Send("place",2,x:17,y:18),"next opponent placement failed");
            Require(match.Board.turn==2&&match.Board.At(9,5)==1&&match.Board.At(11,5)==5&&match.Board.At(13,5)==1,"black hole cancelled the group");
            Require(match.Cards.PlacedStoneCount==6&&match.History.Count==6&&match.History.Any(h=>h.Contains("블랙홀 소멸")),"vanished premove missing from history/progress");
        });
        Check("Out-of-board catapult fails without consuming a card",()=>
        {
            Prepare(9);int revision=match.Revision;
            Require(!Send("ability",x:9,y:18,id:9,power:5),"outside shot accepted");
            Require(!match.Cards.BlackInventory[0].Used&&match.Revision==revision,"failed shot consumed card/state");
            Require(Send("ability",x:9,y:18,id:9,power:1)&&match.Cards.BlackInventory[0].Used,"valid shot failed");
        });
        Check("Shotgun repeats every click for three seconds without extra card use or early placement",()=>
        {
            Prepare(1);match.Board.cells[9*19+9]=2;match.Board.cells[4*19+4]=2;match.Board.cells[4*19+5]=3;
            Require(Send("ability",id:1)&&match.Shotgun.active&&match.Shotgun.hits==1,"first hit did not start burst");
            Require(match.Board.At(9,9)==0&&match.Cards.BlackInventory[0].Used,"first hit did not clear/consume card");
            double deadline=match.Shotgun.deadline;int revision=match.Revision;
            Require(!Send("place",x:18,y:18)&&!Send("shotgun-hit",2,x:4,y:4)&&!Send("shotgun-end"),"burst accepted placement, other owner or early end");
            for(int i=0;i<4;i++)Require(match.Apply(new AugmentMatch.Command{kind="shotgun-hit",side=1,x=4,y=4,revision=revision}),"repeat click rejected by stale revision");
            Require(match.GetComponentsInChildren<AugmentEffectView>().Count(v=>v.name=="AugmentEffect_1")>=5,"overlapping punches replaced one another");
            Require(match.Board.At(4,4)==0&&match.Board.At(5,4)==3&&match.Shotgun.hits==5&&match.Shotgun.deadline==deadline,"repeat hit/window/obstacle wrong");
            Require(match.History.Count==0&&match.Cards.PlacedStoneCount==0,"punch counted as stone placement");
            var snapshot=JsonUtility.FromJson<AugmentMatch.Snapshot>(match.CaptureForSide(2));
            Require(snapshot.shotgun.active&&snapshot.shotgun.hits==5&&snapshot.shotgunRemaining>0,"peer burst missing");
            match.Shotgun.deadline=Time.realtimeSinceStartupAsDouble-1;
            Require(!Send("shotgun-hit",x:9,y:9)&&Send("shotgun-end")&&!match.Shotgun.active,"expired burst still hits");
            Require(!Send("ability",id:1)&&Send("place",x:18,y:18)&&match.Board.turn==2,"burst reused card or blocked next ordinary placement");
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
        Check("Grade finish starts at 160 stones and requires both inventories to be spent",()=>
        {
            GradeState(158);Require(Send("place",x:18,y:18)&&match.Board.winner==0,"ended before 160");
            Require(Send("place",2,x:17,y:18)&&match.Board.winner==1,"lower black grade did not win at 160");
            Require(MatchReviewSession.Detail.Contains("증강 소진"),"result omitted grade decision");
            GradeState(159);match.Cards.WhiteInventory[0].Used=false;
            Require(Send("place",x:18,y:18)&&match.Board.winner==0,"unused opponent card was ignored");
            GradeState(160);var state=match.Cards.Capture();state.blackUsed[0]=false;match.Cards.Restore(state);
            Require(Send("ability",id:2,x:8,y:8)&&match.Board.winner==1,"last ability did not trigger grade decision");
        });
        Check("Lower white grade wins, tied grades draw, and six keeps priority",()=>
        {
            GradeState(159,5,2);Require(Send("place",x:18,y:18)&&match.Board.winner==2,"lower white grade did not win");
            GradeState(159,2,2);Require(Send("place",x:18,y:18)&&match.Board.winner==3,"equal grades did not draw");
            GradeState(159,5,2);for(int x=0;x<5;x++)match.Board.cells[9*19+x]=1;
            Require(Send("place",x:5,y:9)&&match.Board.winner==1,"grade decision overrode six");
        });
        Check("Grade finish waits for shotgun and scheduled premoves to complete",()=>
        {
            GradeState(160);var state=match.Cards.Capture();state.black[0]=1;state.blackUsed[0]=false;match.Cards.Restore(state);
            Require(Send("ability",id:1)&&match.Shotgun.active&&match.Board.winner==0,"last shotgun ended before its burst");
            match.Shotgun.deadline=Time.realtimeSinceStartupAsDouble-1;
            Require(Send("shotgun-end")&&match.Board.winner==1,"shotgun completion did not end match");
            GradeState(160);match.Board.blackPremoves=Enumerable.Range(0,6).Select(i=>5*19+3+i*2).ToArray();
            Require(Send("place",x:18,y:18)&&match.Board.winner==0,"pending queue was discarded");
            Require(Send("place",2,x:17,y:18)&&match.Board.winner==0,"ended after only first group");
            Require(Send("place",2,x:16,y:18)&&match.Board.winner==1,"finished queue did not trigger grade decision");
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
