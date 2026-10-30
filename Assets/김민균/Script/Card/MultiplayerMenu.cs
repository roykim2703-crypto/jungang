using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Functional room UI opened by the existing Home start button.</summary>
public sealed class MultiplayerMenu : MonoBehaviour
{
    private AugmentMultiplayer room;
    private GameObject modal,hud;
    private TMP_Text status,code,hudStatus;
    private TMP_InputField input;
    private Button host,join,local,start,leave,copy,close;
    private TMP_FontAsset font;
    private bool visible;
    private string error="";
    public static void Open()
    {
        var root=AugmentMultiplayer.Instance;
        var menu=root.GetComponent<MultiplayerMenu>();if(menu==null)menu=root.gameObject.AddComponent<MultiplayerMenu>();
        menu.visible=true;menu.error="";
    }
    private void Awake()
    {
        room=GetComponent<AugmentMultiplayer>();
        var assets=Resources.Load<MultiplayerMenuAssets>("MultiplayerMenuAssets");font=assets!=null?assets.Font:TMP_Settings.defaultFontAsset;
        Build();
    }
    private void Update()
    {
        bool home=SceneManager.GetActiveScene().path==AugmentMultiplayer.HomeScene;
        modal.SetActive(home&&visible);hud.SetActive(!home&&SceneManager.GetActiveScene().path==AugmentMultiplayer.GameScene);
        bool offline=!room.Active&&!room.IsConnecting;
        host.interactable=local.interactable=offline;
        input.interactable=offline;join.interactable=offline&&input.text.Trim().Length>=4;
        start.interactable=room.CanStart;leave.interactable=room.Active||room.IsConnecting;
        copy.interactable=room.Active&&!string.IsNullOrEmpty(room.InvitationCode);
        close.interactable=!room.IsConnecting;
        status.text=string.IsNullOrEmpty(error)?room.ConnectionStatus:error;
        code.text=string.IsNullOrEmpty(room.InvitationCode)?"초대코드 · 방을 만들면 표시됩니다":"초대코드  "+room.InvitationCode;
        string side=room.Active?(room.IsHost?"흑":"백"):"로컬";
        hudStatus.text=side+" · "+(room.Stage==AugmentMultiplayer.RoomStage.Playing&&AugmentMatch.Instance!=null?AugmentMatch.Instance.Status:room.Active?room.ConnectionStatus:"대국 진행 중");
    }
    private void Build()
    {
        var canvasObject=new GameObject("MultiplayerUI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform,false);
        var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=250;
        var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
        modal=Rect("RoomMenu",canvasObject.transform,Vector2.zero,Vector2.zero).gameObject;
        var full=(RectTransform)modal.transform;full.anchorMin=Vector2.zero;full.anchorMax=Vector2.one;full.offsetMin=full.offsetMax=Vector2.zero;
        modal.AddComponent<Image>().color=new Color(0,0,0,.65f);
        var panel=Rect("Panel",modal.transform,Vector2.zero,new Vector2(620,560));
        panel.gameObject.AddComponent<Image>().color=new Color(.98f,.94f,.84f);
        var outline=panel.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.23f,.17f,.1f);outline.effectDistance=new Vector2(3,-3);
        Label("Title",panel,new Vector2(0,230),new Vector2(540,45),"대국 시작",30);
        status=Label("Status",panel,new Vector2(0,170),new Vector2(540,65),"",19);
        host=Button("Host",panel,new Vector2(-135,95),new Vector2(250,48),"초대 방 만들기",async()=>
        {error="";try{await room.Connect(true);}catch(Exception e){error=e.Message;}});
        local=Button("Local",panel,new Vector2(135,95),new Vector2(250,48),"같은 기기에서 대국",()=>{visible=false;room.StartLocal();});
        var field=Rect("InviteCodeInput",panel,new Vector2(-60,30),new Vector2(400,48));
        field.gameObject.AddComponent<Image>().color=Color.white;input=field.gameObject.AddComponent<TMP_InputField>();
        var viewport=Rect("Viewport",field,Vector2.zero,new Vector2(376,44));viewport.gameObject.AddComponent<RectMask2D>();
        var entry=Label("Text",viewport,Vector2.zero,new Vector2(370,44),"",23);entry.alignment=TextAlignmentOptions.MidlineLeft;
        var placeholder=Label("Placeholder",viewport,Vector2.zero,new Vector2(370,44),"친구의 초대코드 입력",20);placeholder.color=new Color(.55f,.5f,.43f);placeholder.alignment=TextAlignmentOptions.MidlineLeft;
        input.textViewport=viewport;input.textComponent=entry;input.placeholder=placeholder;input.characterLimit=12;input.contentType=TMP_InputField.ContentType.Alphanumeric;
        join=Button("Join",panel,new Vector2(210,30),new Vector2(100,48),"참가",async()=>
        {error="";try{await room.JoinByCodeAsync(input.text);}catch(Exception e){error=e.Message;}});
        code=Label("RoomCode",panel,new Vector2(-40,-35),new Vector2(440,50),"",22);
        copy=Button("Copy",panel,new Vector2(220,-35),new Vector2(80,40),"복사",()=>GUIUtility.systemCopyBuffer=room.InvitationCode);
        Label("Sides",panel,new Vector2(0,-85),new Vector2(540,40),"방장: 흑 · 참가자: 백 · 2인 대국",17);
        start=Button("Start",panel,new Vector2(0,-140),new Vector2(520,48),"두 사람 준비 완료 후 대국 시작",()=>{error="";room.StartMatch();});
        leave=Button("Leave",panel,new Vector2(-135,-210),new Vector2(250,44),"방 나가기 / 연결 취소",async()=>{error="";await room.Leave();});
        close=Button("Close",panel,new Vector2(135,-210),new Vector2(250,44),"닫기",()=>visible=false);
        hud=Rect("MatchRoomStatus",canvasObject.transform,Vector2.zero,new Vector2(430,52)).gameObject;
        var hudRect=(RectTransform)hud.transform;hudRect.anchorMin=hudRect.anchorMax=new Vector2(.5f,1);hudRect.pivot=new Vector2(.5f,1);hudRect.anchoredPosition=new Vector2(0,-8);
        hud.AddComponent<Image>().color=new Color(.98f,.94f,.84f,.94f);
        hudStatus=Label("Status",hud.transform,new Vector2(-50,0),new Vector2(310,45),"",15);
        Button("ReturnHome",hud.transform,new Vector2(163,0),new Vector2(90,38),"나가기",async()=>await room.Leave());
        modal.SetActive(false);hud.SetActive(false);
    }
    private static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
    {
        var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=position;rect.sizeDelta=size;return rect;
    }
    private TMP_Text Label(string name,Transform parent,Vector2 position,Vector2 size,string content,float fontSize)
    {
        var text=Rect(name,parent,position,size).gameObject.AddComponent<TextMeshProUGUI>();text.font=font;text.fontSize=fontSize;
        text.text=content;text.color=new Color(.17f,.12f,.08f);text.alignment=TextAlignmentOptions.Center;
        text.margin=Vector4.zero;text.textWrappingMode=TextWrappingModes.Normal;text.raycastTarget=false;return text;
    }
    private Button Button(string name,Transform parent,Vector2 position,Vector2 size,string content,UnityEngine.Events.UnityAction action)
    {
        var rect=Rect(name,parent,position,size);var image=rect.gameObject.AddComponent<Image>();image.color=new Color(.88f,.78f,.6f);
        var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(action);
        var colors=button.colors;colors.highlightedColor=new Color(1,.94f,.8f);colors.pressedColor=new Color(.7f,.6f,.45f);colors.disabledColor=new Color(.7f,.68f,.63f,.6f);button.colors=colors;
        Label("Label",rect,Vector2.zero,size-new Vector2(12,4),content,19);return button;
    }
}
