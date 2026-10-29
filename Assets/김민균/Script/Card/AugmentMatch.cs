using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

/// <summary>One input path for local play and host-validated multiplayer.</summary>
public sealed class AugmentMatch : MonoBehaviour
{
    public static AugmentMatch Instance { get; private set; }
    public bool ApplyingCommand { get; private set; }
    public CardManager Cards { get; private set; }
    public AugmentBoard Board { get; private set; } = new AugmentBoard();
    public int Revision { get; private set; }
    public IReadOnlyList<string> History => history;
    public bool Waiting;
    public string Status = "";
    private 가이드_돌_코드 guide;
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
    public MeteorCharge Meteor { get; private set; } = new MeteorCharge();
    private static double Now => Time.realtimeSinceStartupAsDouble;
    public int LocalSide => multiplayer != null && multiplayer.Active ? (multiplayer.IsHost?1:2) : 0;
    public bool CanControl(int side) => !Waiting && (LocalSide==0||LocalSide==side);
    [Serializable] public class Command { public string kind; public int side,id,x,y,dx,dy,power,revision; }
    [Serializable] public class Snapshot { public AugmentBoard board; public CardManager.MatchCards cards; public int revision; public string status; public string[] history; public Command effect; public int effectRevision; public MeteorCharge meteor; public double meteorRemaining; }

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
        var turns=FindAnyObjectByType<수_놓기>();
        // Copy board geometry before stopping the former placement loop.
        if(turns!=null){turns.이제너의턴();turns.StopAllCoroutines();turns.enabled=false;}
        if(guide!=null){guide.StopAllCoroutines();guide.enabled=false;guide.GetComponent<SpriteRenderer>().enabled=false;}
        var debugInput=FindAnyObjectByType<테스트용>();if(debugInput!=null)debugInput.enabled=false;
        var bridge=GetComponent<CardMatchBridge>();if(bridge!=null){bridge.StopAllCoroutines();bridge.enabled=false;}
        multiplayer=gameObject.AddComponent<AugmentMultiplayer>();
        var targetRoot=new GameObject("AugmentTargetPreview");targetRoot.transform.SetParent(transform,false);
        targetView=targetRoot.AddComponent<AugmentTargetView>();
    }
    private void Start(){ResetMatch(Cards.CurrentSeed);}
    public void ResetMatch(int seed)
    {
        Board=new AugmentBoard();Revision=0;selected=-1;history.Clear();Waiting=false;
        dragging=false;targetView?.Hide();
        Meteor=new MeteorCharge();
        lastEffect=null;effectRevision=0;
        foreach(var effect in GetComponentsInChildren<AugmentEffectView>())Destroy(effect.gameObject);
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
        if(Meteor.active||!CanControl(black?1:2)||Cards.IsChoosing||!Cards.CanUseCard(black)||Board.winner!=0)return;
        selected=id;pieceMove=false;power=1;dx=1;dy=0;dragging=false;
        shotgunAim=Mouse.current!=null&&Camera.main!=null?MouseWorld():guide.centerPos;
        var initialAim=Cell(shotgunAim);if(!AugmentBoard.Inside(initialAim.x,initialAim.y))shotgunAim=guide.centerPos;
        Status=$"{Cards.CardList.Find(c=>c.Name==id).DisplayName}: 대상 교차점을 클릭하세요";
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
        if(Waiting||(command.revision!=Revision&&command.kind!="meteor-tap")||Board.winner!=0)return false;
        if(Meteor.active&&command.kind!="meteor-tap"&&command.kind!="meteor-impact"&&command.kind!="meteor-cancel"&&command.kind!="surrender")return false;
        bool success=false;
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
                if(success){Board=trial;Cards.UseCard(11,command.side==1);usedAbility=11;Meteor.active=false;}
            }
            else if(command.kind=="surrender" && (command.side==1||command.side==2)){Board.winner=3-command.side;Meteor.active=false;success=true;}
            else if(command.kind=="select")success=Cards.SelectCard(command.side==1,command.id);
            else if(!Cards.IsChoosing)
            {
                if(command.kind=="ability"&&command.id!=11&&command.side==Board.turn&&Cards.CanUseCard(command.side==1)&&Cards.CheckCard(command.id,command.side==1))
                {
                    var trial=JsonUtility.FromJson<AugmentBoard>(JsonUtility.ToJson(Board));
                    success=trial.Ability(command.id,command.x,command.y,command.dx,command.dy,command.power,Cards.RandomInRange);
                    if(success){Board=trial;Cards.UseCard(command.id,command.side==1);usedAbility=Board.lastAbility;}
                }
                else if(command.kind=="place"&&command.side==(Board.takeover==Board.turn?3-Board.turn:Board.turn))
                {
                    success=Board.Place(command.x,command.y,Cards.CardUsedThisTurn);
                    if(success){history.Add($"{history.Count+1}. {(command.side==1?"흑":"백")} ({command.x+1},{command.y+1})");Cards.BeginTurn(Board.turn==1);Cards.NotifyStonePlaced();}
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
        Revision++;selected=Meteor.active&&CanControl(Meteor.side)?11:-1;pieceMove=false;dragging=false;targetView?.Hide();
        if(Board.winner==0&&Array.TrueForAll(Board.cells,c=>c!=0))
        {
            int comparison=Cards.CompareCardGradeTotals();Board.winner=comparison==0?3:comparison>0?2:1;
        }
        Status=Meteor.active?"운석 충전 중 · 마우스 왼쪽 버튼을 연타하세요":Board.winner==3?"무승부":Board.winner!=0?$"{(Board.winner==1?"흑":"백")} 승리":$"{(Board.turn==1?"흑":"백")} 차례";
        if(Board.winner!=0)Cards.EndMatch();
        Render();
        if(usedAbility>=0)
        {
            lastEffect=new Command{kind="effect",id=usedAbility,x=command.x,y=command.y,dx=command.dx,dy=command.dy,power=command.power};effectRevision=Revision;
            AugmentEffectView.Play(transform,guide,usedAbility,command.x,command.y,command.power,before,Board.cells,new Vector2Int(command.dx,command.dy));
        }
        return true;
    }
    public string Capture()=>JsonUtility.ToJson(new Snapshot{board=Board,cards=Cards.Capture(),revision=Revision,status=Status,history=history.ToArray(),effect=lastEffect,effectRevision=effectRevision,meteor=Meteor,meteorRemaining=Meteor.Remaining(Now)});
    public void Restore(string json)
    {
        var state=JsonUtility.FromJson<Snapshot>(json);
        int[] before=(int[])Board.cells.Clone();bool showEffect=state.effect!=null&&state.effectRevision>effectRevision&&state.effectRevision==state.revision;
        Board=state.board;Revision=state.revision;Status=state.status;lastEffect=state.effect;effectRevision=state.effectRevision;
        Meteor=state.meteor??new MeteorCharge();if(Meteor.active)Meteor.deadline=Now+state.meteorRemaining;
        selected=Meteor.active&&CanControl(Meteor.side)?11:-1;pieceMove=false;dragging=false;targetView?.Hide();Waiting=false;history.Clear();history.AddRange(state.history);
        Cards.Restore(state.cards);Render();
        if(showEffect)AugmentEffectView.Play(transform,guide,lastEffect.id,lastEffect.x,lastEffect.y,lastEffect.power,before,Board.cells,new Vector2Int(lastEffect.dx,lastEffect.dy));
    }
    private void Update()
    {
        if(Meteor.active&&!Waiting&&(!multiplayer.Active||multiplayer.IsHost)&&Meteor.Remaining(Now)<=0)
            Submit(new Command{kind="meteor-impact",side=Meteor.side});
        if(Mouse.current==null||Camera.main==null||guide==null)return;
        if(Mouse.current.rightButton.wasPressedThisFrame||(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame))
        {
            if(Meteor.active&&CanControl(Meteor.side))Submit(new Command{kind="meteor-cancel",side=Meteor.side});
            selected=-1;pieceMove=false;dragging=false;targetView?.Hide();
        }
        Vector3 world=MouseWorld();
        shotgunAim=Vector3.MoveTowards(shotgunAim,world,Time.unscaledDeltaTime*guide.gap*2);
        bool overUI=EventSystem.current!=null&&EventSystem.current.IsPointerOverGameObject();
        HandlePointer(world,Mouse.current.leftButton.wasPressedThisFrame,Mouse.current.leftButton.wasReleasedThisFrame,overUI);
        UpdatePreview();
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
        sr.enabled=false;targetView?.Hide();
        if(Waiting||Cards.IsChoosing||Board.winner!=0)return;
        bool overUI=EventSystem.current!=null&&EventSystem.current.IsPointerOverGameObject();
        if(overUI&&!dragging&&!Meteor.active)return;
        Vector3 world=MouseWorld();var cell=Meteor.active?new Vector2Int(Meteor.x,Meteor.y):dragging?dragOrigin:HoverCell(world);
        if(!AugmentBoard.Inside(cell.x,cell.y))return;
        if(selected>=0||Meteor.active)
        {
            var direction=new Vector2Int(dx,dy);
            var cells=AugmentAim.Cells(Meteor.active?11:selected,cell,direction,Meteor.active?MeteorCharge.Power(Meteor.clicks):selected==11?2:power);
            bool valid=ValidTarget(cell,direction);
            Color color=selected==1||!valid?new Color(1,.12f,.12f):new Color(.25f,1,.55f);
            targetView.Show(cells,guide.centerPos,guide.gap,color,dragging?(Vector2Int?)dragOrigin:null,direction,selected==17?1:power,
                dragging&&selected==3?(Vector3?)world:null);
            return;
        }
        sr.enabled=true;
        guide.transform.position=new Vector3(guide.centerPos.x+(cell.x-9)*guide.gap,guide.centerPos.y-(cell.y-9)*guide.gap,0);
        sr.sprite=Board.turn==1?guide.검은돌이미지:guide.흰돌이미지;sr.color=new Color(1,1,1,.5f);
    }
    private bool ValidTarget(Vector2Int cell,Vector2Int direction)
    {
        int value=Board.At(cell.x,cell.y);
        if(selected==6)
        {for(int y=0;y<2;y++)for(int x=0;x<2;x++)if(!AugmentBoard.Inside(cell.x+x,cell.y+y)||Board.At(cell.x+x,cell.y+y)!=0)return false;return true;}
        if(selected==10||selected==16)return value==0;
        if(selected==2||selected==11||selected==14)return value!=3&&value!=5;
        if(selected==3||selected==9||selected==17)
        {
            if(selected==3&&value!=Board.turn)return false;
            if(selected==17&&(value==0||value==3||value==5))return false;
            if(!dragging)return true;
            if(direction==Vector2Int.zero)return false;
            Vector2Int destination=cell+direction*(selected==17?1:power);
            if(!AugmentBoard.Inside(destination.x,destination.y))return false;
            int target=Board.At(destination.x,destination.y);
            return selected==17?target==0:target!=3&&target!=5;
        }
        return true;
    }
    private void Render()
    {
        foreach(var visual in visuals)if(visual!=null)Destroy(visual);visuals.Clear();
        if(guide==null||guide.돌==null)return;
        for(int p=0;p<361;p++)
        {
            int c=Board.cells[p];if(c==0||(Board.hidden[p]!=0&&LocalSide!=0&&Board.hidden[p]!=LocalSide))continue;
            var go=Instantiate(guide.돌,new Vector3(guide.centerPos.x+(p%19-9)*guide.gap,guide.centerPos.y-(p/19-9)*guide.gap,0),Quaternion.identity);
            var sr=go.GetComponent<SpriteRenderer>();sr.sprite=c==2?guide.흰돌이미지:guide.검은돌이미지;
            sr.color=c==3?new Color(.6f,.4f,.15f):c==4?Color.gray:c==5?new Color(.25f,.1f,.35f):Color.white;
            bool giant=Board.giant[1]==p||Board.giant[2]==p;
            if(c>=3||Board.piece==p||giant)
            {
                sr.sprite=AugmentEffectView.Square;
                go.transform.localScale=Vector3.one*guide.gap*(giant?1.35f:.85f);
                if(giant)sr.color=AugmentEffectView.Tint(14);
                else if(Board.piece==p)sr.color=AugmentEffectView.Tint(7);
            }
            visuals.Add(go);
        }
        var ui=FindAnyObjectByType<CardSideTabUI>();if(ui!=null)ui.RefreshAll();
    }
    private void OnGUI()
    {
        // Ordinary play keeps the original scene UI. Show controls only while targeting an ability.
        if(selected<0&&!pieceMove&&!Meteor.active)return;
        if(guiStyle==null){guiStyle=new GUIStyle(GUI.skin.label){wordWrap=true};guiStyle.font=Font.CreateDynamicFontFromOSFont("Malgun Gothic",15);}
        GUI.skin.label.font=guiStyle.font;GUI.skin.button.font=guiStyle.font;GUI.skin.textField.font=guiStyle.font;
        GUILayout.BeginArea(new Rect(Mathf.Max(8,(Screen.width-460)*.5f),Mathf.Max(8,Screen.height-88),460,80),GUI.skin.box);
        GUILayout.Label(Status,guiStyle);
        GUILayout.Label(selected==3?"내 돌을 누르고 뒤로 당긴 뒤 놓기 · 반대 방향으로 발사":AugmentAim.Directional(selected)?"대상을 누르고 원하는 방향으로 드래그한 뒤 놓기":"표시된 범위를 확인하고 클릭",guiStyle);
        if(selected==2)GUILayout.Label("움직이는 돌이 목표 칸 위에 올 때 클릭하세요.",guiStyle);
        GUILayout.Label(Meteor.active?$"남은 시간 {Meteor.Remaining(Now):0.0}초 · {Meteor.clicks}회 · 범위 {Meteor.Size}×{Meteor.Size}":selected==11?"낙하 위치 클릭 → 3초간 연타 → 자동 낙하 · 우클릭 / Esc 취소":$"세기 {power} · 우클릭 / Esc 취소",guiStyle);
        GUILayout.EndArea();
    }
}
