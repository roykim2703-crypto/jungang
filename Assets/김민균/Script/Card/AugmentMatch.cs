using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

/// <summary>One input path for local play and host-validated multiplayer.</summary>
public sealed class AugmentMatch : MonoBehaviour
{
    public static AugmentMatch Instance { get; private set; }
    public bool ApplyingCommand { get; private set; }
    public CardManager Cards { get; private set; }
    public AugmentBoard Board { get; private set; } = new AugmentBoard();
    public int Revision { get; private set; }
    public IReadOnlyList<string> History => history;
    public bool IsReviewMode { get; private set; }
    public bool IsInitialized { get; private set; }
    public int ReviewIndex { get; private set; }=-1;
    public bool Waiting;
    public string Status = "";
    private 가이드_돌_코드 guide;
    private Vector3 guidePreviewScale=Vector3.one;
    private readonly List<GameObject> visuals = new List<GameObject>();
    private readonly GameObject[] stoneVisuals = new GameObject[361];
    private readonly List<string> history = new List<string>();
    private AugmentMultiplayer multiplayer;
    private int selected = -1, dx=1,dy, power=1;
    private bool pieceMove;
    private Vector3 shotgunAim;
    private GUIStyle guiStyle;
    private Command lastEffect;
    private int effectRevision;
    private readonly List<Command> outcomes=new List<Command>();
    public IReadOnlyList<Command> CardOutcomes=>outcomes;
    private PlacementCue[] lastPlacements=Array.Empty<PlacementCue>();
    private int placementRevision;
    [Serializable] public class PlacementCue { public int cell,color,stamp; public int size=1; public bool stolen; }
    private AugmentTargetView targetView;
    private bool dragging;
    private Vector2 pointerScreenPosition;
    private bool pointerAvailable,pointerIsTouch,pointerHeld;
    private float ignoreMouseUntil;
    private readonly List<RaycastResult> pointerHits=new List<RaycastResult>();
    private PointerEventData pointerEvent;
    private EventSystem pointerEventSystem;
    private Vector2Int dragOrigin;
    private float catapultChargeStarted;
    private float stackStarted;
    private StackStoneView stackView;
    private MysteryBoxView mysteryBoxView;
    private SpriteRenderer catapultPreview;
    public MeteorCharge Meteor { get; private set; } = new MeteorCharge();
    public ShotgunBurst Shotgun { get; private set; } = new ShotgunBurst();
    public MatchClock Clock { get; private set; } = new MatchClock();
    private TMP_Text blackClockLabel,whiteClockLabel;
    private float clockBroadcastAccumulator;
    private bool resultQueued;
    private bool lastCommandRevealedHiddenStone;
    private static double Now => Time.realtimeSinceStartupAsDouble;
    public int LocalSide => multiplayer != null && multiplayer.Active ? (multiplayer.IsHost?1:2) : 0;
    public int ViewingSide=>LocalSide!=0?LocalSide:Board.takeover==Board.turn?3-Board.turn:Board.turn;
    private bool IsBoxOpening=>mysteryBoxView!=null&&mysteryBoxView.IsPlaying;
    private bool IsScreenEffectPlaying=>AbilityScreenView.IsAnimating(transform);
    public bool CanControl(int side) => !IsReviewMode&&!Waiting&&!IsBoxOpening&&!IsScreenEffectPlaying&&!AugmentEffectView.IsGolemSummoning(transform)&&!AugmentEffectView.IsSorting(transform)&&!AugmentEffectView.IsCapturing(transform) && (LocalSide==0||LocalSide==side);
    [Serializable] public class Command { public string kind; public int side,id,x,y,dx,dy,power,revision,draftRound,coinFace; public string result; public int[] removed,changedCells,beforeColors,afterColors; public AugmentBoard.SortMotion[] sortMotions; public AugmentBoard.CapturedStone[] captures; public AugmentBoard.StoneConversion[] conversions; public bool fromMysteryBox; }
    [Serializable] public class Snapshot { public AugmentBoard board; public CardManager.MatchCards cards; public int revision; public string status; public string[] history; public Command effect; public Command[] outcomes; public PlacementCue[] placements; public int placementRevision; public int effectRevision; public MeteorCharge meteor; public double meteorRemaining; public ShotgunBurst shotgun; public double shotgunRemaining; public MatchClock clock; public bool hiddenStoneRevealed; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded; SceneManager.sceneLoaded += OnSceneLoaded;
        Attach();
    }
    private static void OnSceneLoaded(Scene scene,LoadSceneMode mode)=>Attach();
    private static void Attach()
    {
        var manager=FindAnyObjectByType<CardManager>();
        if(manager!=null&&manager.GetComponent<AugmentMatch>()==null)manager.gameObject.AddComponent<AugmentMatch>();
    }
    private void Awake()
    {
        Instance=this; Cards=GetComponent<CardManager>();
        guide=FindAnyObjectByType<가이드_돌_코드>();
        if(guide!=null)guidePreviewScale=guide.transform.localScale;
        var turns=FindAnyObjectByType<수_놓기>();
        // Copy board geometry before stopping the former placement loop.
        if(turns!=null){turns.이제너의턴();turns.StopAllCoroutines();turns.enabled=false;}
        if(guide!=null){guide.StopAllCoroutines();guide.enabled=false;guide.GetComponent<SpriteRenderer>().enabled=false;}
        var debugInput=FindAnyObjectByType<테스트용>();if(debugInput!=null)debugInput.enabled=false;
        var bridge=GetComponent<CardMatchBridge>();if(bridge!=null){bridge.StopAllCoroutines();bridge.enabled=false;}
        multiplayer=AugmentMultiplayer.Instance;multiplayer.Attach(this);
        var targetRoot=new GameObject("AugmentTargetPreview");targetRoot.transform.SetParent(transform,false);
        targetView=targetRoot.AddComponent<AugmentTargetView>();
        var catapultRoot=new GameObject("CatapultAimPreview");catapultRoot.transform.SetParent(transform,false);
        catapultPreview=catapultRoot.AddComponent<SpriteRenderer>();catapultPreview.sortingOrder=90;catapultPreview.enabled=false;
        foreach(var label in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(label.name=="Timer 1")blackClockLabel=label;else if(label.name=="Timer 2")whiteClockLabel=label;
            if(label.name=="Timer 1"||label.name=="Timer 2")
            {
                label.rectTransform.anchoredPosition=new Vector2(label.name=="Timer 1"?-310f:-50f,-5f);label.rectTransform.sizeDelta=new Vector2(130,50);
            }
            string content=label.text.Trim();
            if(content=="x : y"||content=="x:y")label.gameObject.SetActive(false);
            else if(content=="VS")
            {
                label.fontSize=32;label.fontStyle=FontStyles.Bold;label.color=new Color(1f,.78f,.16f);
                label.rectTransform.sizeDelta=new Vector2(70,50);
                var outline=label.GetComponent<Outline>()??label.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.16f,.07f,.02f);outline.effectDistance=new Vector2(2,-2);
                var emptyBox=label.transform.parent.Find("PlayerIcon1 (1)");if(emptyBox!=null)emptyBox.gameObject.SetActive(false);
                emptyBox=label.transform.parent.Find("PlayerIcon1");if(emptyBox!=null)emptyBox.gameObject.SetActive(false);
            }
        }
    }
    private void Start()
    {
        if(MatchReviewSession.TryBeginReview(out string snapshot)){EnterReview(snapshot);return;}
        ResetMatch(Cards.CurrentSeed);IsInitialized=true;multiplayer.MatchLoaded(this);
    }
    public void WaitForNetwork()
    {
        Waiting=true;dragging=false;selected=-1;targetView?.Hide();Cards.EndMatch();
        if(guide!=null)guide.GetComponent<SpriteRenderer>().enabled=false;
    }
    public void ResetMatch(int seed)
    {
        stackView?.Cancel();
        mysteryBoxView?.Cancel();
        StopAllCoroutines();IsReviewMode=false;ReviewIndex=-1;MatchReviewSession.BeginMatch();
        resultQueued=false;
        lastCommandRevealedHiddenStone=false;
        Board=new AugmentBoard();Revision=0;selected=-1;history.Clear();Waiting=false;
        dragging=false;targetView?.Hide();if(catapultPreview!=null)catapultPreview.enabled=false;
        Meteor=new MeteorCharge();
        Shotgun=new ShotgunBurst();
        Clock=new MatchClock();Clock.Reset(multiplayer!=null?multiplayer.TimeLimitSeconds:MatchClock.DefaultSeconds);clockBroadcastAccumulator=0f;UpdateClockUI();
        lastEffect=null;effectRevision=0;outcomes.Clear();lastPlacements=Array.Empty<PlacementCue>();placementRevision=0;
        foreach(var effect in GetComponentsInChildren<AugmentEffectView>()){effect.gameObject.SetActive(false);Destroy(effect.gameObject);}
        ApplyingCommand=true;try{Cards.ResetMatch(seed);}finally{ApplyingCommand=false;}
        Status="흑 차례 · 증강을 선택하세요";Render();
    }
    private void OnDestroy(){if(Instance==this)Instance=null;}
    public bool RequestSelection(bool black,int slot)
    {
        int side=black?1:2;if(!CanControl(side))return false;
        return Submit(new Command{kind="select",side=side,id=slot,draftRound=Cards.RewardRoundCount});
    }
    public void ChooseAbility(bool black,int id)
    {
        if(Board.premoveSetup!=0||Shotgun.active||Meteor.active||!CanControl(black?1:2)||Cards.IsChoosing||!Cards.CanUseCard(black)||!Cards.CheckCard(id,black)||Board.winner!=0)return;
        selected=id;pieceMove=false;power=1;dx=1;dy=0;dragging=false;
        if(id==2)stackStarted=Time.unscaledTime;
        shotgunAim=pointerAvailable&&Camera.main!=null?PointerWorld():guide.centerPos;
        var initialAim=Cell(shotgunAim);if(!AugmentBoard.Inside(initialAim.x,initialAim.y))shotgunAim=guide.centerPos;
        Status=id==9?"투석: 판 아래 투석기를 누르고 조준한 뒤, 길게 눌러 힘을 모아 놓으세요":$"{Cards.CardList.Find(c=>c.Name==id).DisplayName}: 대상 교차점을 클릭하세요";
        if(id==4||id==5||id==8||id==12||id==18||id==20)
            Submit(new Command{kind="ability",side=black?1:2,id=id,x=9,y=9,dx=1,dy=0,power=1});
    }
    private bool Submit(Command command)
    {
        command.revision=Revision;
        if(multiplayer.Active&&!multiplayer.IsHost){multiplayer.SendCommand(command);return true;}
        bool accepted=Apply(command);
        if(accepted&&multiplayer.Active)multiplayer.Broadcast();
        return accepted;
    }
    public bool Apply(Command command)
    {
        if(command==null||(command.side!=1&&command.side!=2))return false;
        bool draftSelection=command.kind=="select"&&Cards.IsChoosing&&(LocalSide==0||command.draftRound==Cards.RewardRoundCount);
        if((IsBoxOpening||IsScreenEffectPlaying||AugmentEffectView.IsGolemSummoning(transform)||AugmentEffectView.IsSorting(transform)||AugmentEffectView.IsCapturing(transform))&&command.kind!="surrender"&&command.kind!="timeout")return false;
        if(IsReviewMode||Waiting||(command.revision!=Revision&&!draftSelection&&command.kind!="meteor-tap"&&command.kind!="shotgun-hit")||Board.winner!=0)return false;
        if(Meteor.active&&command.kind!="meteor-tap"&&command.kind!="meteor-impact"&&command.kind!="meteor-cancel"&&command.kind!="surrender"&&command.kind!="timeout")return false;
        if(Shotgun.active&&command.kind!="shotgun-hit"&&command.kind!="shotgun-end"&&command.kind!="surrender"&&command.kind!="timeout")return false;
        bool success=false;
        bool revealedHiddenStone=false;
        int[] before=(int[])Board.cells.Clone();
        int[] beforeHidden=(int[])Board.hidden.Clone();
        Board.capturedStones.Clear();
        Board.stoneConversions.Clear();
        int originalTurn=Board.turn,originalMoves=Board.moves;
        bool stolenPlacement=command.kind=="place"&&Board.takeover==Board.turn&&command.side!=Board.turn;
        int usedAbility=-1,placedColor=0;
        double commandTime=Now;
        ApplyingCommand=true;
        try
        {
            if(command.kind=="shotgun-hit"&&Shotgun.active&&command.side==Shotgun.side&&Shotgun.Remaining(commandTime)>0&&AugmentBoard.Inside(command.x,command.y))
            {
                success=Board.Ability(1,command.x,command.y,1,0,1,Cards.RandomInRange);
                if(success){Shotgun.Hit(command.side,commandTime);usedAbility=1;command.id=1;}
            }
            else if(command.kind=="shotgun-end"&&Shotgun.active&&command.side==Shotgun.side&&Shotgun.Remaining(Now)<=0)
            {Shotgun.active=false;success=true;}
            else if(command.kind=="meteor-tap")success=Meteor.Tap(command.side,Now);
            else if(command.kind=="meteor-cancel"&&Meteor.active&&command.side==Meteor.side){Meteor.active=false;success=true;}
            else if(command.kind=="meteor-start"&&!Meteor.active&&!Cards.IsChoosing&&command.side==Board.turn&&Cards.CanUseCard(command.side==1)&&Cards.CheckCard(11,command.side==1)
                &&AugmentBoard.Inside(command.x,command.y)&&Board.At(command.x,command.y)!=3&&Board.At(command.x,command.y)!=5)
            {Meteor.Start(command.side,command.x,command.y,Now);success=true;}
            else if(command.kind=="meteor-impact"&&Meteor.active&&command.side==Meteor.side&&Meteor.Remaining(Now)<=0)
            {
                command.x=Meteor.x;command.y=Meteor.y;command.id=11;command.power=MeteorCharge.Power(Meteor.clicks);
                var trial=JsonUtility.FromJson<AugmentBoard>(JsonUtility.ToJson(Board));
                success=trial.Ability(11,command.x,command.y,1,0,command.power,Cards.RandomInRange);
                if(success)
                {
                    success=Cards.UseCard(11,command.side==1);
                    if(success){Board=trial;usedAbility=11;Meteor.active=false;}
                }
            }
            else if(command.kind=="surrender"||command.kind=="timeout"&&command.side==Board.turn&&Clock.Remaining(command.side)<=0)
            {Board.winner=3-command.side;Meteor.active=false;Shotgun.active=false;success=true;}
            else if(draftSelection)success=Cards.SelectCard(command.side==1,command.id);
            else if(!Cards.IsChoosing)
            {
                if(command.kind=="premove"&&command.side==Board.turn&&Board.premoveSetup==command.side)
                    success=Board.QueuePremove(command.x,command.y);
                else if(command.kind=="ability"&&command.id!=11&&command.side==Board.turn&&Cards.CanUseCard(command.side==1)&&Cards.CheckCard(command.id,command.side==1))
                {
                    var trial=JsonUtility.FromJson<AugmentBoard>(JsonUtility.ToJson(Board));
                    success=trial.Ability(command.id,command.x,command.y,command.dx,command.dy,command.power,Cards.RandomInRange);
                    if(success)
                    {
                        success=Cards.UseCard(command.id,command.side==1);
                        if(success)
                        {
                            Board=trial;usedAbility=Board.lastAbility;
                            if(command.id==0)Cards.RevealMysteryBox(command.side==1,usedAbility);
                            if(usedAbility==1){Shotgun.Start(command.side,Now);Shotgun.Hit(command.side,Now);}
                        }
                    }
                }
                else if(command.kind=="place"&&command.side==(Board.takeover==Board.turn?3-Board.turn:Board.turn))
                {
                    int color=Board.grayNext==Board.turn?4:Board.convertNext==Board.turn?3-Board.turn:Board.turn;
                    placedColor=color;
                    success=Board.Place(command.x,command.y,Cards.CardUsedThisTurn,out revealedHiddenStone);
                    if(success)
                    {
                        if(!revealedHiddenStone)
                        {
                            AddPlacementHistory(command.y*19+command.x,color,false);
                            if(Board.lastPlacementVanished)history[history.Count-1]+=" · 블랙홀 소멸";
                        }
                        CompleteTurnPlacements(originalTurn,originalMoves,revealedHiddenStone?0:1);
                    }
                }
                else if(command.kind=="piece"&&command.side==Board.turn)
                {
                    success=Board.MovePiece(command.x,command.y);
                    if(success)CompleteTurnPlacements(originalTurn,originalMoves,1);
                }
            }
        }
        finally{ApplyingCommand=false;}
        if(!success){Status="사용할 수 없는 위치 또는 차례입니다. 다른 위치를 고르세요.";return false;}
        if(usedAbility<0&&Board.capturedStones.Count>0)usedAbility=4;
        if(command.kind=="place"&&(!revealedHiddenStone||Board.automaticPlacements.Count>0))GameAudio.Instance.PlayStone();
        Revision++;lastCommandRevealedHiddenStone=revealedHiddenStone;selected=Shotgun.active&&CanControl(Shotgun.side)?1:Meteor.active&&CanControl(Meteor.side)?11:-1;pieceMove=false;dragging=false;targetView?.Hide();
        var placements=new List<PlacementCue>();
        void Cue(int cell,int color,int size=1,bool stolen=false)
        {
            if(cell<0||cell>=361)return;
            placements.Add(new PlacementCue{cell=cell,color=color,stamp=Board.order[cell],size=size,stolen=stolen});
        }
        if(command.kind=="place"&&!revealedHiddenStone)Cue(command.y*19+command.x,placedColor,1,stolenPlacement);
        if(command.kind=="place"||command.kind=="piece")
            foreach(var placement in Board.automaticPlacements)Cue(placement.cell,placement.color);
        if(command.kind=="piece"&&Board.piece>=0)Cue(Board.piece,Board.pieceOwner);
        // Public stone-producing cards also get a landing cue; hidden placements stay private.
        if(usedAbility==6)Cue(command.y*19+command.x,command.side,2);
        else if(usedAbility==9)
        {var landing=AugmentAim.CatapultLanding(new Vector2Int(command.x,command.y),new Vector2Int(command.dx,command.dy),command.power);Cue(landing.y*19+landing.x,command.side);}
        else if(usedAbility==2||usedAbility==10||usedAbility==11||usedAbility==19)
            for(int p=0;p<361;p++)
                if(Board.cells[p]!=0&&Board.cells[p]!=5&&Board.cells[p]!=before[p]&&Board.hidden[p]==0)Cue(p,Board.cells[p]);
        if(placements.Count>0){lastPlacements=placements.ToArray();placementRevision=Revision;}
        bool gradeFinish=Board.winner==0&&Cards.PlacedStoneCount>=160&&Cards.AllAcquiredCardsUsed
            &&!Shotgun.active&&!Meteor.active&&!Board.HasPendingPremoves;
        if(gradeFinish)
        {
            int comparison=Cards.CompareCardGradeTotals();Board.winner=comparison==0?3:comparison>0?2:1;
        }
        if(Board.winner==0&&Array.TrueForAll(Board.cells,c=>c!=0&&c!=5))
        {
            int comparison=Cards.CompareCardGradeTotals();Board.winner=comparison==0?3:comparison>0?2:1;
        }
        Status=command.kind=="timeout"?$"{(command.side==1?"흑":"백")} 시간 초과 · {(Board.winner==1?"흑":"백")} 승리":revealedHiddenStone&&Board.winner==0?$"투명돌 발견 · 착수 실패, {(Board.turn==1?"흑":"백")} 차례":Meteor.active?"운석 충전 중 · 마우스 왼쪽 버튼을 연타하세요":Board.winner==3?"무승부":Board.winner!=0?$"{(Board.winner==1?"흑":"백")} 승리":$"{(Board.turn==1?"흑":"백")} 차례";
        if(gradeFinish)Status=$"증강 소진 · 흑 {Cards.GetCardGradeTotal(CardManager.PlayerSide.Black):0.#} / 백 {Cards.GetCardGradeTotal(CardManager.PlayerSide.White):0.#} · {(Board.winner==3?"무승부":Board.winner==1?"흑 승리":"백 승리")}";
        if(Board.winner==0&&Board.premoveSetup!=0)Status=$"야리나오시다 · 예약 위치 {Board.Premoves(Board.premoveSetup).Length}/6 선택 · 선택 후 이번 턴은 직접 착수하세요";
        if(Board.winner==0&&Shotgun.active)Status="샷건 난타 · 3초 동안 클릭할 때마다 내려찍습니다";
        if((command.kind=="place"||command.kind=="piece")&&Board.blockedPremoves>0)Status+=" · 야리나오시다 예약 묶음 취소";
        if(Board.winner!=0)Cards.EndMatch();
        Render();
        if(placements.Count>0)PlayPlacementCues();
        bool fromMysteryBox=command.kind=="ability"&&command.id==0;
        if(usedAbility>=0&&(!AugmentVisibility.IsSecret(usedAbility)||fromMysteryBox))
        {
            var removed=new List<int>();var changedCells=new List<int>();var beforeColors=new List<int>();var afterColors=new List<int>();int changed=0;
            for(int p=0;p<361;p++)
            {
                // A destroyed hidden stone must not leak its former location in a public result.
                if(beforeHidden[p]!=0||Board.hidden[p]!=0)continue;
                if(before[p]!=Board.cells[p])
                {
                    changed++;changedCells.Add(p);beforeColors.Add(before[p]);afterColors.Add(Board.cells[p]);
                }
                if(before[p]!=0&&before[p]!=5&&(Board.cells[p]==0||Board.cells[p]==5))removed.Add(p);
            }
            // Vacated origins of movement/sorting are not destroyed stones.
            bool moved=usedAbility==3||usedAbility==13||usedAbility==17;
            if(moved||usedAbility==9)removed.Clear();
            string title=Cards.CardList.Find(c=>c.Name==usedAbility)?.DisplayName??"증강";
            string detail=usedAbility==20?(Board.lastFlipResult==1?"앞면 · 흑돌 ↔ 백돌 색 교환":$"뒷면 · 내 돌 제거 {removed.Count}칸"):usedAbility==9?"절대 고정 돌 착탄 · 주변 3×3의 돌을 바깥으로 한 칸 밀어냅니다":moved?$"이동 완료 · 변경 {changed}칸":$"효과 적용 · 변경 {changed}칸 / 제거 {removed.Count}칸";
            if(removed.Count>0)
            {
                var coordinates=removed.GetRange(0,Math.Min(3,removed.Count)).ConvertAll(p=>$"({p%19+1},{p/19+1})");
                detail+=" · "+string.Join(" ",coordinates)+(removed.Count>3?$" 외 {removed.Count-3}칸":"");
            }
            lastEffect=new Command{kind="effect",side=command.side,id=usedAbility,x=command.x,y=command.y,dx=command.dx,dy=command.dy,power=command.power,revision=Revision,result=$"{(command.side==1?"흑":"백")} · {title}\n{detail}",removed=removed.ToArray(),changedCells=changedCells.ToArray(),beforeColors=beforeColors.ToArray(),afterColors=afterColors.ToArray()};effectRevision=Revision;
            outcomes.Add(lastEffect);if(outcomes.Count>128)outcomes.RemoveAt(0);
            lastEffect.fromMysteryBox=fromMysteryBox;
            if(usedAbility==20)lastEffect.coinFace=Board.lastFlipResult;
            if(usedAbility==13)lastEffect.sortMotions=Board.sortMotions.FindAll(m=>beforeHidden[m.from]==0).ToArray();
            if(usedAbility==4)lastEffect.captures=Board.capturedStones.ToArray();
            PlayPublicEffect(lastEffect);
        }
        var conversions=Board.stoneConversions.FindAll(c=>Board.cells[c.cell]==c.to).ToArray();
        if(conversions.Length>0)
        {
            lastEffect=new Command{kind="conversion",id=12,side=originalTurn,revision=Revision,conversions=conversions,result="착수한 돌의 색이 바뀌었습니다"};
            effectRevision=Revision;outcomes.Add(lastEffect);if(outcomes.Count>128)outcomes.RemoveAt(0);
            PlayPublicEffect(lastEffect);
        }
        if(revealedHiddenStone)MatchReviewSession.Record("착수 실패 · 투명돌 발견",Capture());
        else if(command.kind=="place"&&history.Count>0)MatchReviewSession.Record(history[history.Count-1],Capture());
        else if(fromMysteryBox)MatchReviewSession.Record("개꿀잼 상자",Capture());
        else if(usedAbility>=0&&!AugmentVisibility.IsSecret(usedAbility)||command.kind=="piece")MatchReviewSession.Record(command.kind=="piece"?"기물 이동":$"{(command.side==1?"흑":"백")} · {Cards.CardList.Find(c=>c.Name==usedAbility)?.DisplayName ?? "증강"}",Capture());
        if(Board.winner!=0)QueueResult();
        return true;
    }
    private static bool IsReplayableEffect(Command effect)=>effect!=null&&(effect.kind=="conversion"||effect.kind=="effect"&&(!AugmentVisibility.IsSecret(effect.id)||effect.fromMysteryBox));
    private void PlayPublicEffect(Command effect)
    {
        if(effect!=null&&effect.kind=="conversion")
        {
            AbilityScreenView.PlayConversion(transform,guide,effect.conversions,stoneVisuals);
            return;
        }
        if(effect==null||effect.kind!="effect")return;
        if(effect.fromMysteryBox)
        {
            if(mysteryBoxView==null)
            {
                var root=new GameObject("MysteryBoxReveal");root.transform.SetParent(transform,false);
                mysteryBoxView=root.AddComponent<MysteryBoxView>();
            }
            var card=Cards.CardList.Find(c=>c.Name==effect.id);
            mysteryBoxView.Play(card?.DisplayName??"비공개 증강",card?.ItemImage,()=>PlayResolvedEffect(effect));
            return;
        }
        PlayResolvedEffect(effect);
    }
    private void PlayResolvedEffect(Command effect)
    {
        if(effect.id<0||AugmentVisibility.IsSecret(effect.id))return;
        if(effect.id==18)
        {
            AbilityScreenView.PlayTakeover(transform,guide,effect.side);
            return;
        }
        if(effect.id==4&&effect.captures!=null)
        {
            AugmentEffectView.PlayCapture(transform,guide,effect.captures);
            return;
        }
        if(effect.id==2)
        {
            EnsureStackView();
            int baseColor=effect.side;
            if(effect.changedCells!=null&&effect.beforeColors!=null)
                for(int i=0;i<effect.changedCells.Length&&i<effect.beforeColors.Length;i++)
                    if(effect.changedCells[i]==effect.y*19+effect.x)baseColor=effect.beforeColors[i];
            stackView.PlayLanding(effect.side,baseColor,Mathf.Clamp(effect.dx/1000f,-2,2));
            return;
        }
        if(effect.id==13&&effect.sortMotions!=null)
        {
            AugmentEffectView.PlaySort(transform,guide,effect.sortMotions,stoneVisuals);
            return;
        }
        if(effect.id==20)
        {
            int face=effect.coinFace!=0?effect.coinFace:effect.result!=null&&effect.result.Contains("앞면")?1:2;
            AbilityScreenView.PlayCoin(transform,face,effect.removed==null?0:effect.removed.Length,()=>PlayGenericEffect(effect));
            return;
        }
        PlayGenericEffect(effect);
    }
    private void PlayGenericEffect(Command effect)
    {
        // Use the host's public, per-activation changes instead of comparing unrelated snapshots.
        var before=new int[361];var after=new int[361];
        if(effect.changedCells!=null&&effect.beforeColors!=null&&effect.afterColors!=null)
            for(int i=0;i<effect.changedCells.Length&&i<effect.beforeColors.Length&&i<effect.afterColors.Length;i++)
            {
                int cell=effect.changedCells[i];if(cell<0||cell>=361)continue;
                before[cell]=effect.beforeColors[i];after[cell]=effect.afterColors[i];
            }
        AugmentEffectView.Play(transform,guide,effect.id,effect.x,effect.y,effect.power,before,after,new Vector2Int(effect.dx,effect.dy),effect.removed);
    }
    private void AddPlacementHistory(int cell,int color,bool automatic)
    {
        history.Add($"{history.Count+1}. {(color==1?"흑":color==2?"백":"회색")}{(automatic?" 프리무브":"")} ({cell%19+1},{cell/19+1})");
    }
    private void CompleteTurnPlacements(int originalTurn,int originalMoves,int manualCount)
    {
        foreach(var placement in Board.automaticPlacements)
        {
            AddPlacementHistory(placement.cell,placement.color,true);
            if(placement.vanished)history[history.Count-1]+=" · 블랙홀 소멸";
        }
        if(Board.moves>originalMoves&&Board.turn==originalTurn)Cards.BeginTurn(originalTurn!=1);
        Cards.BeginTurn(Board.turn==1);
        Cards.NotifyStonesPlaced(manualCount+Board.automaticPlacements.Count);
    }
    public string Capture()=>JsonUtility.ToJson(new Snapshot{board=Board,cards=Cards.Capture(),revision=Revision,status=Status,history=history.ToArray(),effect=lastEffect,outcomes=outcomes.ToArray(),placements=lastPlacements,placementRevision=placementRevision,effectRevision=effectRevision,meteor=Meteor,meteorRemaining=Meteor.Remaining(Now),shotgun=Shotgun,shotgunRemaining=Shotgun.Remaining(Now),clock=Clock,hiddenStoneRevealed=lastCommandRevealedHiddenStone});
    public string CaptureForSide(int side)
    {
        var state=JsonUtility.FromJson<Snapshot>(Capture());
        HidePrivateBoxResult(state.effect,side);
        if(state.outcomes!=null)foreach(var effect in state.outcomes)HidePrivateBoxResult(effect,side);
        // The charge is public; its aim and growing radius belong only to the caster.
        if(state.meteor!=null&&state.meteor.active&&state.meteor.side!=side)
        {state.meteor.x=state.meteor.y=-1;state.meteor.clicks=0;}
        // Do not send an opponent's queued traps or hidden pieces to this player's client.
        if(state.board.grayNext==side)state.board.grayNext=0;
        if(state.board.convertNext==side)state.board.convertNext=0;
        if(state.board.takeover==side&&state.board.turn!=side)state.board.takeover=0;
        // Reservations never leave the authoritative host except to their owner.
        if(side==1)state.board.whitePremoves=Array.Empty<int>();else state.board.blackPremoves=Array.Empty<int>();
        for(int p=0;p<state.board.cells.Length;p++)
            if(state.board.hidden[p]!=0&&state.board.hidden[p]!=side)
            {state.board.cells[p]=0;state.board.hidden[p]=0;state.board.order[p]=0;}
        int[] ids=side==1?state.cards.white:state.cards.black;
        bool[] used=side==1?state.cards.whiteUsed:state.cards.blackUsed;
        int[] resolved=side==1?state.cards.whiteResolved:state.cards.blackResolved;
        // A used card becomes public; unrevealed inventory slots retain only their count.
        for(int i=0;i<ids.Length;i++)if(!used[i]){ids[i]=-1;if(resolved!=null&&i<resolved.Length)resolved[i]=-1;}
        if(AugmentVisibility.IsSecret(state.board.lastAbility))
        {
            if(state.cards.turnBlack!=(side==1))state.cards.used=false;
            state.board.lastAbility=-1;
        }
        if(state.effect!=null&&state.effect.kind=="effect"&&!state.effect.fromMysteryBox&&AugmentVisibility.IsSecret(state.effect.id))state.effect=null;
        // Offers, unused inventory IDs and the draw seed remain private.
        state.cards.seed=0;
        if(side==1)
        {
            state.cards.whiteOffers=Array.Empty<int>();
        }
        else
        {
            state.cards.blackOffers=Array.Empty<int>();
        }
        return JsonUtility.ToJson(state);
    }
    private static void HidePrivateBoxResult(Command effect,int viewer)
    {
        if(effect==null||!effect.fromMysteryBox||effect.side==viewer||!AugmentVisibility.IsSecret(effect.id))return;
        effect.x=effect.y=effect.dx=effect.dy=effect.power=0;
        if(effect.result!=null)effect.result=effect.result.Split('\n')[0];
        effect.removed=effect.changedCells=effect.beforeColors=effect.afterColors=Array.Empty<int>();
        effect.sortMotions=null;effect.captures=null;
    }
    public void Restore(string json)
    {
        int previousHistory=history.Count;
        var state=JsonUtility.FromJson<Snapshot>(json);
        if(!IsReviewMode&&state.revision<Revision)return;
        if(!IsReviewMode&&!Waiting&&state.revision==Revision)
        {
            Clock=state.clock??Clock;
            if(Meteor.active)Meteor.deadline=Now+state.meteorRemaining;
            if(Shotgun.active)Shotgun.deadline=Now+state.shotgunRemaining;
            UpdateClockUI();return;
        }
        int previousEffectRevision=effectRevision;
        bool showEffect=IsReplayableEffect(state.effect)&&state.effectRevision>effectRevision;
        bool showPlacement=state.placementRevision>placementRevision;
        lastPlacements=state.placements??Array.Empty<PlacementCue>();placementRevision=state.placementRevision;
        Board=state.board;Revision=state.revision;Status=state.status;lastEffect=state.effect;effectRevision=state.effectRevision;
        outcomes.Clear();if(state.outcomes!=null)outcomes.AddRange(state.outcomes);
        lastCommandRevealedHiddenStone=state.hiddenStoneRevealed;
        if(!IsReviewMode&&state.history!=null&&state.history.Length>previousHistory)GameAudio.Instance.PlayStone();
        Waiting=false;Meteor=state.meteor??new MeteorCharge();if(Meteor.active)Meteor.deadline=Now+state.meteorRemaining;
        Shotgun=state.shotgun??new ShotgunBurst();if(Shotgun.active)Shotgun.deadline=Now+state.shotgunRemaining;
        Clock=state.clock??new MatchClock();UpdateClockUI();
        selected=Shotgun.active&&CanControl(Shotgun.side)?1:Meteor.active&&CanControl(Meteor.side)?11:-1;pieceMove=false;dragging=false;targetView?.Hide();Waiting=false;history.Clear();history.AddRange(state.history);
        Cards.Restore(state.cards);Render();
        if(showPlacement&&!IsReviewMode)PlayPlacementCues();
        if(!IsReviewMode)
        {
            bool played=false;
            foreach(var effect in outcomes)
                if(IsReplayableEffect(effect)&&effect.revision>previousEffectRevision)
                {PlayPublicEffect(effect);played=true;}
            if(!played&&showEffect)PlayPublicEffect(lastEffect);
        }
        if(!IsReviewMode&&state.hiddenStoneRevealed)MatchReviewSession.Record("착수 실패 · 투명돌 발견",json);
        else if(!IsReviewMode&&history.Count>previousHistory&&history.Count>0)MatchReviewSession.Record(history[history.Count-1],json);
        else if(!IsReviewMode&&showEffect)MatchReviewSession.Record($"증강 · {Cards.CardList.Find(c=>c.Name==lastEffect.id)?.DisplayName ?? "효과"}",json);
        if(!IsReviewMode&&Board.winner!=0)QueueResult();
    }
    private void EnterReview(string snapshot)
    {
        IsReviewMode=true;Restore(snapshot);Waiting=true;Cards.EndMatch();ReviewIndex=MatchReviewSession.Frames.Count-1;
        Status=$"복기 {ReviewIndex+1}/{MatchReviewSession.Frames.Count} · 기보를 클릭해 해당 수를 확인하세요";Render();
    }
    public void ShowReviewMove(int index)
    {
        if(!IsReviewMode)return;string snapshot=MatchReviewSession.SnapshotAt(index);if(string.IsNullOrEmpty(snapshot))return;
        ReviewIndex=index;Restore(snapshot);Waiting=true;Cards.EndMatch();Status=$"복기 {index+1}/{MatchReviewSession.Frames.Count}";Render();
    }
    private void QueueResult()
    {
        if(resultQueued||IsReviewMode)return;resultQueued=true;
        MatchReviewSession.Complete(Board.winner,LocalSide,Capture(),Status.StartsWith("증강 소진")?Status:null);StartCoroutine(OpenResult());
    }
    private System.Collections.IEnumerator OpenResult()
    {
        yield return new WaitForSecondsRealtime(.45f);
        while(AugmentEffectView.IsAbilityAnimating(transform))yield return null;
        SceneManager.LoadSceneAsync(MatchReviewSession.ResultScene);
    }
    private void Update()
    {
        if(IsReviewMode){UpdateClockUI();return;}
        if(IsBoxOpening||IsScreenEffectPlaying)
        {
            if(Shotgun.active)Shotgun.deadline=Now+ShotgunBurst.Duration;
            targetView?.Hide();if(guide!=null)guide.GetComponent<SpriteRenderer>().enabled=false;
            UpdateClockUI();return;
        }
        UpdateClock();
        if(Shotgun.active&&Shotgun.Remaining(Now)<=0)
        {
            if(!Waiting&&(!multiplayer.Active||multiplayer.IsHost))Submit(new Command{kind="shotgun-end",side=Shotgun.side});
            UpdatePreview();return;
        }
        if(Meteor.active&&!Waiting&&(!multiplayer.Active||multiplayer.IsHost)&&Meteor.Remaining(Now)<=0)
            Submit(new Command{kind="meteor-impact",side=Meteor.side});
        if(AudioSettings.IsOpen){dragging=false;targetView?.Hide();return;}
        if(Camera.main==null||guide==null)return;
        if(!ReadPointer(out bool pressed,out bool released)){UpdatePreview();return;}
        if(!Shotgun.active&&((Mouse.current!=null&&Mouse.current.rightButton.wasPressedThisFrame)||(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)))
        {
            if(Meteor.active&&CanControl(Meteor.side))Submit(new Command{kind="meteor-cancel",side=Meteor.side});
            selected=-1;pieceMove=false;dragging=false;targetView?.Hide();if(catapultPreview!=null)catapultPreview.enabled=false;
        }
        Vector3 world=PointerWorld();
        shotgunAim=Vector3.MoveTowards(shotgunAim,world,Time.unscaledDeltaTime*guide.gap*2);
        bool overUI=PointerOverUI();
        HandlePointer(world,pressed,released,overUI);
        UpdatePreview();
    }
    private void UpdateClock()
    {
        bool running=!IsReviewMode&&!Waiting&&!IsBoxOpening&&!IsScreenEffectPlaying&&Board.winner==0&&!Cards.IsChoosing&&!AugmentEffectView.IsGolemSummoning(transform)&&!AugmentEffectView.IsCapturing(transform);
        if(running)
        {
            bool expired=Clock.Tick(Board.turn,Time.unscaledDeltaTime);
            bool authority=!multiplayer.Active||multiplayer.IsHost;
            if(expired&&authority)Submit(new Command{kind="timeout",side=Board.turn});
            if(authority&&multiplayer.Active)
            {
                clockBroadcastAccumulator+=Time.unscaledDeltaTime;
                if(clockBroadcastAccumulator>=1f){clockBroadcastAccumulator=0f;multiplayer.Broadcast();}
            }
        }
        UpdateClockUI();
    }
    private void UpdateClockUI()
    {
        if(blackClockLabel!=null)
        {
            blackClockLabel.text=FormatClock(Clock.Remaining(1));
            blackClockLabel.color=Clock.Remaining(1)<=10f?Color.red:Board.turn==1&&Board.winner==0?new Color(.68f,.36f,.04f):new Color(.2f,.15f,.1f);
        }
        if(whiteClockLabel!=null)
        {
            whiteClockLabel.text=FormatClock(Clock.Remaining(2));
            whiteClockLabel.color=Clock.Remaining(2)<=10f?Color.red:Board.turn==2&&Board.winner==0?new Color(.68f,.36f,.04f):new Color(.2f,.15f,.1f);
        }
    }
    private static string FormatClock(float seconds)
    {
        int total=Mathf.Max(0,Mathf.CeilToInt(seconds));return $"{total/60:00}:{total%60:00}";
    }
    private bool ReadPointer(out bool pressed,out bool released)
    {
        pressed=released=false;
        var touch=Touchscreen.current?.primaryTouch;
        if(touch!=null&&(touch.press.isPressed||touch.press.wasPressedThisFrame||touch.press.wasReleasedThisFrame))
        {
            pointerIsTouch=true;pointerAvailable=true;pointerHeld=touch.press.isPressed;
            pointerScreenPosition=touch.position.ReadValue();
            // Some browsers also emit compatibility mouse events for a tap.
            ignoreMouseUntil=Time.unscaledTime+.2f;
            if(touch.phase.ReadValue()==UnityEngine.InputSystem.TouchPhase.Canceled)
            {dragging=false;pointerAvailable=false;return false;}
            pressed=touch.press.wasPressedThisFrame;released=touch.press.wasReleasedThisFrame;
            return true;
        }
        pointerAvailable=false;pointerHeld=false;
        if(Mouse.current==null||Time.unscaledTime<ignoreMouseUntil)return false;
        pointerIsTouch=false;pointerAvailable=true;
        pointerScreenPosition=Mouse.current.position.ReadValue();
        pointerHeld=Mouse.current.leftButton.isPressed;
        pressed=Mouse.current.leftButton.wasPressedThisFrame;released=Mouse.current.leftButton.wasReleasedThisFrame;
        return true;
    }
    private bool PointerOverUI()
    {
        var events=EventSystem.current;if(events==null)return false;
        // A mouse click can close a UI panel earlier in this frame. Keep the UI
        // module's hit too, so that same click cannot fall through onto the board.
        if(!pointerIsTouch&&events.IsPointerOverGameObject())return true;
        if(pointerEvent==null||pointerEventSystem!=events){pointerEventSystem=events;pointerEvent=new PointerEventData(events);}
        pointerEvent.Reset();pointerEvent.position=pointerScreenPosition;
        pointerHits.Clear();events.RaycastAll(pointerEvent,pointerHits);
        foreach(var hit in pointerHits)if(hit.module is GraphicRaycaster)return true;
        return false;
    }
    private Vector3 PointerWorld(){Vector3 p=Camera.main.ScreenToWorldPoint(pointerScreenPosition);p.z=0;return p;}
    private void OnApplicationFocus(bool focused)
    {
        if(focused)return;
        dragging=false;pointerAvailable=false;pointerHeld=false;
    }
    private Vector2 Grid(Vector3 world)=>new Vector2((world.x-guide.centerPos.x)/guide.gap+9,9-(world.y-guide.centerPos.y)/guide.gap);
    private Vector2Int Cell(Vector3 world){Vector2 p=Grid(world);return new Vector2Int(Mathf.RoundToInt(p.x),Mathf.RoundToInt(p.y));}
    private Vector2Int HoverCell(Vector3 world)
    {
        var cell=Cell(selected==1?shotgunAim:world);
        if(selected==2)cell.x+=Mathf.RoundToInt(AugmentAim.StackOffset(Time.unscaledTime-stackStarted));
        return cell;
    }
    private void HandlePointer(Vector3 world,bool pressed,bool released,bool overUI)
    {
        if(Waiting||Cards.IsChoosing||Board.winner!=0){dragging=false;return;}
        int side=selected>=0||pieceMove?Board.turn:Board.takeover==Board.turn?3-Board.turn:Board.turn;
        if(!CanControl(side))return;
        if(Shotgun.active)
        {
            if(pressed&&!overUI)
            {
                var position=HoverCell(world);
                Submit(new Command{kind="shotgun-hit",side=Shotgun.side,x=position.x,y=position.y});
            }
            return;
        }
        if(Board.premoveSetup!=0)
        {
            if(pressed&&!overUI)
            {
                var position=Cell(world);
                Submit(new Command{kind="premove",side=Board.turn,x=position.x,y=position.y});
            }
            return;
        }
        if(Meteor.active)
        {
            if(pressed&&!overUI)Submit(new Command{kind="meteor-tap",side=Meteor.side});
            return;
        }
        if(selected==3)
        {
            if(pressed&&!overUI)
            {
                var origin=Cell(world);
                if(!AugmentBoard.Inside(origin.x,origin.y)||Board.At(origin.x,origin.y)!=Board.turn){Status="내 돌을 잡고 뒤로 당겨주세요.";return;}
                dragOrigin=origin;dragging=true;
            }
            if(!dragging)return;
            bool aimed=AugmentAim.FlickPull(dragOrigin,Grid(world),out Vector2Int preciseDirection,out int pullPower);
            if(aimed){dx=preciseDirection.x;dy=preciseDirection.y;power=pullPower;}
            if(!released)return;
            dragging=false;if(overUI||!aimed)return;
            Submit(new Command{kind="ability",side=side,id=3,x=dragOrigin.x,y=dragOrigin.y,dx=dx,dy=dy,power=power});return;
        }
        if(selected==9)
        {
            Vector2 launcherGrid=new Vector2(9,18);
            if(pressed&&!overUI){dragOrigin=new Vector2Int(9,18);dragging=true;catapultChargeStarted=Time.unscaledTime;}
            bool aimed=AugmentAim.CatapultDirection(launcherGrid,Grid(world),out Vector2Int direction);
            if(aimed){dx=direction.x;dy=direction.y;}
            power=dragging?AugmentAim.CatapultPower(Time.unscaledTime-catapultChargeStarted):1;
            if(!dragging||!released)return;
            dragging=false;
            if(overUI||!aimed||dy>=0){Status="투석기는 판 안쪽(위쪽)을 향해야 합니다.";return;}
            Submit(new Command{kind="ability",side=side,id=9,x=9,y=18,dx=dx,dy=dy,power=power});return;
        }
        if(selected>=0&&AugmentAim.Directional(selected))
        {
            if(pressed&&!overUI)
            {
                var origin=Cell(world);
                if(!AugmentBoard.Inside(origin.x,origin.y))return;
                int stone=Board.At(origin.x,origin.y);
                if(selected==3&&stone!=Board.turn){Status="내 돌을 누른 뒤 뒤로 당겨주세요.";return;}
                if(selected==17&&(stone==0||stone==3||stone==5||stone==AugmentBoard.GolemCell)){Status="이동할 수 있는 돌을 골라주세요.";return;}
                dragOrigin=origin;dragging=true;
            }
            if(!dragging)return;
            bool aimed=AugmentAim.Drag(dragOrigin,Grid(world),selected==3,out Vector2Int direction,out int strength);
            dx=direction.x;dy=direction.y;power=selected==17||selected==13?1:strength;
            if(!released)return;
            dragging=false;
            if(overUI||!aimed)return;
            Submit(new Command{kind="ability",side=side,id=selected,x=dragOrigin.x,y=dragOrigin.y,dx=dx,dy=dy,power=power});return;
        }
        if(!pressed||overUI)return;
        var target=HoverCell(world);if(!AugmentBoard.Inside(target.x,target.y))return;
        if(selected==2)dx=Mathf.RoundToInt(AugmentAim.StackOffset(Time.unscaledTime-stackStarted)*1000f);
        if(selected<0&&!pieceMove&&Board.piece==target.y*19+target.x&&Board.pieceOwner==Board.turn)
        {pieceMove=true;Status="기물을 이동할 교차점을 클릭하세요.";return;}
        Submit(new Command{kind=selected==11?"meteor-start":selected>=0?"ability":pieceMove?"piece":"place",side=side,id=selected,x=target.x,y=target.y,dx=dx,dy=dy,power=power});
    }
    public void Surrender()
    {
        int side=LocalSide==0?Board.turn:LocalSide;
        if(CanControl(side))Submit(new Command{kind="surrender",side=side});
    }
    private void UpdatePreview()
    {
        stackView?.HideAim();
        if(guide==null||Camera.main==null)return;
        var sr=guide.GetComponent<SpriteRenderer>();
        sr.enabled=false;targetView?.Hide();if(catapultPreview!=null)catapultPreview.enabled=false;
        if(!pointerAvailable||(pointerIsTouch&&!pointerHeld))return;
        if(IsReviewMode||Waiting||Cards.IsChoosing||Board.winner!=0||AugmentEffectView.IsGolemSummoning(transform))return;
        int controllingSide=selected>=0||pieceMove?Board.turn:Board.takeover==Board.turn?3-Board.turn:Board.turn;
        if(!CanControl(controllingSide))return;
        bool overUI=PointerOverUI();
        if(overUI&&!dragging&&!Meteor.active)return;
        Vector3 world=PointerWorld();var cell=Meteor.active?new Vector2Int(Meteor.x,Meteor.y):selected==9?new Vector2Int(9,18):dragging?dragOrigin:HoverCell(world);
        if(selected==2)
        {
            EnsureStackView();
            var origin=Cell(world);
            float phase=Time.unscaledTime-stackStarted;
            bool stackValid=AugmentBoard.Inside(cell.x,cell.y)&&ValidTarget(cell,Vector2Int.right)&&!Board.IsAnchored(cell.x,cell.y);
            stackView.ShowAim(Board.turn,Board.VisibleAt(origin.x,origin.y,ViewingSide),AugmentAim.StackOffset(phase),AugmentAim.StackSpeed(phase),stackValid);
        }
        if(!AugmentBoard.Inside(cell.x,cell.y))return;
        if(Board.premoveSetup!=0)
        {
            int value=Board.VisibleAt(cell.x,cell.y,Board.turn);
            bool valid=(value==0||value==5)&&Array.IndexOf(Board.Premoves(Board.turn),cell.y*19+cell.x)<0;
            ShowPlacementMarker(cell,valid);return;
        }
        if(selected>=0||Meteor.active)
        {
            if(selected==9&&catapultPreview!=null)
            {
                catapultPreview.sprite=AugmentEffectView.CatapultFrame(0);catapultPreview.enabled=catapultPreview.sprite!=null;
                catapultPreview.transform.position=guide.centerPos+Vector3.down*guide.gap*8.1f;
                if(catapultPreview.sprite!=null)
                {
                    float fit=guide.gap*2.25f/Mathf.Max(.01f,catapultPreview.sprite.bounds.size.x);
                    catapultPreview.transform.localScale=Vector3.one*fit;
                    Vector3 aim=world-catapultPreview.transform.position;
                    if(aim.sqrMagnitude>.01f)catapultPreview.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(aim.y,aim.x)*Mathf.Rad2Deg+90f);
                }
                var catapultDirection=new Vector2Int(dx,dy);var origin=new Vector2Int(9,18);
                var landing=AugmentAim.CatapultLanding(origin,catapultDirection,power);
                bool catapultValid=dy<0&&ValidTarget(origin,catapultDirection);
                var catapultColor=catapultValid?new Color(.25f,1,.55f):new Color(1,.12f,.12f);
                var end=new Vector3(guide.centerPos.x+(landing.x-9)*guide.gap,guide.centerPos.y-(landing.y-9)*guide.gap,-.2f);
                targetView.Show(AugmentAim.Cells(9,origin,catapultDirection,power),guide.centerPos,guide.gap,catapultColor,origin,catapultDirection,power,null,end);
                if(AugmentBoard.Inside(landing.x,landing.y))ShowPlacementMarker(landing,catapultValid);
                return;
            }
            var direction=new Vector2Int(dx,dy);
            var cells=AugmentAim.Cells(Meteor.active?11:selected,cell,direction,Meteor.active?MeteorCharge.Power(Meteor.clicks):selected==11?2:power);
            bool valid=ValidTarget(cell,direction);
            Color color=selected==1||!valid?new Color(1,.12f,.12f):new Color(.25f,1,.55f);
            targetView.Show(cells,guide.centerPos,guide.gap,color,dragging?(Vector2Int?)dragOrigin:null,direction,selected==17?1:power,
                dragging&&selected==3?(Vector3?)world:null);
            if(!valid)ShowPlacementMarker(cell,false);
            return;
        }
        bool legal=pieceMove?Board.CanMovePiece(cell.x,cell.y):Board.PreviewLegal(cell.x,cell.y,Board.turn,Cards.CardUsedThisTurn,ViewingSide);
        ShowPlacementMarker(cell,legal);
    }
    private void ShowPlacementMarker(Vector2Int cell,bool legal)
    {
        var sr=guide.GetComponent<SpriteRenderer>();
        guide.transform.position=new Vector3(guide.centerPos.x+(cell.x-9)*guide.gap,guide.centerPos.y-(cell.y-9)*guide.gap,0);
        sr.sprite=legal?(Board.turn==1?guide.검은돌이미지:guide.흰돌이미지):guide.금지이미지;
        sr.enabled=sr.sprite!=null;sr.sortingOrder=110;
        sr.color=legal?new Color(1,1,1,.5f):Color.white;
        guide.transform.localScale=legal||sr.sprite==null?guidePreviewScale:Vector3.one*(guide.gap*.9f/Mathf.Max(.01f,sr.sprite.bounds.size.x));
    }
    private bool ValidTarget(Vector2Int cell,Vector2Int direction)
    {
        if(selected==9)
        {
            var landing=AugmentAim.CatapultLanding(cell,direction,power);
            return direction!=Vector2Int.zero&&AugmentBoard.Inside(landing.x,landing.y)&&Board.VisibleAt(landing.x,landing.y,ViewingSide)!=3&&!Board.IsAnchored(landing.x,landing.y);
        }
        int value=Board.VisibleAt(cell.x,cell.y,ViewingSide);
        if(selected==6)
        {for(int y=0;y<2;y++)for(int x=0;x<2;x++)if(!AugmentBoard.Inside(cell.x+x,cell.y+y)||Board.VisibleAt(cell.x+x,cell.y+y,ViewingSide)!=0)return false;return true;}
        if(selected==10||selected==16)return value==0;
        if(selected==14)return Board.CanSummonGolem(cell.x,cell.y,Board.turn);
        if(selected==2)return value!=3&&value!=5&&value!=AugmentBoard.GolemCell;
        if(selected==11)return value!=3&&value!=5;
        if(selected==3||selected==9||selected==17)
        {
            if(selected==3&&(value!=Board.turn||Board.IsAnchored(cell.x,cell.y)))return false;
            if(selected==17&&(value==0||value==3||value==5||value==AugmentBoard.GolemCell))return false;
            if(!dragging)return true;
            if(direction==Vector2Int.zero)return false;
            if(selected==3)return true;
            Vector2Int destination=cell+direction*(selected==17?1:power);
            if(!AugmentBoard.Inside(destination.x,destination.y))return false;
            int target=Board.VisibleAt(destination.x,destination.y,ViewingSide);
            return selected==17?target==0:target!=3&&target!=5;
        }
        return true;
    }
    private void EnsureStackView()
    {
        if(stackView!=null)return;
        var root=new GameObject("StackStoneSideView");root.transform.SetParent(transform,false);
        stackView=root.AddComponent<StackStoneView>();
    }
    private void Render()
    {
        mysteryBoxView?.Cancel();
        foreach(var screen in GetComponentsInChildren<AbilityScreenView>()){screen.Cancel();Destroy(screen.gameObject);}
        foreach(var effect in GetComponentsInChildren<AugmentEffectView>())if(effect.IsSortMotion||effect.IsCaptureMotion){effect.gameObject.SetActive(false);Destroy(effect.gameObject);}
        Array.Clear(stoneVisuals,0,stoneVisuals.Length);
        foreach(var visual in visuals)if(visual!=null)Destroy(visual);visuals.Clear();
        if(guide==null||guide.돌==null)return;
        for(int owner=1;owner<=2;owner++)if(Board.giant[owner]>=0)
        {
            int p=Board.giant[owner];
            var golem=AugmentEffectView.CreateGolemIdle(transform,new Vector3(guide.centerPos.x+(p%19-9)*guide.gap,guide.centerPos.y-(p/19-9)*guide.gap,-.08f),guide.gap);
            if(golem!=null)visuals.Add(golem);
        }
        var coveredByLargeStone=new bool[361];
        for(int anchor=0;anchor<361;anchor++)
        {
            int owner=Board.largeStone!=null&&anchor<Board.largeStone.Length?Board.largeStone[anchor]:0;
            int ax=anchor%19,ay=anchor/19;
            if((owner!=1&&owner!=2)||ax>=18||ay>=18)continue;
            bool intact=true;
            for(int oy=0;oy<2;oy++)for(int ox=0;ox<2;ox++)if(Board.cells[(ay+oy)*19+ax+ox]!=owner)intact=false;
            if(!intact)continue;
            Vector3 center=new Vector3(guide.centerPos.x+(ax-8.5f)*guide.gap,guide.centerPos.y-(ay-8.5f)*guide.gap,0);
            var large=Instantiate(guide.돌,center,Quaternion.identity);
            var renderer=large.GetComponent<SpriteRenderer>();renderer.sprite=owner==2?guide.흰돌이미지:guide.검은돌이미지;renderer.color=Color.white;
            float scale=guide.gap*1.9f/Mathf.Max(.01f,renderer.sprite.bounds.size.x);large.transform.localScale=Vector3.one*scale;
            RenderLargeStoneFootprint(large,renderer,scale);
            visuals.Add(large);
            for(int oy=0;oy<2;oy++)for(int ox=0;ox<2;ox++)coveredByLargeStone[(ay+oy)*19+ax+ox]=true;
        }
        for(int p=0;p<361;p++)
        {
            if(coveredByLargeStone[p])continue;
            int c=Board.cells[p];if(c==0||(Board.hidden[p]!=0&&Board.hidden[p]!=ViewingSide))continue;
            if(c==5||c==AugmentBoard.GolemCell)continue;
            var go=Instantiate(guide.돌,new Vector3(guide.centerPos.x+(p%19-9)*guide.gap,guide.centerPos.y-(p/19-9)*guide.gap,0),Quaternion.identity);
            var sr=go.GetComponent<SpriteRenderer>();sr.sprite=c==2||c==4?guide.흰돌이미지:guide.검은돌이미지;
            if(Board.IsAnchored(p%19,p/19))sr.sortingOrder=100;
            // A winning landing inside the vortex stays visible above the remaining black-hole art.
            if(Board.winner!=0&&Array.Exists(lastPlacements,cue=>cue!=null&&cue.cell==p))sr.sortingOrder=100;
            sr.color=c==3?new Color(.6f,.4f,.15f):c==4?Color.gray:c==5?new Color(.25f,.1f,.35f):Color.white;
            if(Board.hidden[p]!=0)sr.color=new Color(1,1,1,.45f);
            if(c==3&&AugmentEffectView.Artwork!=null&&AugmentEffectView.Artwork.MeteorRock!=null)
            {
                sr.sprite=AugmentEffectView.Artwork.MeteorRock;sr.color=Color.white;
                float rockScale=guide.gap*.9f/Mathf.Max(.01f,sr.sprite.bounds.size.x);go.transform.localScale=Vector3.one*rockScale;
            }
            else if(c==3||c==5||Board.piece==p)
            {
                sr.sprite=AugmentEffectView.Square;
                go.transform.localScale=Vector3.one*guide.gap*.85f;
                if(Board.piece==p)sr.color=AugmentEffectView.Tint(7);
            }
            visuals.Add(go);
            stoneVisuals[p]=go;
        }
        RenderBlackHoles();
        for(int p=0;p<361;p++)if(Board.IsAnchored(p%19,p/19))
        {
            var pos=new Vector3(guide.centerPos.x+(p%19-9)*guide.gap,guide.centerPos.y-(p/19-9)*guide.gap,-.12f);
            visuals.Add(AugmentEffectView.CreateAnchoredMarker(transform,pos,guide.gap));
        }
        RenderPremoves();
        RenderLastPlacements();
        var ui=FindAnyObjectByType<CardSideTabUI>();if(ui!=null)ui.RefreshAll();
    }
    private Vector3 PlacementPosition(PlacementCue cue)
    {
        float offset=(cue.size-1)*.5f;
        return new Vector3(guide.centerPos.x+(cue.cell%19-9+offset)*guide.gap,guide.centerPos.y-(cue.cell/19-9+offset)*guide.gap,-.15f);
    }
    private void PlayPlacementCues()
    {
        if(guide==null)return;
        foreach(var cue in lastPlacements)
            if(cue!=null&&cue.cell>=0&&cue.cell<361&&(Board.hidden[cue.cell]==0||Board.hidden[cue.cell]==ViewingSide))
            {
                AugmentEffectView.PlayPlacement(transform,PlacementPosition(cue),guide.gap*cue.size,cue.color);
                if(cue.stolen)AugmentEffectView.PlayTakeoverLanding(transform,PlacementPosition(cue),guide.gap*cue.size);
            }
    }
    private void RenderLastPlacements()
    {
        foreach(var cue in lastPlacements)
        {
            if(cue==null||cue.cell<0||cue.cell>=361||Board.cells[cue.cell]==0||Board.cells[cue.cell]==5||Board.order[cue.cell]!=cue.stamp)continue;
            if(Board.hidden[cue.cell]!=0&&Board.hidden[cue.cell]!=ViewingSide)continue;
            var root=AugmentEffectView.CreatePlacementMarker(transform,PlacementPosition(cue),guide.gap*cue.size);
            visuals.Add(root);
        }
    }
    private void RenderLargeStoneFootprint(GameObject stone,SpriteRenderer body,float bodyScale)
    {
        // Keep one stone body, with a permanent frame around its four occupied intersections.
        var root=new GameObject("LargeStoneFootprint_2x2");root.transform.SetParent(stone.transform,false);
        root.transform.localScale=Vector3.one/bodyScale;
        float gap=guide.gap;Color gold=new Color(1f,.73f,.16f),ink=new Color(.19f,.12f,.04f,.95f);
        void Box(string name,Vector2 position,Vector2 size,Color color,int order)
        {
            var go=new GameObject(name);go.transform.SetParent(root.transform,false);
            go.transform.localPosition=new Vector3(position.x,position.y,-.02f);
            go.transform.localScale=new Vector3(size.x,size.y,1);
            var sr=go.AddComponent<SpriteRenderer>();sr.sprite=AugmentEffectView.Square;sr.color=color;
            sr.sortingLayerID=body.sortingLayerID;sr.sortingOrder=order;
        }
        int front=body.sortingOrder+3;
        for(int edge=0;edge<4;edge++)
        {
            bool horizontal=edge<2;float sign=edge%2==0?-1:1;
            Vector2 pos=horizontal?new Vector2(0,sign*gap):new Vector2(sign*gap,0);
            Box("FootprintBorderShadow",pos,horizontal?new Vector2(gap*2.07f,gap*.075f):new Vector2(gap*.075f,gap*2.07f),ink,front);
            Box("FootprintBorder",pos,horizontal?new Vector2(gap*2.03f,gap*.035f):new Vector2(gap*.035f,gap*2.03f),gold,front+1);
        }
        for(int y=0;y<2;y++)for(int x=0;x<2;x++)
        {
            var pos=new Vector2((x-.5f)*gap,(y-.5f)*gap);
            Box("OccupiedIntersectionShadow",pos,Vector2.one*gap*.15f,ink,front);
            Box("OccupiedIntersection",pos,Vector2.one*gap*.075f,gold,front+1);
        }
        Box("SizeBadge",new Vector2(0,-gap*.97f),new Vector2(gap*.78f,gap*.34f),ink,front+2);
        var label=new GameObject("SizeLabel");label.transform.SetParent(root.transform,false);
        label.transform.localPosition=new Vector3(0,-gap*.97f,-.03f);
        var text=label.AddComponent<TextMeshPro>();text.text="2×2";text.fontSize=gap*2.5f;
        text.fontStyle=FontStyles.Bold;text.alignment=TextAlignmentOptions.Center;text.color=gold;
        text.rectTransform.sizeDelta=new Vector2(gap*.78f,gap*.34f);
        text.GetComponent<Renderer>().sortingLayerID=body.sortingLayerID;text.GetComponent<Renderer>().sortingOrder=front+3;
    }
    private void RenderPremoves()
    {
        if(IsReviewMode)return;
        int side=LocalSide==0?Board.turn:LocalSide;
        var queue=Board.Premoves(side);
        for(int i=Board.premoveIndex[side];i<queue.Length;i++)
        {
            int p=queue[i];if(p<0||p>=361)continue;
            var root=new GameObject("PrivatePremove_"+(i+1));root.transform.position=new Vector3(guide.centerPos.x+(p%19-9)*guide.gap,guide.centerPos.y-(p/19-9)*guide.gap,-.2f);
            var sprite=root.AddComponent<SpriteRenderer>();sprite.sprite=side==1?guide.검은돌이미지:guide.흰돌이미지;sprite.color=new Color(1,1,1,.28f);sprite.sortingOrder=92;
            float scale=guide.gap*.8f/Mathf.Max(.01f,sprite.sprite.bounds.size.x);root.transform.localScale=Vector3.one*scale;
            var number=new GameObject("Order");number.transform.SetParent(root.transform,false);number.transform.localScale=Vector3.one/scale;
            var text=number.AddComponent<TextMeshPro>();text.text=(i+1).ToString();text.fontSize=guide.gap*5;text.fontStyle=FontStyles.Bold;text.alignment=TextAlignmentOptions.Center;text.color=Color.white;text.outlineColor=Color.black;text.outlineWidth=.15f;text.rectTransform.sizeDelta=Vector2.one*guide.gap;text.GetComponent<Renderer>().sortingOrder=115;
            visuals.Add(root);
        }
    }
    private void RenderBlackHoles()
    {
        var visited=new bool[361];var queue=new Queue<int>();
        for(int start=0;start<361;start++)
        {
            if(visited[start]||Board.cells[start]!=5)continue;
            int minX=start%19,maxX=minX,minY=start/19,maxY=minY;visited[start]=true;queue.Enqueue(start);
            while(queue.Count>0)
            {
                int p=queue.Dequeue(),x=p%19,y=p/19;minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);
                int[] next={p-1,p+1,p-19,p+19};
                foreach(int n in next)if(n>=0&&n<361&&!visited[n]&&Board.cells[n]==5&&Mathf.Abs(n%19-x)+Mathf.Abs(n/19-y)==1){visited[n]=true;queue.Enqueue(n);}
            }
            float cx=(minX+maxX)*.5f,cy=(minY+maxY)*.5f;
            Vector3 pos=new Vector3(guide.centerPos.x+(cx-9)*guide.gap,guide.centerPos.y-(cy-9)*guide.gap,-.06f);
            float size=Mathf.Max(maxX-minX+1,maxY-minY+1)*guide.gap;
            var blackHole=AugmentEffectView.CreateBlackHole(transform,pos,size);if(blackHole!=null)visuals.Add(blackHole);
        }
    }
    private void OnGUI()
    {
        if(IsReviewMode)
        {
            if(guiStyle==null){guiStyle=new GUIStyle(GUI.skin.label){wordWrap=true};guiStyle.font=GuiFont;}
            guiStyle.normal.textColor=Color.white;
            GUI.skin.label.font=GUI.skin.button.font=guiStyle.font;
            GUILayout.BeginArea(new Rect(Mathf.Max(8,(Screen.width-500)*.5f),Screen.height-92,500,84),GUI.skin.box);
            GUILayout.Label(Status,guiStyle);GUILayout.BeginHorizontal();
            if(GUILayout.Button("◀ 이전"))ShowReviewMove(Mathf.Max(0,ReviewIndex-1));
            if(GUILayout.Button("다음 ▶"))ShowReviewMove(Mathf.Min(MatchReviewSession.Frames.Count-1,ReviewIndex+1));
            if(GUILayout.Button("결과 화면"))SceneManager.LoadSceneAsync(MatchReviewSession.ResultScene);
            if(GUILayout.Button("메인 화면"))ReturnFromReview();
            GUILayout.EndHorizontal();GUILayout.EndArea();return;
        }
        // Reservation instructions are private, just like the numbered board markers.
        if(Meteor.active&&!CanControl(Meteor.side)||Shotgun.active&&!CanControl(Shotgun.side))return;
        bool planning=Board.premoveSetup==Board.turn&&CanControl(Board.turn);
        bool reservationsReady=CanControl(Board.turn)&&Board.Premoves(Board.turn).Length==6&&Board.premoveIndex[Board.turn]==0;
        if(selected<0&&!pieceMove&&!Meteor.active&&!planning&&!reservationsReady)return;
        if(guiStyle==null){guiStyle=new GUIStyle(GUI.skin.label){wordWrap=true};guiStyle.font=GuiFont;}
        GUI.skin.label.font=guiStyle.font;GUI.skin.button.font=guiStyle.font;GUI.skin.textField.font=guiStyle.font;
        float panelHeight=selected==9?170:80;
        float panelY=selected==9?150:Mathf.Max(8,Screen.height-panelHeight-8);
        guiStyle.normal.textColor=selected==9?Color.white:new Color(.16f,.12f,.07f);
        GUILayout.BeginArea(new Rect(Mathf.Max(8,(Screen.width-460)*.5f),panelY,460,panelHeight),selected==9?GUI.skin.box:GUIStyle.none);
        if(planning||reservationsReady)
        {
            GUILayout.Label(planning?$"야리나오시다 · 예약 위치 {Board.Premoves(Board.turn).Length}/6": "예약 완료 · 이번 턴에 돌 1개를 직접 두세요",guiStyle);
            GUILayout.Label("6곳을 순서대로 클릭 · 다음 내 두 턴에 3개씩 자동 착수 · 상대에게 예약은 보이지 않습니다",guiStyle);
            GUILayout.EndArea();return;
        }
        GUILayout.Label(Status,guiStyle);
        if(Shotgun.active)
        {
            GUILayout.Label($"남은 시간 {Shotgun.Remaining(Now):0.0}초 · {Shotgun.hits}회 내려찍기 · 같은 위치도 반복 타격 가능",guiStyle);
            GUILayout.EndArea();return;
        }
        GUILayout.Label(selected==9?"마우스로 조준 · 누르면 힘이 빠르게 왕복 · 놓으면 표시된 착탄점으로 발사":selected==3?"내 돌을 뒤로 당긴 거리만큼 힘이 쌓입니다 · 놓으면 반대 방향으로 발사":AugmentAim.Directional(selected)?"대상을 누르고 원하는 방향으로 드래그한 뒤 놓기":"표시된 범위를 확인하고 클릭",guiStyle);
        if(selected==2)GUILayout.Label("움직이는 돌이 목표 칸 위에 올 때 클릭하세요.",guiStyle);
        GUILayout.Label(Meteor.active?$"남은 시간 {Meteor.Remaining(Now):0.0}초 · {Meteor.clicks}회 · 범위 {Meteor.Size}×{Meteor.Size}":selected==11?"낙하 위치 클릭 → 3초간 연타 → 자동 낙하 · 우클릭 / Esc 취소":$"세기 {power} · 우클릭 / Esc 취소",guiStyle);
        if(selected==9)
        {
            var landing=AugmentAim.CatapultLanding(new Vector2Int(9,18),new Vector2Int(dx,dy),power);
            GUILayout.Label(AugmentBoard.Inside(landing.x,landing.y)?$"예상 착탄 ({landing.x+1},{landing.y+1}) · 영향 범위 3×3":"예상 착탄: 판 밖 · 놓으면 발사 실패",guiStyle);
            float charge=dragging?AugmentAim.CatapultCharge(Time.unscaledTime-catapultChargeStarted):0;
            Rect gauge=GUILayoutUtility.GetRect(430,22);GUI.Box(gauge,GUIContent.none);
            Rect fill=new Rect(gauge.x+3,gauge.y+3,(gauge.width-6)*charge,gauge.height-6);
            Color old=GUI.color;GUI.color=Color.Lerp(new Color(.25f,.85f,.35f),new Color(1,.25f,.12f),charge);GUI.DrawTexture(fill,Texture2D.whiteTexture);GUI.color=old;
            GUI.Label(gauge,$"힘 게이지  {power}/{AugmentAim.CatapultMaxPower}",new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,font=guiStyle.font});
        }
        GUILayout.EndArea();
    }
    private static Font GuiFont=>Resources.Load<MultiplayerMenuAssets>("MultiplayerMenuAssets")?.Font?.sourceFontFile??Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    public async void ReturnFromReview()
    {
        MatchReviewSession.Clear();if(multiplayer.Active||multiplayer.IsConnecting)await multiplayer.Leave();else SceneManager.LoadSceneAsync(AugmentMultiplayer.HomeScene);
    }
}
