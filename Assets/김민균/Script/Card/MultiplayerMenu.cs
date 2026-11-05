using System;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>Functional room UI opened by the existing Home start button.</summary>
public sealed class MultiplayerMenu : MonoBehaviour
{
    private AugmentMultiplayer room;
    private GameObject modal,hud;
    private TMP_Text status,code,hudStatus,timeLimit;
    private TMP_InputField input;
    private Button host,join,local,start,leave,copy,close,timeDown,timeUp;
    private TMP_FontAsset font;
    private bool visible;
    private bool mobileConfirmation;
    private Button confirmPlacement;
    private string error="";
    private TMP_Text copyLabel;
    private string copyMessage="",copyCode="";
    private float copyMessageUntil;
    private bool copySucceeded;
#if UNITY_WEBGL && !UNITY_EDITOR
    private readonly Vector3[] copyCorners=new Vector3[4];
    private bool webCopyVisible;
    [DllImport("__Internal")] private static extern void Jungang_ClipboardUpdate(string text,float x,float y,float width,float height,float screenWidth,float screenHeight,int visible,int focused);
    [DllImport("__Internal")] private static extern void Jungang_ClipboardCopyFallback();
    [DllImport("__Internal")] private static extern int Jungang_ClipboardReadResult();
    [DllImport("__Internal")] private static extern void Jungang_ClipboardHide();
#endif
    private static readonly int[] TimeOptions={180,300,600,900,1200};
    public static void Open()
    {
        var root=AugmentMultiplayer.Instance;
        var menu=root.GetComponent<MultiplayerMenu>();if(menu==null)menu=root.gameObject.AddComponent<MultiplayerMenu>();
        menu.visible=true;menu.error="";
    }
    private void Awake()
    {
        room=GetComponent<AugmentMultiplayer>();mobileConfirmation=MobileGameInput.RequiresPlacementConfirmation;
        var assets=Resources.Load<MultiplayerMenuAssets>("MultiplayerMenuAssets");font=assets!=null?assets.Font:TMP_Settings.defaultFontAsset;
        Build();
    }
    private void Update()
    {
        bool home=SceneManager.GetActiveScene().path==AugmentMultiplayer.HomeScene;
        modal.SetActive(home&&visible);hud.SetActive(!home&&SceneManager.GetActiveScene().path==AugmentMultiplayer.GameScene&&(AugmentMatch.Instance==null||!AugmentMatch.Instance.IsReviewMode));
        bool offline=!room.Active&&!room.IsConnecting;
        host.interactable=local.interactable=offline;
        input.interactable=offline;join.interactable=offline&&input.text.Trim().Length>=4;
        start.interactable=room.CanStart;leave.interactable=room.Active||room.IsConnecting;
        copy.interactable=room.Active&&!string.IsNullOrEmpty(room.InvitationCode);
        if(copyCode!=room.InvitationCode){copyCode=room.InvitationCode;copyMessageUntil=0;}
#if UNITY_WEBGL && !UNITY_EDITOR
        int copyResult=Jungang_ClipboardReadResult();
        if(copyResult!=0)ShowCopyResult(copyResult>0);
#endif
        bool copyNotice=Time.unscaledTime<copyMessageUntil;
        copyLabel.text=copyNotice&&copySucceeded?"복사됨":"복사";
        close.interactable=!room.IsConnecting;
        bool canEditTime=offline||room.Active&&room.IsHost&&room.Stage==AugmentMultiplayer.RoomStage.Lobby;
        timeDown.interactable=timeUp.interactable=canEditTime;
        timeLimit.text=$"인당 제한시간  {room.TimeLimitSeconds/60}분";
        status.text=copyNotice?copyMessage:string.IsNullOrEmpty(error)?room.ConnectionStatus:error;
        code.text=string.IsNullOrEmpty(room.InvitationCode)?"초대코드 · 방을 만들면 표시됩니다":"초대코드  "+room.InvitationCode;
        string side=room.Active?(room.IsHost?"흑":"백"):"로컬";
        hudStatus.text=side+" · "+(room.Stage==AugmentMultiplayer.RoomStage.Playing&&AugmentMatch.Instance!=null?AugmentMatch.Instance.Status:room.Active?room.ConnectionStatus:"대국 진행 중");
        if(mobileConfirmation)
        {
            var match=AugmentMatch.Instance;
            confirmPlacement.interactable=match!=null&&match.CanConfirmPlacement&&!AudioSettings.IsOpen;
            if(match!=null&&match.HasPendingPlacement)
            {
                var cell=match.PendingPlacementCell;
                hudStatus.text=match.CanConfirmPlacement?$"({cell.x+1}, {cell.y+1}) 선택":"다른 위치 선택";
            }
            else hudStatus.text=match!=null&&match.Cards.IsChoosing?"증강 선택 중":"위치 선택";
        }
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
        var panel=Rect("Panel",modal.transform,Vector2.zero,new Vector2(620,650));
        panel.gameObject.AddComponent<Image>().color=new Color(.98f,.94f,.84f); RoundedUI.Apply(panel.GetComponent<Image>(),24f);
        var outline=panel.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.23f,.17f,.1f);outline.effectDistance=new Vector2(3,-3);
        Label("Title",panel,new Vector2(0,275),new Vector2(540,45),"대국 시작",30);
        status=Label("Status",panel,new Vector2(0,220),new Vector2(540,55),"",19);
        timeLimit=Label("TimeLimit",panel,new Vector2(0,162),new Vector2(250,42),"",20);
        timeDown=Button("TimeDown",panel,new Vector2(-160,162),new Vector2(64,42),"◀",()=>ChangeTime(-1));
        timeUp=Button("TimeUp",panel,new Vector2(160,162),new Vector2(64,42),"▶",()=>ChangeTime(1));
        host=Button("Host",panel,new Vector2(-135,105),new Vector2(250,48),"초대 방 만들기",async()=>
        {error="";try{await room.Connect(true);}catch(Exception e){error=e.Message;}});
        local=Button("Local",panel,new Vector2(135,105),new Vector2(250,48),"같은 기기에서 대국",()=>{visible=false;room.StartLocal();});
        var field=Rect("InviteCodeInput",panel,new Vector2(-60,42),new Vector2(400,48));
        field.gameObject.AddComponent<Image>().color=Color.white; RoundedUI.Apply(field.GetComponent<Image>(),12f);input=field.gameObject.AddComponent<TMP_InputField>();
        var viewport=Rect("Viewport",field,Vector2.zero,new Vector2(376,44));viewport.gameObject.AddComponent<RectMask2D>();
        var entry=Label("Text",viewport,Vector2.zero,new Vector2(370,44),"",23);entry.alignment=TextAlignmentOptions.MidlineLeft;
        var placeholder=Label("Placeholder",viewport,Vector2.zero,new Vector2(370,44),"친구의 초대코드 입력",20);placeholder.color=new Color(.55f,.5f,.43f);placeholder.alignment=TextAlignmentOptions.MidlineLeft;
        input.textViewport=viewport;input.textComponent=entry;input.placeholder=placeholder;input.characterLimit=12;input.contentType=TMP_InputField.ContentType.Alphanumeric;
        join=Button("Join",panel,new Vector2(210,42),new Vector2(100,48),"참가",async()=>
        {error="";try{await room.JoinByCodeAsync(input.text);}catch(Exception e){error=e.Message;}});
        code=Label("RoomCode",panel,new Vector2(-40,-22),new Vector2(440,50),"",22);
        copy=Button("Copy",panel,new Vector2(220,-22),new Vector2(80,40),"복사",CopyInvitationCode);
        copyLabel=copy.GetComponentInChildren<TMP_Text>();
        Label("Sides",panel,new Vector2(0,-72),new Vector2(540,40),"방장: 흑 · 참가자: 백 · 2인 대국",17);
        start=Button("Start",panel,new Vector2(0,-128),new Vector2(520,48),"두 사람 준비 완료 후 대국 시작",()=>{error="";room.StartMatch();});
        leave=Button("Leave",panel,new Vector2(-135,-245),new Vector2(250,44),"방 나가기 / 연결 취소",async()=>{error="";await room.Leave();});
        close=Button("Close",panel,new Vector2(135,-245),new Vector2(250,44),"닫기",()=>visible=false);
        hud=Rect("MatchRoomStatus",canvasObject.transform,Vector2.zero,new Vector2(430,52)).gameObject;
        var hudRect=(RectTransform)hud.transform;hudRect.anchorMin=hudRect.anchorMax=new Vector2(.5f,0);hudRect.pivot=new Vector2(.5f,0);hudRect.anchoredPosition=new Vector2(0,8);
        hud.AddComponent<Image>().color=new Color(.98f,.94f,.84f,.94f); RoundedUI.Apply(hud.GetComponent<Image>(),18f);
        hudStatus=Label("Status",hud.transform,new Vector2(-50,0),new Vector2(310,45),"",15);
        var returnHome=Button("ReturnHome",hud.transform,new Vector2(163,0),new Vector2(90,38),"나가기",async()=>await room.Leave());
        var audio=Button("AudioSettings",hud.transform,new Vector2(0,-52),new Vector2(110,38),"소리 설정",AudioSettings.Open);
        if(mobileConfirmation)
        {
            // Reuse the room HUD and its existing buttons in the side panel's header.
            hudRect.anchorMin=hudRect.anchorMax=new Vector2(.8f,.75f);hudRect.pivot=Vector2.one*.5f;
            hudRect.anchoredPosition=Vector2.zero;hudRect.sizeDelta=new Vector2(430,76);
            hudStatus.rectTransform.anchoredPosition=new Vector2(-135,0);
            hudStatus.rectTransform.sizeDelta=new Vector2(135,60);hudStatus.fontSize=20;
            confirmPlacement=returnHome;confirmPlacement.name="ConfirmPlacement";
            var confirmRect=(RectTransform)confirmPlacement.transform;
            confirmRect.anchoredPosition=new Vector2(30,0);confirmRect.sizeDelta=new Vector2(180,60);
            confirmPlacement.onClick.RemoveAllListeners();
            confirmPlacement.onClick.AddListener(()=>{var match=AugmentMatch.Instance;if(match!=null)match.ConfirmPlacement();});
            var confirmText=confirmPlacement.GetComponentInChildren<TMP_Text>();
            confirmText.text="착수 확인";confirmText.font=font;confirmText.fontSize=24;
            confirmText.rectTransform.sizeDelta=new Vector2(168,56);
            var leaveRect=(RectTransform)audio.transform;leaveRect.anchoredPosition=new Vector2(170,0);leaveRect.sizeDelta=new Vector2(72,60);
            audio.name="ReturnHome";audio.onClick.RemoveAllListeners();audio.onClick.AddListener(async()=>await room.Leave());
            var leaveText=audio.GetComponentInChildren<TMP_Text>();leaveText.text="나가기";leaveText.fontSize=18;leaveText.rectTransform.sizeDelta=new Vector2(66,56);
        }
        TitleFadeIn.ConfigurePopup(modal, panel);
        modal.SetActive(false);hud.SetActive(false);
    }
    private void ChangeTime(int direction)
    {
        int current=0,best=int.MaxValue;
        for(int i=0;i<TimeOptions.Length;i++){int delta=Mathf.Abs(TimeOptions[i]-room.TimeLimitSeconds);if(delta<best){best=delta;current=i;}}
        current=(current+direction+TimeOptions.Length)%TimeOptions.Length;room.SetTimeLimitSeconds(TimeOptions[current]);
    }
    private void CopyInvitationCode()
    {
        if(!copy.interactable||string.IsNullOrEmpty(room.InvitationCode))return;
#if UNITY_WEBGL && !UNITY_EDITOR
        Jungang_ClipboardCopyFallback();
#else
        GUIUtility.systemCopyBuffer=room.InvitationCode;
        ShowCopyResult(GUIUtility.systemCopyBuffer==room.InvitationCode);
#endif
    }
    private void ShowCopyResult(bool success)
    {
        copySucceeded=success;
        copyMessage=success?"초대코드가 복사되었습니다.":"자동 복사가 차단되었습니다. 표시된 초대코드를 직접 복사해 주세요.";
        copyMessageUntil=Time.unscaledTime+3f;
    }
