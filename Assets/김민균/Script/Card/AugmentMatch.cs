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
    private readonly List<string> history = new List<string>();
    private AugmentMultiplayer multiplayer;
    private int selected = -1, dx=1,dy, power=1;
    private bool pieceMove;
    private Vector3 shotgunAim;
    private GUIStyle guiStyle;
    private Command lastEffect;
    private int effectRevision;
    private AugmentTargetView targetView;
    private bool dragging;
    private Vector2Int dragOrigin;
    private float catapultChargeStarted;
    private SpriteRenderer catapultPreview;
    public MeteorCharge Meteor { get; private set; } = new MeteorCharge();
    public MatchClock Clock { get; private set; } = new MatchClock();
    private TMP_Text blackClockLabel,whiteClockLabel;
    private float clockBroadcastAccumulator;
    private bool resultQueued;
    private bool lastCommandRevealedHiddenStone;
    private static double Now => Time.realtimeSinceStartupAsDouble;
    public int LocalSide => multiplayer != null && multiplayer.Active ? (multiplayer.IsHost?1:2) : 0;
    public int ViewingSide=>LocalSide!=0?LocalSide:Board.takeover==Board.turn?3-Board.turn:Board.turn;
    public bool CanControl(int side) => !IsReviewMode&&!Waiting&&!AugmentEffectView.IsGolemSummoning(transform) && (LocalSide==0||LocalSide==side);
    [Serializable] public class Command { public string kind; public int side,id,x,y,dx,dy,power,revision; }
    [Serializable] public class Snapshot { public AugmentBoard board; public CardManager.MatchCards cards; public int revision; public string status; public string[] history; public Command effect; public int effectRevision; public MeteorCharge meteor; public double meteorRemaining; public MatchClock clock; public bool hiddenStoneRevealed; }

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
        StopAllCoroutines();IsReviewMode=false;ReviewIndex=-1;MatchReviewSession.BeginMatch();
        resultQueued=false;
        lastCommandRevealedHiddenStone=false;
        Board=new AugmentBoard();Revision=0;selected=-1;history.Clear();Waiting=false;
        dragging=false;targetView?.Hide();if(catapultPreview!=null)catapultPreview.enabled=false;
        Meteor=new MeteorCharge();
        Clock=new MatchClock();Clock.Reset(multiplayer!=null?multiplayer.TimeLimitSeconds:MatchClock.DefaultSeconds);clockBroadcastAccumulator=0f;UpdateClockUI();
        lastEffect=null;effectRevision=0;
        foreach(var effect in GetComponentsInChildren<AugmentEffectView>()){effect.gameObject.SetActive(false);Destroy(effect.gameObject);}
        ApplyingCommand=true;try{Cards.ResetMatch(seed);}finally{ApplyingCommand=false;}
        Status="흑 차례 · 증강을 선택하세요";Render();
    }
    private void OnDestroy(){if(Instance==this)Instance=null;}
    public bool RequestSelection(bool black,int slot)
    {
        int side=black?1:2;if(!CanControl(side))return false;
        return Submit(new Command{kind="select",side=side,id=slot});
    }
    public void ChooseAbility(bool black,int id)
    {
        if(Meteor.active||!CanControl(black?1:2)||Cards.IsChoosing||!Cards.CanUseCard(black)||!Cards.CheckCard(id,black)||Board.winner!=0)return;
        selected=id;pieceMove=false;power=1;dx=1;dy=0;dragging=false;
        shotgunAim=Mouse.current!=null&&Camera.main!=null?MouseWorld():guide.centerPos;
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
        if(AugmentEffectView.IsGolemSummoning(transform)&&command.kind!="surrender"&&command.kind!="timeout")return false;
        if(IsReviewMode||Waiting||(command.revision!=Revision&&command.kind!="meteor-tap")||Board.winner!=0)return false;
        if(Meteor.active&&command.kind!="meteor-tap"&&command.kind!="meteor-impact"&&command.kind!="meteor-cancel"&&command.kind!="surrender"&&command.kind!="timeout")return false;
        bool success=false;
        bool revealedHiddenStone=false;
        int[] before=(int[])Board.cells.Clone();
        int usedAbility=-1;
        ApplyingCommand=true;
        try
        {
            if(command.kind=="meteor-tap")success=Meteor.Tap(command.side,Now);
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
            {Board.winner=3-command.side;Meteor.active=false;success=true;}
            else if(command.kind=="select")success=Cards.SelectCard(command.side==1,command.id);
            else if(!Cards.IsChoosing)
            {
                if(command.kind=="ability"&&command.id!=11&&command.side==Board.turn&&Cards.CanUseCard(command.side==1)&&Cards.CheckCard(command.id,command.side==1))
                {
                    var trial=JsonUtility.FromJson<AugmentBoard>(JsonUtility.ToJson(Board));
                    success=trial.Ability(command.id,command.x,command.y,command.dx,command.dy,command.power,Cards.RandomInRange);
                    if(success)
                    {
                        success=Cards.UseCard(command.id,command.side==1);
                        if(success){Board=trial;usedAbility=Board.lastAbility;}
                    }
                }
                else if(command.kind=="place"&&command.side==(Board.takeover==Board.turn?3-Board.turn:Board.turn))
                {
                    success=Board.Place(command.x,command.y,Cards.CardUsedThisTurn,out revealedHiddenStone);
                    if(success)
                    {
                        if(!revealedHiddenStone)history.Add($"{history.Count+1}. {(Board.At(command.x,command.y)==1?"흑":Board.At(command.x,command.y)==2?"백":"회색")} ({command.x+1},{command.y+1})");
                        Cards.BeginTurn(Board.turn==1);
                        if(!revealedHiddenStone)Cards.NotifyStonePlaced();
                    }
                }
                else if(command.kind=="piece"&&command.side==Board.turn)
                {
                    success=Board.MovePiece(command.x,command.y);
                    if(success){Cards.BeginTurn(Board.turn==1);Cards.NotifyStonePlaced();}
                }
            }
        }
        finally{ApplyingCommand=false;}
        if(!success){Status="사용할 수 없는 위치 또는 차례입니다. 다른 위치를 고르세요.";return false;}
        if(command.kind=="place"&&!revealedHiddenStone)GameAudio.Instance.PlayStone();
        Revision++;lastCommandRevealedHiddenStone=revealedHiddenStone;selected=Meteor.active&&CanControl(Meteor.side)?11:-1;pieceMove=false;dragging=false;targetView?.Hide();
        if(Board.winner==0&&Array.TrueForAll(Board.cells,c=>c!=0))
        {
            int comparison=Cards.CompareCardGradeTotals();Board.winner=comparison==0?3:comparison>0?2:1;
        }
        Status=command.kind=="timeout"?$"{(command.side==1?"흑":"백")} 시간 초과 · {(Board.winner==1?"흑":"백")} 승리":revealedHiddenStone&&Board.winner==0?$"투명돌 발견 · 착수 실패, {(Board.turn==1?"흑":"백")} 차례":Meteor.active?"운석 충전 중 · 마우스 왼쪽 버튼을 연타하세요":Board.winner==3?"무승부":Board.winner!=0?$"{(Board.winner==1?"흑":"백")} 승리":$"{(Board.turn==1?"흑":"백")} 차례";
        if(Board.winner!=0)Cards.EndMatch();
        Render();
        if(usedAbility>=0&&!AugmentVisibility.IsSecret(usedAbility))
        {
            lastEffect=new Command{kind="effect",id=usedAbility,x=command.x,y=command.y,dx=command.dx,dy=command.dy,power=command.power};effectRevision=Revision;
            AugmentEffectView.Play(transform,guide,usedAbility,command.x,command.y,command.power,before,Board.cells,new Vector2Int(command.dx,command.dy));
        }
        if(revealedHiddenStone)MatchReviewSession.Record("착수 실패 · 투명돌 발견",Capture());
        else if(command.kind=="place"&&history.Count>0)MatchReviewSession.Record(history[history.Count-1],Capture());
        else if(usedAbility>=0&&!AugmentVisibility.IsSecret(usedAbility)||command.kind=="piece")MatchReviewSession.Record(command.kind=="piece"?"기물 이동":$"{(command.side==1?"흑":"백")} · {Cards.CardList.Find(c=>c.Name==usedAbility)?.DisplayName ?? "증강"}",Capture());
        if(Board.winner!=0)QueueResult();
        return true;
    }
    public string Capture()=>JsonUtility.ToJson(new Snapshot{board=Board,cards=Cards.Capture(),revision=Revision,status=Status,history=history.ToArray(),effect=lastEffect,effectRevision=effectRevision,meteor=Meteor,meteorRemaining=Meteor.Remaining(Now),clock=Clock,hiddenStoneRevealed=lastCommandRevealedHiddenStone});
    public string CaptureForSide(int side)
    {
        var state=JsonUtility.FromJson<Snapshot>(Capture());
        // Do not send an opponent's queued traps or hidden pieces to this player's client.
        if(state.board.grayNext==side)state.board.grayNext=0;
        if(state.board.convertNext==side)state.board.convertNext=0;
        if(state.board.takeover==side&&state.board.turn!=side)state.board.takeover=0;
        for(int p=0;p<state.board.cells.Length;p++)
            if(state.board.hidden[p]!=0&&state.board.hidden[p]!=side)
            {state.board.cells[p]=0;state.board.hidden[p]=0;state.board.order[p]=0;}
        int[] ids=side==1?state.cards.white:state.cards.black;
        bool[] used=side==1?state.cards.whiteUsed:state.cards.blackUsed;
        for(int i=0;i<ids.Length;i++)if(AugmentVisibility.IsSecret(ids[i]))used[i]=false;
        if(AugmentVisibility.IsSecret(state.board.lastAbility))
        {
            if(state.cards.turnBlack!=(side==1))state.cards.used=false;
            state.board.lastAbility=-1;
        }
        if(state.effect!=null&&AugmentVisibility.IsSecret(state.effect.id))state.effect=null;
        return JsonUtility.ToJson(state);
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
            UpdateClockUI();return;
        }
        int[] before=(int[])Board.cells.Clone();bool showEffect=state.effect!=null&&state.effectRevision>effectRevision&&state.effectRevision==state.revision;
        Board=state.board;Revision=state.revision;Status=state.status;lastEffect=state.effect;effectRevision=state.effectRevision;
        lastCommandRevealedHiddenStone=state.hiddenStoneRevealed;
        if(!IsReviewMode&&!state.hiddenStoneRevealed&&state.history!=null&&state.history.Length==previousHistory+1)GameAudio.Instance.PlayStone();
        Waiting=false;Meteor=state.meteor??new MeteorCharge();if(Meteor.active)Meteor.deadline=Now+state.meteorRemaining;
        Clock=state.clock??new MatchClock();UpdateClockUI();
        selected=Meteor.active&&CanControl(Meteor.side)?11:-1;pieceMove=false;dragging=false;targetView?.Hide();Waiting=false;history.Clear();history.AddRange(state.history);
        Cards.Restore(state.cards);Render();
        if(showEffect&&!IsReviewMode)AugmentEffectView.Play(transform,guide,lastEffect.id,lastEffect.x,lastEffect.y,lastEffect.power,before,Board.cells,new Vector2Int(lastEffect.dx,lastEffect.dy));
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
        MatchReviewSession.Complete(Board.winner,LocalSide,Capture());StartCoroutine(OpenResult());
    }
    private System.Collections.IEnumerator OpenResult()
    {
        yield return new WaitForSecondsRealtime(.45f);SceneManager.LoadSceneAsync(MatchReviewSession.ResultScene);
    }
    private void Update()
    {
        if(IsReviewMode){UpdateClockUI();return;}
        UpdateClock();
        if(Meteor.active&&!Waiting&&(!multiplayer.Active||multiplayer.IsHost)&&Meteor.Remaining(Now)<=0)
            Submit(new Command{kind="meteor-impact",side=Meteor.side});
        if(AudioSettings.IsOpen){dragging=false;targetView?.Hide();return;}
        if(Mouse.current==null||Camera.main==null||guide==null)return;
        if(Mouse.current.rightButton.wasPressedThisFrame||(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame))
        {
            if(Meteor.active&&CanControl(Meteor.side))Submit(new Command{kind="meteor-cancel",side=Meteor.side});
            selected=-1;pieceMove=false;dragging=false;targetView?.Hide();if(catapultPreview!=null)catapultPreview.enabled=false;
        }
        Vector3 world=MouseWorld();
        shotgunAim=Vector3.MoveTowards(shotgunAim,world,Time.unscaledDeltaTime*guide.gap*2);
        bool overUI=EventSystem.current!=null&&EventSystem.current.IsPointerOverGameObject();
        HandlePointer(world,Mouse.current.leftButton.wasPressedThisFrame,Mouse.current.leftButton.wasReleasedThisFrame,overUI);
        UpdatePreview();
    }
    private void UpdateClock()
    {
        bool running=!IsReviewMode&&!Waiting&&Board.winner==0&&!Cards.IsChoosing&&!AugmentEffectView.IsGolemSummoning(transform);
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
    private Vector3 MouseWorld(){Vector3 p=Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());p.z=0;return p;}
    private Vector2 Grid(Vector3 world)=>new Vector2((world.x-guide.centerPos.x)/guide.gap+9,9-(world.y-guide.centerPos.y)/guide.gap);
    private Vector2Int Cell(Vector3 world){Vector2 p=Grid(world);return new Vector2Int(Mathf.RoundToInt(p.x),Mathf.RoundToInt(p.y));}
    private Vector2Int HoverCell(Vector3 world)
    {
        var cell=Cell(selected==1?shotgunAim:world);
        if(selected==2)cell.x+=Mathf.RoundToInt(Mathf.Sin(Time.unscaledTime*4f)*2);
        return cell;
    }
    private void HandlePointer(Vector3 world,bool pressed,bool released,bool overUI)
    {
        if(Waiting||Cards.IsChoosing||Board.winner!=0){dragging=false;return;}
        int side=selected>=0||pieceMove?Board.turn:Board.takeover==Board.turn?3-Board.turn:Board.turn;
        if(!CanControl(side))return;
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
            Vector2 launcherGrid=new Vector2(9,20);
            if(pressed&&!overUI){dragOrigin=new Vector2Int(9,18);dragging=true;catapultChargeStarted=Time.unscaledTime;}
            if(!dragging)return;
            bool aimed=AugmentAim.Drag(launcherGrid,Grid(world),false,out Vector2Int direction,out _);
            if(aimed){dx=direction.x;dy=direction.y;}
            power=Mathf.Clamp(1+Mathf.FloorToInt((Time.unscaledTime-catapultChargeStarted)/.4f),1,AugmentAim.CatapultMaxPower);
            if(!released)return;
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
                if(selected==17&&(stone==0||stone==3||stone==5)){Status="이동할 수 있는 돌을 골라주세요.";return;}
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
        if(guide==null||Mouse.current==null||Camera.main==null)return;
        var sr=guide.GetComponent<SpriteRenderer>();
        sr.enabled=false;targetView?.Hide();if(catapultPreview!=null)catapultPreview.enabled=false;
        if(IsReviewMode||Waiting||Cards.IsChoosing||Board.winner!=0||AugmentEffectView.IsGolemSummoning(transform))return;
        int controllingSide=selected>=0||pieceMove?Board.turn:Board.takeover==Board.turn?3-Board.turn:Board.turn;
        if(!CanControl(controllingSide))return;
        bool overUI=EventSystem.current!=null&&EventSystem.current.IsPointerOverGameObject();
        if(overUI&&!dragging&&!Meteor.active)return;
        Vector3 world=MouseWorld();var cell=Meteor.active?new Vector2Int(Meteor.x,Meteor.y):selected==9?new Vector2Int(9,18):dragging?dragOrigin:HoverCell(world);
        if(!AugmentBoard.Inside(cell.x,cell.y))return;
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
                // 투석은 힘과 방향만 보여 주고 정확한 착지 칸은 숨긴다.
                targetView?.Hide();return;
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
        int value=Board.VisibleAt(cell.x,cell.y,ViewingSide);
        if(selected==6)
        {for(int y=0;y<2;y++)for(int x=0;x<2;x++)if(!AugmentBoard.Inside(cell.x+x,cell.y+y)||Board.VisibleAt(cell.x+x,cell.y+y,ViewingSide)!=0)return false;return true;}
        if(selected==10||selected==16)return value==0;
        if(selected==2||selected==11||selected==14)return value!=3&&value!=5;
        if(selected==3||selected==9||selected==17)
        {
            if(selected==3&&value!=Board.turn)return false;
            if(selected==17&&(value==0||value==3||value==5))return false;
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
    private void Render()
    {
        foreach(var visual in visuals)if(visual!=null)Destroy(visual);visuals.Clear();
        if(guide==null||guide.돌==null)return;
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
            visuals.Add(large);
            for(int oy=0;oy<2;oy++)for(int ox=0;ox<2;ox++)coveredByLargeStone[(ay+oy)*19+ax+ox]=true;
        }
        for(int p=0;p<361;p++)
        {
            if(coveredByLargeStone[p])continue;
            int c=Board.cells[p];if(c==0||(Board.hidden[p]!=0&&Board.hidden[p]!=ViewingSide))continue;
            if(c==5)continue;
            bool giant=Board.giant[1]==p||Board.giant[2]==p;
            if(giant)
            {
                var golem=AugmentEffectView.CreateGolemIdle(transform,new Vector3(guide.centerPos.x+(p%19-9)*guide.gap,guide.centerPos.y-(p/19-9)*guide.gap,-.08f),guide.gap);
                if(golem!=null)visuals.Add(golem);
                continue;
            }
            var go=Instantiate(guide.돌,new Vector3(guide.centerPos.x+(p%19-9)*guide.gap,guide.centerPos.y-(p/19-9)*guide.gap,0),Quaternion.identity);
            var sr=go.GetComponent<SpriteRenderer>();sr.sprite=c==2||c==4?guide.흰돌이미지:guide.검은돌이미지;
            sr.color=c==3?new Color(.6f,.4f,.15f):c==4?Color.gray:c==5?new Color(.25f,.1f,.35f):Color.white;
            if(Board.hidden[p]!=0)sr.color=new Color(1,1,1,.45f);
            if(c==3&&AugmentEffectView.Artwork!=null&&AugmentEffectView.Artwork.MeteorRock!=null)
            {
                sr.sprite=AugmentEffectView.Artwork.MeteorRock;sr.color=Color.white;
                float rockScale=guide.gap*.9f/Mathf.Max(.01f,sr.sprite.bounds.size.x);go.transform.localScale=Vector3.one*rockScale;
            }
            else if(c==3||c==5||Board.piece==p||giant)
            {
                sr.sprite=AugmentEffectView.Square;
                go.transform.localScale=Vector3.one*guide.gap*(giant?1.35f:.85f);
                if(giant)sr.color=AugmentEffectView.Tint(14);
                else if(Board.piece==p)sr.color=AugmentEffectView.Tint(7);
            }
            visuals.Add(go);
        }
        RenderBlackHoles();
        var ui=FindAnyObjectByType<CardSideTabUI>();if(ui!=null)ui.RefreshAll();
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
            if(guiStyle==null){guiStyle=new GUIStyle(GUI.skin.label){wordWrap=true};guiStyle.font=Font.CreateDynamicFontFromOSFont("Malgun Gothic",15);}
            GUI.skin.label.font=GUI.skin.button.font=guiStyle.font;
            GUILayout.BeginArea(new Rect(Mathf.Max(8,(Screen.width-500)*.5f),Screen.height-92,500,84),GUI.skin.box);
            GUILayout.Label(Status,guiStyle);GUILayout.BeginHorizontal();
            if(GUILayout.Button("◀ 이전"))ShowReviewMove(Mathf.Max(0,ReviewIndex-1));
            if(GUILayout.Button("다음 ▶"))ShowReviewMove(Mathf.Min(MatchReviewSession.Frames.Count-1,ReviewIndex+1));
            if(GUILayout.Button("결과 화면"))SceneManager.LoadSceneAsync(MatchReviewSession.ResultScene);
            if(GUILayout.Button("메인 화면"))ReturnFromReview();
            GUILayout.EndHorizontal();GUILayout.EndArea();return;
        }
        // Ordinary play keeps the original scene UI. Show controls only while targeting an ability.
        if(selected<0&&!pieceMove&&!Meteor.active)return;
        if(guiStyle==null){guiStyle=new GUIStyle(GUI.skin.label){wordWrap=true};guiStyle.font=Font.CreateDynamicFontFromOSFont("Malgun Gothic",15);}
        GUI.skin.label.font=guiStyle.font;GUI.skin.button.font=guiStyle.font;GUI.skin.textField.font=guiStyle.font;
        float panelHeight=selected==9?170:80;
        float panelY=selected==9?150:Mathf.Max(8,Screen.height-panelHeight-8);
        GUILayout.BeginArea(new Rect(Mathf.Max(8,(Screen.width-460)*.5f),panelY,460,panelHeight),GUI.skin.box);
        GUILayout.Label(Status,guiStyle);
        GUILayout.Label(selected==9?"판 아래 투석기에서 판 안쪽으로 조준 · 누르는 시간으로 세기 1~5 · 최대 충전은 판을 넘어갑니다":selected==3?"내 돌을 뒤로 당긴 거리만큼 힘이 쌓입니다 · 놓으면 반대 방향으로 발사":AugmentAim.Directional(selected)?"대상을 누르고 원하는 방향으로 드래그한 뒤 놓기":"표시된 범위를 확인하고 클릭",guiStyle);
        if(selected==2)GUILayout.Label("움직이는 돌이 목표 칸 위에 올 때 클릭하세요.",guiStyle);
        GUILayout.Label(Meteor.active?$"남은 시간 {Meteor.Remaining(Now):0.0}초 · {Meteor.clicks}회 · 범위 {Meteor.Size}×{Meteor.Size}":selected==11?"낙하 위치 클릭 → 3초간 연타 → 자동 낙하 · 우클릭 / Esc 취소":$"세기 {power} · 우클릭 / Esc 취소",guiStyle);
        if(selected==9)
        {
            float charge=dragging?Mathf.Clamp01((Time.unscaledTime-catapultChargeStarted)/1.6f):0;
            Rect gauge=GUILayoutUtility.GetRect(430,22);GUI.Box(gauge,GUIContent.none);
            Rect fill=new Rect(gauge.x+3,gauge.y+3,(gauge.width-6)*charge,gauge.height-6);
            Color old=GUI.color;GUI.color=Color.Lerp(new Color(.25f,.85f,.35f),new Color(1,.25f,.12f),charge);GUI.DrawTexture(fill,Texture2D.whiteTexture);GUI.color=old;
            GUI.Label(gauge,$"힘 게이지  {power}/{AugmentAim.CatapultMaxPower}",new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,font=guiStyle.font});
        }
        GUILayout.EndArea();
    }
    private async void ReturnFromReview()
    {
        MatchReviewSession.Clear();if(multiplayer.Active||multiplayer.IsConnecting)await multiplayer.Leave();else SceneManager.LoadSceneAsync(AugmentMultiplayer.HomeScene);
    }
}
