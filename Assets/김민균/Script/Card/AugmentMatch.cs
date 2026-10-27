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
    public int LocalSide => multiplayer != null && multiplayer.Active ? (multiplayer.IsHost?1:2) : 0;
    public bool CanControl(int side) => !Waiting && (LocalSide==0||LocalSide==side);
    [Serializable] public class Command { public string kind; public int side,id,x,y,dx,dy,power,revision; }
    [Serializable] public class Snapshot { public AugmentBoard board; public CardManager.MatchCards cards; public int revision; public string status; public string[] history; }

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
    }
    private void Start(){ResetMatch(Cards.CurrentSeed);}
    public void ResetMatch(int seed)
    {
        Board=new AugmentBoard();Revision=0;selected=-1;history.Clear();Waiting=false;
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
        if(!CanControl(black?1:2)||Cards.IsChoosing||!Cards.CanUseCard(black)||Board.winner!=0)return;
        selected=id;pieceMove=false;power=1;
        shotgunAim=guide.transform.position;
        Status=$"{Cards.CardList.Find(c=>c.Name==id).DisplayName}: 대상 교차점을 클릭하세요";
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
        if(Waiting||command.revision!=Revision||Board.winner!=0)return false;
        bool success=false;
        ApplyingCommand=true;
        try
        {
            if(command.kind=="surrender" && (command.side==1||command.side==2)){Board.winner=3-command.side;success=true;}
            else if(command.kind=="select")success=Cards.SelectCard(command.side==1,command.id);
            else if(!Cards.IsChoosing)
            {
                if(command.kind=="ability"&&command.side==Board.turn&&Cards.CanUseCard(command.side==1)&&Cards.CheckCard(command.id,command.side==1))
                {
                    var trial=JsonUtility.FromJson<AugmentBoard>(JsonUtility.ToJson(Board));
                    success=trial.Ability(command.id,command.x,command.y,command.dx,command.dy,command.power,Cards.RandomInRange);
                    if(success){Board=trial;Cards.UseCard(command.id,command.side==1);}
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
        Revision++;selected=-1;pieceMove=false;
        if(Board.winner==0&&Array.TrueForAll(Board.cells,c=>c!=0))
        {
            int comparison=Cards.CompareCardGradeTotals();Board.winner=comparison==0?3:comparison>0?2:1;
        }
        Status=Board.winner==3?"무승부":Board.winner!=0?$"{(Board.winner==1?"흑":"백")} 승리":$"{(Board.turn==1?"흑":"백")} 차례";
        if(Board.winner!=0)Cards.EndMatch();
        Render();return true;
    }
    public string Capture()=>JsonUtility.ToJson(new Snapshot{board=Board,cards=Cards.Capture(),revision=Revision,status=Status,history=history.ToArray()});
    public void Restore(string json)
    {
        var state=JsonUtility.FromJson<Snapshot>(json);Board=state.board;Revision=state.revision;Status=state.status;
        selected=-1;pieceMove=false;Waiting=false;history.Clear();history.AddRange(state.history);
        Cards.Restore(state.cards);Render();
    }
    private void Update()
    {
        if(selected==11&&Keyboard.current!=null&&Keyboard.current.spaceKey.wasPressedThisFrame)power=Math.Min(3,power+1);
        UpdatePreview();
        if(Waiting||Cards.IsChoosing||Board.winner!=0||Mouse.current==null||!Mouse.current.leftButton.wasPressedThisFrame)return;
        if((selected>=0||pieceMove)&&AbilityControlsRect.Contains(new Vector2(Mouse.current.position.ReadValue().x,Screen.height-Mouse.current.position.ReadValue().y)))return;
        if(EventSystem.current!=null&&EventSystem.current.IsPointerOverGameObject())return;
        Vector3 world=Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        int x=Mathf.RoundToInt((world.x-guide.centerPos.x)/guide.gap)+9,y=9-Mathf.RoundToInt((world.y-guide.centerPos.y)/guide.gap);
        if(selected==1){x=Mathf.RoundToInt((shotgunAim.x-guide.centerPos.x)/guide.gap)+9;y=9-Mathf.RoundToInt((shotgunAim.y-guide.centerPos.y)/guide.gap);}
        if(selected==2)x+=Mathf.RoundToInt(Mathf.Sin(Time.unscaledTime*4f)*2f);
        if(!AugmentBoard.Inside(x,y))return;
        int side=selected>=0||pieceMove?Board.turn:Board.takeover==Board.turn?3-Board.turn:Board.turn;
        if(!CanControl(side))return;
        if(selected<0&&!pieceMove&&Board.piece==y*19+x&&Board.pieceOwner==Board.turn)
        {
            pieceMove=true;Status="기물을 이동할 교차점을 클릭하세요. 이동하면 턴을 소모합니다.";return;
        }
        Submit(new Command{kind=selected>=0?"ability":pieceMove?"piece":"place",side=side,id=selected,x=x,y=y,dx=dx,dy=dy,power=power});
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
        sr.enabled=!Waiting&&!Cards.IsChoosing&&Board.winner==0;
        if(!sr.enabled)return;
        Vector3 target=Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());target.z=0;
        shotgunAim=Vector3.MoveTowards(shotgunAim,target,Time.unscaledDeltaTime*guide.gap*2);
        Vector3 pos=selected==1?shotgunAim:target;
        int x=Mathf.RoundToInt((pos.x-guide.centerPos.x)/guide.gap)+9,y=9-Mathf.RoundToInt((pos.y-guide.centerPos.y)/guide.gap);
        if(selected==2)x+=Mathf.RoundToInt(Mathf.Sin(Time.unscaledTime*4f)*2f);
        if(!AugmentBoard.Inside(x,y)){sr.enabled=false;return;}
        guide.transform.position=new Vector3(guide.centerPos.x+(x-9)*guide.gap,guide.centerPos.y-(y-9)*guide.gap,0);
        sr.sprite=Board.turn==1?guide.검은돌이미지:guide.흰돌이미지;sr.color=new Color(1,1,1,.5f);
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
            if(Board.giant[1]==p||Board.giant[2]==p)go.transform.localScale*=1.4f;
            visuals.Add(go);
        }
        var ui=FindAnyObjectByType<CardSideTabUI>();if(ui!=null)ui.RefreshAll();
    }
    private void OnGUI()
    {
        // Ordinary play keeps the original scene UI. Show controls only while targeting an ability.
        if(selected<0&&!pieceMove)return;
        if(guiStyle==null){guiStyle=new GUIStyle(GUI.skin.label){wordWrap=true};guiStyle.font=Font.CreateDynamicFontFromOSFont("Malgun Gothic",15);}
        GUI.skin.label.font=guiStyle.font;GUI.skin.button.font=guiStyle.font;GUI.skin.textField.font=guiStyle.font;
        GUILayout.BeginArea(AbilityControlsRect,GUI.skin.box);
        GUILayout.Label(Status,guiStyle);
        GUILayout.Label("증강: 보관 카드 클릭 → 대상 교차점 클릭\n방향과 세기는 아래에서 선택",guiStyle);
        if(selected==2)GUILayout.Label("움직이는 돌이 목표 칸 위에 올 때 클릭하세요.",guiStyle);
        if(selected==11)GUILayout.Label("스페이스를 연타해 운석을 강화한 뒤 클릭하세요.",guiStyle);
        GUILayout.BeginHorizontal();
        if(GUILayout.Button("←")){dx=-1;dy=0;}if(GUILayout.Button("→")){dx=1;dy=0;}
        if(GUILayout.Button("↑")){dx=0;dy=-1;}if(GUILayout.Button("↓")){dx=0;dy=1;}
        if(GUILayout.Button("↗")){dx=1;dy=-1;}if(GUILayout.Button("↘")){dx=1;dy=1;}
        GUILayout.EndHorizontal();
        power=Mathf.RoundToInt(GUILayout.HorizontalSlider(power,1,3));GUILayout.Label($"방향 ({dx},{dy}) · 세기 {power}",guiStyle);
        if(GUILayout.Button("선택 취소")){selected=-1;pieceMove=false;}
        if(Board.giant[1]>=0||Board.giant[2]>=0)GUILayout.Label($"거신병 경과: 흑 {Math.Max(0,Board.giantTurns[1]-1)}/3 · 백 {Math.Max(0,Board.giantTurns[2]-1)}/3",guiStyle);
        GUILayout.EndArea();
    }
    private Rect AbilityControlsRect => new Rect(Mathf.Max(8,(Screen.width-330)*.5f),Mathf.Max(8,Screen.height-220),330,212);
}
