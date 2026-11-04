using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Screen-space side view of the timing stone. Shares its continuous aim with the board.</summary>
public sealed class StackStoneView : MonoBehaviour
{
    private CanvasGroup group;
    private RectTransform panel,upper,lower,shadow,flash;
    private StackStoneProfile upperProfile,lowerProfile;
    private TMP_Text instruction;
    private readonly RectTransform[] streaks=new RectTransform[12],rays=new RectTransform[16];
    private bool landing;
    public bool IsLanding=>landing;
    private static readonly Color Gold=new Color(1,.78f,.3f);

    private RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position)
    {
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);
        var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
        rect.sizeDelta=size;rect.anchoredPosition=position;return rect;
    }
    private RectTransform Bar(string name,Vector2 size,Vector2 position,Color color)
    {
        var rect=Rect(name,panel,size,position);var image=rect.gameObject.AddComponent<Image>();
        image.color=color;image.raycastTarget=false;return rect;
    }
    private TMP_Text Label(string name,string text,Vector2 position,float size,Color color)
    {
        var rect=Rect(name,panel,new Vector2(600,40),position);
        var label=rect.gameObject.AddComponent<TextMeshProUGUI>();label.text=text;label.fontSize=size;
        label.alignment=TextAlignmentOptions.Center;label.color=color;label.raycastTarget=false;return label;
    }
    private void Build()
    {
        if(group!=null)return;
        var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder=2200;
        var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        group=gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
        panel=Rect("SideViewPanel",transform,new Vector2(680,320),Vector2.zero);
        panel.anchorMin=panel.anchorMax=new Vector2(.5f,.24f);
        var backing=panel.gameObject.AddComponent<Image>();backing.color=new Color(.025f,.04f,.08f,.94f);backing.raycastTarget=false;
        RoundedUI.Apply(backing,22);
        Bar("TopAccent",new Vector2(580,2),new Vector2(0,151),Gold);
        Bar("Floor",new Vector2(570,2),new Vector2(0,-91),new Color(.5f,.65f,.8f,.5f));
        Bar("CenterGuide",new Vector2(2,166),new Vector2(0,-3),new Color(1,.8f,.35f,.18f));
        Label("Title","돌 겹치기",new Vector2(0,120),25,Gold).fontStyle=FontStyles.Bold;
        instruction=Label("Instruction","중앙에 맞춰 클릭해 떨어뜨리세요",new Vector2(0,-128),18,new Color(.8f,.87f,1));
        for(int i=0;i<streaks.Length;i++)streaks[i]=Bar("SpeedLine",new Vector2(40,2),Vector2.zero,new Color(.6f,.85f,1,.6f));
        shadow=Bar("GroundShadow",new Vector2(185,7),new Vector2(0,-86),new Color(0,0,0,.6f));
        lower=Rect("BaseStone",panel,new Vector2(166,53),new Vector2(0,-59));
        lowerProfile=lower.gameObject.AddComponent<StackStoneProfile>();lowerProfile.raycastTarget=false;
        upper=Rect("MovingStone",panel,new Vector2(166,53),new Vector2(0,48));
        upperProfile=upper.gameObject.AddComponent<StackStoneProfile>();upperProfile.raycastTarget=false;
        for(int i=0;i<rays.Length;i++)
        {
            rays[i]=Bar("ImpactRay",new Vector2(44,3),Vector2.zero,Gold);rays[i].gameObject.SetActive(false);
        }
        flash=Bar("ImpactFlash",new Vector2(220,4),new Vector2(0,-37),new Color(1,.9f,.6f,0));
        group.alpha=0;
    }
    public void HideAim(){if(group!=null&&!landing)group.alpha=0;}
    public void Cancel()
    {
        StopAllCoroutines();landing=false;if(group==null)return;
        group.alpha=0;panel.anchoredPosition=Vector2.zero;
        foreach(var ray in rays)ray.gameObject.SetActive(false);
        foreach(var streak in streaks){streak.gameObject.SetActive(true);streak.localRotation=Quaternion.identity;}
        flash.GetComponent<Image>().color=Color.clear;
    }
    public void ShowAim(int side,int baseColor,float offset,float speed,bool valid)
    {
        Build();if(landing)return;
        group.alpha=1;panel.anchoredPosition=Vector2.zero;
        upperProfile.SetStone(side);lowerProfile.SetStone(baseColor==0||baseColor==5?side:baseColor);
        lower.anchoredPosition=new Vector2(0,-59);
        lower.gameObject.SetActive(baseColor!=0&&baseColor!=5);
        upper.anchoredPosition=new Vector2(offset*90,48);
        upper.localScale=Vector3.one;lower.localScale=Vector3.one;
        upper.localRotation=Quaternion.Euler(0,0,-offset*3);
        shadow.anchoredPosition=new Vector2(offset*65,-86);
        instruction.text=valid?"중앙에 맞춰 클릭해 떨어뜨리세요":"놓을 수 없는 칸 · 다른 위치를 조준하세요";
        instruction.color=valid?new Color(.8f,.87f,1):new Color(1,.4f,.3f);
        for(int i=0;i<streaks.Length;i++)
        {
            float phase=Time.unscaledTime*2.5f+i*.17f;
            float x=Mathf.Repeat(phase,1)*590-295;
            streaks[i].anchoredPosition=new Vector2(x,18+(i%5)*14);
            streaks[i].sizeDelta=new Vector2(Mathf.Lerp(12,80,speed)*(1+(i%3)*.25f),i%3==0?2:1);
            var c=new Color(.55f,.8f,1,Mathf.Lerp(.04f,.5f,speed));streaks[i].GetComponent<Image>().color=c;
        }
    }
    public void PlayLanding(int side,int baseColor,float offset)
    {
        Cancel();Build();ShowAim(side,baseColor,offset,1,true);landing=true;
        StartCoroutine(Land(offset,baseColor!=0&&baseColor!=5));
    }
    private IEnumerator Land(float offset,bool stacked)
    {
        instruction.text="낙하!";float x=offset*90;
        // The result view centers the receiving stone underneath the confirmed landing column.
        lower.anchoredPosition=new Vector2(x,-59);
        float targetY=stacked?-25:-64;
        for(float elapsed=0;elapsed<.32f;elapsed+=Time.unscaledDeltaTime)
        {
            float t=Mathf.Clamp01(elapsed/.32f);
            upper.anchoredPosition=new Vector2(x,Mathf.Lerp(48,targetY,t*t*t));
            upper.localRotation=Quaternion.Euler(0,0,Mathf.Lerp(-offset*3,0,t));
            for(int i=0;i<streaks.Length;i++)
            {
                streaks[i].anchoredPosition=new Vector2(x+(i%2==0?-110:110),Mathf.Lerp(95,targetY+30,t)+(i/2)*9);
                streaks[i].localRotation=Quaternion.Euler(0,0,90);
                streaks[i].sizeDelta=new Vector2(18+t*55,2);
            }
            yield return null;
        }
        upper.anchoredPosition=new Vector2(x,targetY);
        instruction.text=stacked?"겹치기 완료!":"착지 완료!";instruction.color=Gold;
        foreach(var streak in streaks)streak.gameObject.SetActive(false);
        for(float elapsed=0;elapsed<.75f;elapsed+=Time.unscaledDeltaTime)
        {
            float impact=Mathf.Clamp01(elapsed/.34f),bounce=Mathf.Sin(impact*Mathf.PI);
            panel.anchoredPosition=new Vector2(Mathf.Sin(elapsed*110)*5*(1-impact),Mathf.Cos(elapsed*90)*3*(1-impact));
            upper.localScale=new Vector3(1+.17f*bounce,1-.18f*bounce,1);
            upper.anchoredPosition=new Vector2(x,targetY+Mathf.Sin(impact*Mathf.PI)*8);
            for(int i=0;i<rays.Length;i++)
            {
                var ray=rays[i];ray.gameObject.SetActive(true);float angle=i*Mathf.PI*2/rays.Length;
                ray.anchoredPosition=new Vector2(x,stacked?-40:-86)+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle)*.5f)*Mathf.Lerp(65,230,impact);
                ray.localRotation=Quaternion.Euler(0,0,angle*Mathf.Rad2Deg);
                ray.sizeDelta=new Vector2(Mathf.Lerp(65,12,impact),i%2==0?3:1.5f);
                ray.GetComponent<Image>().color=new Color(1,.82f,.4f,1-impact);
            }
            flash.anchoredPosition=new Vector2(x,stacked?-40:-86);flash.sizeDelta=new Vector2(Mathf.Lerp(120,450,impact),4*(1-impact));
            flash.GetComponent<Image>().color=new Color(1,.9f,.65f,1-impact);
            group.alpha=1-Mathf.Clamp01((elapsed-.5f)/.25f);
            yield return null;
        }
        foreach(var ray in rays)ray.gameObject.SetActive(false);
        foreach(var streak in streaks){streak.gameObject.SetActive(true);streak.localRotation=Quaternion.identity;}
        landing=false;group.alpha=0;
    }
}