#if UNITY_WEBGL && !UNITY_EDITOR
    private void LateUpdate()
    {
        bool show=modal!=null&&modal.activeInHierarchy&&copy.interactable&&!AudioSettings.IsOpen;
        if(!show){if(webCopyVisible)Jungang_ClipboardHide();webCopyVisible=false;return;}
        var rect=(RectTransform)copy.transform;rect.GetWorldCorners(copyCorners);
        var canvas=copy.GetComponentInParent<Canvas>();
        Camera camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
        Vector2 bottom=RectTransformUtility.WorldToScreenPoint(camera,copyCorners[0]);
        Vector2 top=RectTransformUtility.WorldToScreenPoint(camera,copyCorners[2]);
        bool focused=EventSystem.current!=null&&EventSystem.current.currentSelectedGameObject==copy.gameObject;
        Jungang_ClipboardUpdate(room.InvitationCode,bottom.x,bottom.y,top.x-bottom.x,top.y-bottom.y,Screen.width,Screen.height,1,focused?1:0);
        webCopyVisible=true;
    }
    private void OnDisable(){if(webCopyVisible)Jungang_ClipboardHide();webCopyVisible=false;}
#endif
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
        RoundedUI.Apply(image); var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(action);
        var colors=button.colors;colors.highlightedColor=new Color(1,.94f,.8f);colors.pressedColor=new Color(.7f,.6f,.45f);colors.disabledColor=new Color(.7f,.68f,.63f,.6f);button.colors=colors;
        Label("Label",rect,Vector2.zero,size-new Vector2(12,4),content,19);return button;
    }
}
