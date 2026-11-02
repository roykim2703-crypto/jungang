using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>Standalone victory/defeat scene UI.</summary>
public sealed class MatchResultScreen : MonoBehaviour
{
    private TMP_FontAsset font;
    private void Awake()
    {
        if(!MatchReviewSession.HasResult){SceneManager.LoadSceneAsync(AugmentMultiplayer.HomeScene);return;}
        var assets=Resources.Load<MultiplayerMenuAssets>("MultiplayerMenuAssets");font=assets!=null?assets.Font:TMP_Settings.defaultFontAsset;
        if(FindAnyObjectByType<EventSystem>()==null)new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
        Build();
    }
    private void Build()
    {
        var canvasObject=new GameObject("ResultCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
        var backdrop=Rect("Backdrop",canvasObject.transform,Vector2.zero,Vector2.zero);backdrop.anchorMin=Vector2.zero;backdrop.anchorMax=Vector2.one;backdrop.offsetMin=backdrop.offsetMax=Vector2.zero;
        backdrop.gameObject.AddComponent<Image>().color=MatchReviewSession.Heading=="패배"?new Color(.13f,.08f,.1f):new Color(.055f,.09f,.12f);
        var glow=Rect("Glow",backdrop,Vector2.zero,new Vector2(720,500));var glowImage=glow.gameObject.AddComponent<Image>();glowImage.color=MatchReviewSession.Heading=="패배"?new Color(.45f,.12f,.12f,.34f):new Color(.9f,.61f,.12f,.28f);
        var panel=Rect("ResultPanel",backdrop,Vector2.zero,new Vector2(620,440));var image=panel.gameObject.AddComponent<Image>();image.color=new Color(.97f,.93f,.82f,.98f);
        var outline=panel.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.95f,.65f,.16f);outline.effectDistance=new Vector2(5,-5);
        Label("Heading",panel,new Vector2(0,120),new Vector2(540,100),MatchReviewSession.Heading,66,new Color(.22f,.12f,.07f),FontStyles.Bold);
        Label("Detail",panel,new Vector2(0,50),new Vector2(520,50),MatchReviewSession.Detail,25,new Color(.38f,.25f,.14f),FontStyles.Normal);
        Label("Guide",panel,new Vector2(0,-5),new Vector2(520,50),"기보를 다시 보거나 메인 화면으로 이동할 수 있습니다",17,new Color(.42f,.36f,.28f),FontStyles.Normal);
        Button("Review",panel,new Vector2(-145,-110),new Vector2(250,64),"게임 복기",MatchReviewSession.RequestReview);
        Button("Home",panel,new Vector2(145,-110),new Vector2(250,64),"메인 화면",GoHome);
    }
    private async void GoHome()
    {
        MatchReviewSession.Clear();var room=AugmentMultiplayer.Instance;
        if(room.Active||room.IsConnecting)await room.Leave();else SceneManager.LoadSceneAsync(AugmentMultiplayer.HomeScene);
    }
    private static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
    {
        var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=position;rect.sizeDelta=size;return rect;
    }
    private TMP_Text Label(string name,Transform parent,Vector2 position,Vector2 size,string value,float fontSize,Color color,FontStyles style)
    {
        var text=Rect(name,parent,position,size).gameObject.AddComponent<TextMeshProUGUI>();text.font=font;text.fontSize=fontSize;text.text=value;text.color=color;text.fontStyle=style;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;return text;
    }
    private Button Button(string name,Transform parent,Vector2 position,Vector2 size,string value,UnityEngine.Events.UnityAction action)
    {
        var rect=Rect(name,parent,position,size);var image=rect.gameObject.AddComponent<Image>();image.color=new Color(.82f,.62f,.28f);
        var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(action);
        var colors=button.colors;colors.highlightedColor=new Color(1f,.82f,.42f);colors.pressedColor=new Color(.65f,.43f,.16f);button.colors=colors;
        Label("Label",rect,Vector2.zero,size-new Vector2(12,8),value,23,new Color(.13f,.08f,.04f),FontStyles.Bold);return button;
    }
}