/// <summary>Layered convex lens mesh: a shaded stone profile with a beveled rim and glossy top.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class StackStoneProfile : MaskableGraphic
{
    private int side=1;
    public void SetStone(int value){if(side==value)return;side=value;SetVerticesDirty();}
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();var rect=rectTransform.rect;bool white=side==2||side==4;
        Color rim=white?new Color(.45f,.51f,.61f):new Color(.035f,.055f,.095f);
        Color face=white?new Color(.96f,.97f,1):new Color(.19f,.24f,.33f);
        Color shine=white?Color.white:new Color(.5f,.61f,.75f);
        Ellipse(mesh,rect.center,new Vector2(rect.width*.5f,rect.height*.5f),rim,rim);
        Ellipse(mesh,rect.center+Vector2.up*rect.height*.1f,new Vector2(rect.width*.48f,rect.height*.38f),face,rim);
        Ellipse(mesh,rect.center+new Vector2(-rect.width*.08f,rect.height*.22f),new Vector2(rect.width*.34f,rect.height*.13f),shine,face);
    }
    private static void Ellipse(VertexHelper mesh,Vector2 center,Vector2 radius,Color middle,Color edge)
    {
        int start=mesh.currentVertCount;mesh.AddVert(center,middle,Vector2.zero);
        const int count=64;
        for(int i=0;i<=count;i++)
        {
            float angle=i*Mathf.PI*2/count;
            mesh.AddVert(center+new Vector2(Mathf.Cos(angle)*radius.x,Mathf.Sin(angle)*radius.y),edge,Vector2.zero);
            if(i>0)mesh.AddTriangle(start,start+i,start+i+1);
        }
    }
}
