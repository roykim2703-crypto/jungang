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
        match.Apply(new AugmentMatch.Command{kind=kind,side=side,x=x,y=y,id=id,dx=0,dy=-1,power=power,revision=match.Revision,draftRound=match.Cards.RewardRoundCount});
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
        Check("Online draft is simultaneous, private and accepts concurrent revisions only in its round",()=>
        {
            var room=AugmentMultiplayer.Instance;var active=typeof(AugmentMultiplayer).GetField("active",Private);
            try
            {
                active.SetValue(room,true);match.ResetMatch(12345);
                Require(match.LocalSide==2&&match.Cards.SimultaneousSelection,"online perspective unavailable");
                var ui=UnityEngine.Object.FindAnyObjectByType<CardAcquisitionUI>(UnityEngine.FindObjectsInactive.Include);
                Require(ui.WhiteGroup.activeSelf&&!ui.BlackGroup.activeSelf,"opponent draft displayed");
                Require(ui.BlackSlots.All(s=>s.Title.text==""&&s.Artwork.sprite==null),"opponent offer retained in hidden UI");
                var black=JsonUtility.FromJson<AugmentMatch.Snapshot>(match.CaptureForSide(1));
                var white=JsonUtility.FromJson<AugmentMatch.Snapshot>(match.CaptureForSide(2));
                Require(black.cards.whiteOffers.Length==0&&black.cards.blackOffers.Length==3&&white.cards.blackOffers.Length==0&&white.cards.whiteOffers.Length==3,"offer IDs leaked");
                Require(black.cards.seed==0&&white.cards.seed==0,"draw seed leaked");
                var concurrent=new AugmentMatch.Command{kind="select",side=1,id=1,revision=match.Revision,draftRound=1};
                Require(Send("select",2,id:2),"white could not select first");
                Require(match.Cards.IsChoosing&&match.Cards.HasSelected(false)&&!match.Cards.HasSelected(true),"draft ended before both choices");
                Require(ui.WhiteSlots.All(s=>!s.Button.interactable)&&ui.WhiteStatus.text.Contains("기다리는"),"selected player not waiting");
                black=JsonUtility.FromJson<AugmentMatch.Snapshot>(match.CaptureForSide(1));
                Require(black.cards.white.SequenceEqual(new[]{-1})&&black.cards.whiteUsed.All(v=>!v),"acquired card leaked");
                Require(match.Apply(concurrent)&&!match.Cards.IsChoosing,"concurrent revision rejected");
                Require(!match.Apply(concurrent),"duplicate choice accepted");
                string projected=match.CaptureForSide(2);match.WaitForNetwork();match.Restore(projected);
                Require(match.Cards.BlackInventory[0].Name==-1&&match.Cards.WhiteInventory[0].Name>=0,"private inventory restore failed");
                var tabs=UnityEngine.Object.FindAnyObjectByType<CardSideTabUI>();tabs.RefreshAll();
                var slots=(CardSideTabUI.CardButtonSlot[])typeof(CardSideTabUI).GetField("blackSlots",Private).GetValue(tabs);
                Require(slots[0].Title.text=="비공개 증강"&&slots[0].Artwork.sprite==null&&!slots[0].Button.interactable,"opponent card visible in side tab");
                match.ResetMatch(12345);Require(Send("select",2)&&Send("select",1),"second setup failed");
                match.Cards.NotifyStonesPlaced(30);
                Require(match.Cards.IsChoosing&&match.Cards.RewardRoundCount==2,"next round missing");
                concurrent.revision=match.Revision;
                Require(!match.Apply(concurrent),"previous round selection accepted in next draft");
            }
            finally{active.SetValue(room,false);match.ResetMatch(12345);}
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
        Check("Normalized half-step grades determine all three acquisition pools",()=>
        {
            Prepare();
            var expected=new Dictionary<int,float>{{14,5},{17,4.5f},{3,4.5f},{12,4.5f},{13,3.5f},{15,3.5f},{1,3.5f},{19,4},{20,4.5f},{8,3},{6,2.5f},{2,2.5f},{16,3},{4,1},{11,2},{10,1.5f},{9,3}};
            foreach(var entry in expected)Require(match.Cards.CardList.Find(c=>c.Name==entry.Key).Value==entry.Value,"wrong grade for "+entry.Key);
            foreach(var phase in new[]{CardManager.AcquisitionPhase.Early,CardManager.AcquisitionPhase.Middle,CardManager.AcquisitionPhase.Late})
            {
                bool Eligible(CardManager.CardData c)=>phase==CardManager.AcquisitionPhase.Early?c.Value>=1&&c.Value<3:phase==CardManager.AcquisitionPhase.Middle?c.Value>=2&&c.Value<4:c.Value>=2;
                foreach(var card in match.Cards.CardList)
                {
                    Require(card.Value*2==Mathf.Round(card.Value*2),"non-half grade remained");
                    Require((card.AvailableFrom<=phase&&phase<=card.AvailableUntil)==Eligible(card),"wrong pool for "+card.Name);
                }
                for(int draw=0;draw<20;draw++)
                {
                    Require(match.Cards.TryDrawOffers(phase,0,match.Cards.CardList.Count,out var black,out var white),"fewer than six candidates");
                    Require(black.Concat(white).All(Eligible)&&black.Concat(white).Select(c=>c.Name).Distinct().Count()==6,"invalid or duplicate offer");
                }
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
        Check("Both flip results and removed cells reach the peer and survive later moves",()=>
        {
            for(int roll=0;roll<2;roll++)
            {
                Prepare(20);match.Board.cells[0]=match.Board.cells[2]=match.Board.cells[4]=1;match.Board.cells[19]=2;
                int seed=0;while(new System.Random(seed).Next(0,2)!=roll)seed++;
                typeof(CardManager).GetField("random",Private).SetValue(match.Cards,new System.Random(seed));
                Require(Send("ability",id:20),"flip failed");
                var host=match.CardOutcomes.Last();
                Require(host.result.Contains(roll==0?"앞면":"뒷면"),"incorrect face text");
                Require(host.removed.Length==(roll==0?0:3),"incorrect removal coordinates");
                Require(Send("place",x:18,y:18),"following move failed");
                string json=match.CaptureForSide(2);var peer=JsonUtility.FromJson<AugmentMatch.Snapshot>(json);
                Require(peer.effect.result==host.result&&peer.outcomes.Last().result==host.result,"peer outcome differs");
                Require(peer.effect.removed.SequenceEqual(host.removed),"peer markers differ");
                Require(peer.effectRevision<peer.revision,"test did not advance beyond effect revision");
                match.WaitForNetwork();match.Restore(json);
                Require(match.CardOutcomes.Last().result==host.result,"restored result missing");
            }
        });
        Check("Public results preserve secret traps and destroyed hidden-stone locations",()=>
        {
            foreach(int id in new[]{8,12,18,16})
            {
                Prepare(id);Require(Send("ability",id:id),"secret ability failed");
                var peer=JsonUtility.FromJson<AugmentMatch.Snapshot>(match.CaptureForSide(2));
                Require((peer.effect==null||string.IsNullOrEmpty(peer.effect.kind))&&peer.outcomes.Length==0,"secret activation leaked");
            }
            Prepare(1);int hidden=9*19+9,visible=9*19+10;
            match.Board.cells[hidden]=1;match.Board.hidden[hidden]=1;match.Board.cells[visible]=2;
            Require(Send("ability",id:1),"shotgun failed");
            var outcome=match.CardOutcomes.Last();
            Require(outcome.removed.SequenceEqual(new[]{visible}),"hidden stone leaked in public markers");
            Require(!outcome.changedCells.Contains(hidden),"hidden stone leaked in public motion fragments");
            var publicState=JsonUtility.FromJson<AugmentMatch.Snapshot>(match.CaptureForSide(2));
            Require(publicState.outcomes.Last().removed.SequenceEqual(new[]{visible}),"peer marker privacy differs");
        });
        Check("Movement results do not label vacated cells as destroyed stones",()=>
        {
            Prepare(17);match.Board.cells[9*19+9]=1;
            Require(Send("ability",id:17),"move ability failed");
            Require(match.CardOutcomes.Last().removed.Length==0&&match.CardOutcomes.Last().result.Contains("이동 완료"),"movement reported as destruction");
        });
        Check("Every public ability exposes the same committed motion from either player",()=>
        {
            foreach(int side in new[]{1,2})foreach(int id in new[]{1,2,3,4,5,6,9,10,11,13,14,15,17,19,20})
            {
                Prepare(id);match.Board.turn=side;
                var cards=match.Cards.Capture();cards.black=cards.white=new[]{id};cards.blackUsed=new[]{false};cards.whiteUsed=new[]{false};cards.turnBlack=side==1;match.Cards.Restore(cards);
                if(id==3||id==17)match.Board.cells[180]=side;
                if(id==5){match.Board.cells[0]=match.Board.cells[2]=side;match.Board.order[2]=2;}
                if(id==11)
                {
                    Require(Send("meteor-start",side),"meteor start failed");match.Meteor.deadline=Time.realtimeSinceStartupAsDouble-1;
                    Require(Send("meteor-impact",side),"meteor impact failed");
                }
                else Require(Send("ability",side,id:id),$"ability {id} failed for {side}");
                string projected=match.CaptureForSide(3-side);
                var peer=JsonUtility.FromJson<AugmentMatch.Snapshot>(projected);
                Require(peer.effect.id==id&&peer.effect.side==side&&peer.effect.kind=="effect",$"ability {id} motion missing");
                Require(peer.effect.changedCells.SequenceEqual(match.CardOutcomes.Last().changedCells),$"ability {id} delta differs");
                Require(match.GetComponentsInChildren<AugmentEffectView>().Any(v=>v.name=="AugmentEffect_"+id),$"ability {id} did not spawn its motion");
                match.ResetMatch(12345);match.WaitForNetwork();match.Restore(projected);
                Require(match.GetComponentsInChildren<AugmentEffectView>().Any(v=>v.name=="AugmentEffect_"+id&&v.IsAbilityMotion),$"ability {id} did not play on observer");
            }
        });
        Check("Snapshot catch-up plays every committed shotgun hit once without aim previews",()=>
        {
            Prepare(1);Require(Send("ability",id:1),"initial punch failed");
            Require(Send("shotgun-hit",x:3,y:5)&&Send("shotgun-hit",x:13,y:12),"repeat hits failed");
            string json=match.CaptureForSide(2);match.ResetMatch(12345);match.WaitForNetwork();match.Restore(json);
            int CountFists()=>match.GetComponentsInChildren<AugmentEffectView>().Count(v=>v.name=="AugmentEffect_1");
            Require(CountFists()==3,"catch-up lost a punch");
            var target=(AugmentTargetView)typeof(AugmentMatch).GetField("targetView",Private).GetValue(match);
            Require(target.GetComponentsInChildren<SpriteRenderer>().All(s=>!s.gameObject.activeInHierarchy),"observer received an aim range");
            match.Restore(json);Require(CountFists()==3,"same snapshot duplicated punches");
        });
        Check("Meteor aim stays private until the committed impact is broadcast",()=>
        {
            Prepare(11);Require(Send("meteor-start",x:5,y:6),"charge failed");match.Meteor.clicks=13;
            var owner=JsonUtility.FromJson<AugmentMatch.Snapshot>(match.CaptureForSide(1));
            var observer=JsonUtility.FromJson<AugmentMatch.Snapshot>(match.CaptureForSide(2));
            Require(owner.meteor.x==5&&owner.meteor.y==6&&owner.meteor.clicks==13,"owner charge masked");
            Require(observer.meteor.x==-1&&observer.meteor.y==-1&&observer.meteor.clicks==0,"charge aim leaked");
            Require(observer.outcomes.Length==0,"charge played impact early");
            match.Meteor.deadline=Time.realtimeSinceStartupAsDouble-1;Require(Send("meteor-impact"),"impact failed");
            observer=JsonUtility.FromJson<AugmentMatch.Snapshot>(match.CaptureForSide(2));
            Require(observer.effect.id==11&&observer.effect.x==5&&observer.effect.y==6&&observer.effect.power==3,"impact motion missing final target/power");
        });
        Check("Placement cues synchronize once and invalid clicks preserve the latest move",()=>
        {
            Prepare();Require(Send("place",x:9,y:9),"placement failed");
            var state=JsonUtility.FromJson<AugmentMatch.Snapshot>(match.Capture());
            Require(state.placements.Length==1&&state.placements[0].cell==180&&state.placements[0].color==1,"wrong landing cue");
            Require(state.placements[0].stamp==match.Board.order[180],"marker not linked to stone");
            Require(!Send("place",2,x:9,y:9),"occupied placement accepted");
            Require(JsonUtility.FromJson<AugmentMatch.Snapshot>(match.Capture()).placementRevision==state.placementRevision,"failed click changed marker");
            string json=match.CaptureForSide(2);match.ResetMatch(12345);match.WaitForNetwork();match.Restore(json);
            Require(JsonUtility.FromJson<AugmentMatch.Snapshot>(match.Capture()).placements[0].cell==180,"peer marker differs");
            int CountPulses()=>match.GetComponentsInChildren<AugmentEffectView>().Count(v=>v.gameObject.name=="StonePlacementPulse");
            int count=CountPulses();Require(count==1,"peer did not play landing pulse");
            match.Restore(json);Require(CountPulses()==count,"clock sync replayed landing pulse");
        });
        Check("Black-hole placement records a move, remains playable, and synchronizes victory",()=>
        {
            Prepare();match.Board.cells[180]=5;
            Require(Send("place"),"black-hole placement rejected");
            Require(match.Board.At(9,9)==5&&match.Board.turn==2&&match.Cards.PlacedStoneCount==1,"swallowed move did not consume turn/progress");
            Require(match.History.Last().Contains("블랙홀 소멸"),"swallowed move missing from history");
            var peer=JsonUtility.FromJson<AugmentMatch.Snapshot>(match.CaptureForSide(2));
            Require(peer.board.At(9,9)==5&&peer.placements[0].cell==180,"peer landing/terrain differs");
            Prepare();for(int x=0;x<5;x++)match.Board.cells[9*19+x]=1;match.Board.cells[9*19+5]=5;
            Require(Send("place",x:5,y:9)&&match.Board.winner==1,"black-hole six did not win");
            peer=JsonUtility.FromJson<AugmentMatch.Snapshot>(match.CaptureForSide(2));
            Require(peer.board.winner==1&&peer.board.At(5,9)==1,"winning placement missing from peer result");
            Prepare();Array.Fill(match.Board.cells,3);match.Board.cells[180]=5;
            Require(Send("place")&&match.Board.winner==0,"playable black hole caused a full-board finish");
        });
        Check("Premove landing cues include all three cells and hidden failures do not fake placement",()=>
        {
            Prepare();match.Board.blackPremoves=new[]{0,2,4,38,40,42};match.Board.turn=2;
            Require(Send("place",2,x:18,y:18),"premove trigger failed");
            var state=JsonUtility.FromJson<AugmentMatch.Snapshot>(match.CaptureForSide(2));
            Require(state.placements.Select(c=>c.cell).SequenceEqual(new[]{360,0,2,4}),"batch omitted a landing");
            Prepare(16);Require(Send("ability",id:16),"hidden placement failed");
            var hidden=JsonUtility.FromJson<AugmentMatch.Snapshot>(match.CaptureForSide(2));
            Require(hidden.placements.Length==0&&hidden.placementRevision==0,"hidden position leaked via landing cue");
            match.Board.turn=2;Require(Send("place",2),"hidden collision failed");
            Require(JsonUtility.FromJson<AugmentMatch.Snapshot>(match.Capture()).placements.Length==0,"failed placement played a landing cue");
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
            match.Cards.NotifyStonesPlaced(28);
            Require(Send("ability",id:5),"activation failed");
            for(int i=0;i<6;i++)Require(Send("premove",x:3+i*2,y:5),"reservation failed");
            Require(Send("place",x:18,y:18)&&Send("place",2,x:17,y:18),"placements failed");
            Require(match.Cards.PlacedStoneCount==33&&match.Cards.IsChoosing&&match.Cards.RewardRoundCount==2,"batch skipped reward or lost stones");
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
        Check("Catapult release uses its oscillating preview and shares the fixed landing and flight",()=>
        {
            Prepare(9);match.ChooseAbility(true,9);
            var guide=UnityEngine.Object.FindAnyObjectByType<가이드_돌_코드>();var pointer=guide.centerPos+Vector3.up*guide.gap*7;
            var handle=typeof(AugmentMatch).GetMethod("HandlePointer",Private);
            handle.Invoke(match,new object[]{pointer,true,false,false});
            foreach(var sample in new[]{new Vector2(.18f,3),new Vector2(.38f,5),new Vector2(.62f,3)})
            {
                typeof(AugmentMatch).GetField("catapultChargeStarted",Private).SetValue(match,Time.unscaledTime-sample.x);
                handle.Invoke(match,new object[]{pointer,false,false,false});
                Require((int)typeof(AugmentMatch).GetField("power",Private).GetValue(match)==(int)sample.y,"charge did not oscillate");
            }
            match.Board.cells[6*19+10]=2;match.Board.hidden[6*19+10]=2;
            handle.Invoke(match,new object[]{pointer,false,true,false});
            Require(match.Board.At(9,6)==1&&match.Board.IsAnchored(9,6),"release missed displayed landing");
            var effect=match.CardOutcomes.Last();Require(effect.power==3&&effect.dy<0,"flight lost released aim");
            Require(!effect.changedCells.Contains(6*19+11),"pushed hidden stone leaked into motion data");
            string json=match.CaptureForSide(2);match.ResetMatch(12345);match.WaitForNetwork();match.Restore(json);
            Require(match.Board.IsAnchored(9,6)&&match.Board.At(9,6)==1,"peer lost absolute stone");
            Require(match.GetComponentsInChildren<AugmentEffectView>().Any(v=>v.name=="AugmentEffect_9"&&v.IsAbilityMotion),"peer flight not played");
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
