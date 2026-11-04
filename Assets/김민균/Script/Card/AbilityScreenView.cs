using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Public reveal effects use the authoritative result; they never roll gameplay randomness.</summary>
public sealed class AbilityScreenView : MonoBehaviour
{
    public bool IsPlaying { get; private set; }
    private Texture2D screenshot;
    private Material shatterMaterial;
    private readonly List<SpriteRenderer> restoredStones=new List<SpriteRenderer>();
    private readonly List<Sprite> finalSprites=new List<Sprite>();
    private readonly List<Vector3> finalScales=new List<Vector3>();
    public static bool IsAnimating(Transform parent)
    {
        if(parent==null)return false;
        foreach(var view in parent.GetComponentsInChildren<AbilityScreenView>())if(view.IsPlaying)return true;
        return false;
    }
    private static AbilityScreenView Create(Transform parent,string name)
    {
        var root=new GameObject(name,typeof(RectTransform));root.transform.SetParent(parent,false);
        var view=root.AddComponent<AbilityScreenView>();
        var canvas=root.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=2600;
        var group=root.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
        view.IsPlaying=true;return view;
    }
    public static void PlayConversion(Transform parent,가이드_돌_코드 guide,AugmentBoard.StoneConversion[] conversions,GameObject[] stones)
    {
        if(guide==null||conversions==null||conversions.Length==0)return;
        var view=Create(parent,"StoneColorShatter");
        var visible=new List<AugmentBoard.StoneConversion>();
        foreach(var conversion in conversions)
        {
            if(conversion.cell<0||conversion.cell>=stones.Length||stones[conversion.cell]==null)continue;
            var renderer=stones[conversion.cell].GetComponent<SpriteRenderer>();if(renderer==null)continue;
            view.restoredStones.Add(renderer);view.finalSprites.Add(renderer.sprite);view.finalScales.Add(renderer.transform.localScale);
            renderer.sprite=conversion.from==2?guide.흰돌이미지:guide.검은돌이미지;
            visible.Add(conversion);
        }
        view.StartCoroutine(view.ConvertStones(guide,visible));
    }
    public static void PlayCoin(Transform parent,int face,int removed,Action completed)
    {
        var view=Create(parent,"BoardFlipCoin");view.StartCoroutine(view.TossCoin(face==1,removed,completed));
    }
    public static void PlayTakeover(Transform parent,가이드_돌_코드 guide,int side)
    {
        if(guide==null)return;
        var view=Create(parent,"TakeoverGrab");view.StartCoroutine(view.GrabTurn(guide,side));
    }
    private static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position)
    {
        var rect=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;rect.SetParent(parent,false);
        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=size;rect.anchoredPosition=position;return rect;
    }
    private TMP_Text Label(string name,string content,Vector2 position,float size,Color color)
    {
        var rect=Rect(name,transform,new Vector2(1000,100),position);
        var text=rect.gameObject.AddComponent<TextMeshProUGUI>();text.text=content;text.fontSize=size;
        text.fontStyle=FontStyles.Bold;text.alignment=TextAlignmentOptions.Center;text.color=color;text.raycastTarget=false;
        text.enableAutoSizing=true;text.fontSizeMin=16;text.fontSizeMax=size;return text;
    }
    private Image Bar(string name,Transform parent,Vector2 size,Vector2 position,Color color)
    {
        var image=Rect(name,parent,size,position).gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return image;
    }
    private IEnumerator GrabTurn(가이드_돌_코드 guide,int side)
    {
        var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        var dim=Bar("TakeoverBackdrop",transform,Vector2.zero,Vector2.zero,new Color(.035f,.01f,.07f,.9f));
        dim.rectTransform.anchorMin=Vector2.zero;dim.rectTransform.anchorMax=Vector2.one;
        var violet=new Color(.8f,.32f,1);var gold=new Color(1,.8f,.3f);
        var speed=new RectTransform[24];
        for(int i=0;i<speed.Length;i++)speed[i]=Bar("GrabSpeedLine",transform,new Vector2(200,2),Vector2.zero,new Color(.65f,.3f,1,.4f)).rectTransform;
        var links=new RectTransform[16];
        for(int i=0;i<links.Length;i++)
        {
            links[i]=Rect("StolenTurnChain",transform,new Vector2(32,24),Vector2.zero);
            Bar("Top",links[i],new Vector2(32,4),new Vector2(0,12),gold);
            Bar("Bottom",links[i],new Vector2(32,4),new Vector2(0,-12),gold);
            Bar("Left",links[i],new Vector2(4,24),new Vector2(-16,0),gold);
            Bar("Right",links[i],new Vector2(4,24),new Vector2(16,0),gold);
        }
        float sign=side==1?1:-1;
        var stone=Rect("SnatchedStone",transform,new Vector2(120,120),new Vector2(sign*110,20));
        var seal=Rect("TakeoverReticle",stone,new Vector2(200,200),Vector2.zero).gameObject.AddComponent<TakeoverReticleGraphic>();
        seal.color=violet;seal.raycastTarget=false;
        var stoneImage=Bar("OpponentStone",stone,new Vector2(100,100),Vector2.zero,Color.white);
        stoneImage.sprite=side==1?guide.흰돌이미지:guide.검은돌이미지;stoneImage.preserveAspect=true;
        var hand=Bar("GrabbingHand",transform,new Vector2(360,360),Vector2.zero,Color.white);
        hand.sprite=AugmentEffectView.Artwork!=null?AugmentEffectView.Artwork.Fist:null;hand.preserveAspect=true;hand.enabled=hand.sprite!=null;
        var rays=new RectTransform[20];for(int i=0;i<rays.Length;i++)rays[i]=Bar("GrabImpactRay",transform,new Vector2(80,3),Vector2.zero,violet).rectTransform;
        var heading=Label("TakeoverHeading","내놔!",new Vector2(0,310),100,gold);
        var subtitle=Label("TakeoverSubtitle","착수권 강탈",new Vector2(0,-200),45,violet);
        Label("TakeoverInstruction",$"{(side==1?"흑":"백")}이 {(side==1?"백":"흑")}의 다음 착수 위치를 지정합니다",new Vector2(0,-285),27,new Color(.9f,.85f,1));
        var flash=Bar("GrabFlash",transform,Vector2.zero,Vector2.zero,Color.clear);flash.rectTransform.anchorMin=Vector2.zero;flash.rectTransform.anchorMax=Vector2.one;
        var audio=FindAnyObjectByType<GameAudio>();if(audio!=null)audio.PlayCardSelection();bool grabbed=false;
        var group=GetComponent<CanvasGroup>();
        for(float elapsed=0;elapsed<1.65f;elapsed+=Time.unscaledDeltaTime)
        {
            float enter=Mathf.Clamp01(elapsed/.28f),pull=Mathf.Clamp01((elapsed-.28f)/.55f);
            float ease=1-Mathf.Pow(1-pull,3),impact=Mathf.Clamp01((elapsed-.28f)/.5f);
            Vector2 start=new Vector2(sign*110,20),end=new Vector2(-sign*260,-10);
            stone.anchoredPosition=Vector2.Lerp(start,end,ease)+Vector2.up*Mathf.Sin(pull*Mathf.PI)*70;
            stone.localScale=Vector3.one*(1+.15f*Mathf.Sin(pull*Mathf.PI));
            seal.rectTransform.localRotation=Quaternion.Euler(0,0,elapsed*100);
            Vector2 handTarget=stone.anchoredPosition+new Vector2(sign*210,40);
            hand.rectTransform.anchoredPosition=elapsed<.28f?Vector2.Lerp(new Vector2(sign*950,110),handTarget,1-Mathf.Pow(1-enter,3)):handTarget;
            hand.rectTransform.localRotation=Quaternion.Euler(0,0,side==1?90:-90);
            heading.rectTransform.localScale=Vector3.one*(Mathf.Lerp(.6f,1,enter)+Mathf.Sin(enter*Mathf.PI)*.22f);
            heading.rectTransform.localRotation=Quaternion.Euler(0,0,-sign*4*(1-pull));
            subtitle.alpha=Mathf.Clamp01(elapsed/.18f);
            for(int i=0;i<links.Length;i++)
            {
                float t=(i+.5f)/links.Length;Vector2 from=new Vector2(-sign*970,-40);
                links[i].anchoredPosition=Vector2.Lerp(from,stone.anchoredPosition,t)+Vector2.up*Mathf.Sin(t*Mathf.PI)*Mathf.Sin(elapsed*35)*13;
                links[i].localRotation=Quaternion.Euler(0,0,45+Mathf.Sin(elapsed*12+i)*8);
                links[i].localScale=Vector3.one*Mathf.Clamp01((elapsed-.15f)*7);
            }
            for(int i=0;i<speed.Length;i++)
            {
                float phase=Mathf.Repeat(elapsed*2+i*.173f,1);
                speed[i].anchoredPosition=new Vector2(sign*Mathf.Lerp(1050,-1050,phase),(i-11.5f)*42);
                speed[i].sizeDelta=new Vector2(90+(i%4)*75,1+i%3);
            }
            for(int i=0;i<rays.Length;i++)
            {
                float angle=i*Mathf.PI*2/rays.Length;
                rays[i].anchoredPosition=stone.anchoredPosition+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*Mathf.Lerp(70,300,impact);
                rays[i].localRotation=Quaternion.Euler(0,0,angle*Mathf.Rad2Deg);
                rays[i].sizeDelta=new Vector2(Mathf.Lerp(110,20,impact),i%2==0?4:2);
                rays[i].GetComponent<Image>().color=new Color(violet.r,violet.g,violet.b,elapsed<.28f?0:1-impact);
            }
            flash.color=new Color(.85f,.5f,1,Mathf.Clamp01(1-Mathf.Abs(elapsed-.3f)/.08f)*.22f);
            if(!grabbed&&elapsed>=.28f){grabbed=true;if(audio!=null)audio.PlayCapturePing(6);}
            group.alpha=Mathf.Clamp01(elapsed/.1f)*(1-Mathf.Clamp01((elapsed-1.4f)/.25f));
            yield return null;
        }
        Finish();
    }
    private IEnumerator ConvertStones(가이드_돌_코드 guide,List<AugmentBoard.StoneConversion> conversions)
    {
        for(int i=0;i<conversions.Count;i++)
        {
            // Capture after the ordinary-color stone has actually been drawn, including overlay UI.
            yield return new WaitForEndOfFrame();
            if(restoredStones[i]==null)continue;
            screenshot=ScreenCapture.CaptureScreenshotAsTexture();
            var rect=Rect("FrozenGrayScreen",transform,Vector2.zero,Vector2.zero);
            rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
            var shard=rect.gameObject.AddComponent<ScreenShatterGraphic>();shard.raycastTarget=false;
            var shader=Resources.Load<Shader>("Effects/ScreenShatter");
            shatterMaterial=new Material(shader){hideFlags=HideFlags.HideAndDontSave};shard.material=shatterMaterial;
            Canvas.ForceUpdateCanvases();
            var screen=Camera.main.WorldToScreenPoint(restoredStones[i].transform.position);
            shard.Initialize(screenshot,new Vector2(screen.x/Screen.width,screen.y/Screen.height));
            var audio=FindAnyObjectByType<GameAudio>();
            for(float elapsed=0;elapsed<.95f;elapsed+=Time.unscaledDeltaTime)
            {
                shard.SetProgress(Mathf.Clamp01((elapsed-.2f)/.35f),Mathf.Clamp01((elapsed-.5f)/.45f));
                yield return null;
            }
            shard.SetProgress(1,1);rect.gameObject.SetActive(false);Destroy(rect.gameObject);
            Destroy(screenshot);screenshot=null;Destroy(shatterMaterial);shatterMaterial=null;
            if(audio!=null)audio.PlayCapturePing(5);
            for(float elapsed=0;elapsed<.42f;elapsed+=Time.unscaledDeltaTime)
            {
                float t=Mathf.Clamp01(elapsed/.42f);
                if(restoredStones[i]==null)break;
                restoredStones[i].sprite=t<.5f?(conversions[i].from==2?guide.흰돌이미지:guide.검은돌이미지):finalSprites[i];
                var scale=finalScales[i];scale.x*=Mathf.Max(.035f,Mathf.Abs(Mathf.Cos(t*Mathf.PI)));
                restoredStones[i].transform.localScale=scale;
                yield return null;
            }
            if(restoredStones[i]!=null)
            {
                restoredStones[i].sprite=finalSprites[i];restoredStones[i].transform.localScale=finalScales[i];
                AugmentEffectView.PlayPlacement(transform.parent,restoredStones[i].transform.position,guide.gap,conversions[i].to);
            }
        }
        Finish();
    }
    private IEnumerator TossCoin(bool success,int removed,Action completed)
    {
        var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        var dimRect=Rect("CoinBackdrop",transform,Vector2.zero,Vector2.zero);dimRect.anchorMin=Vector2.zero;dimRect.anchorMax=Vector2.one;
        var dim=dimRect.gameObject.AddComponent<Image>();dim.color=new Color(.025f,.035f,.065f,.88f);dim.raycastTarget=false;
        Label("CoinHeading","판 뒤집기",new Vector2(0,340),38,new Color(1,.8f,.3f));
        var coin=Rect("SpinningCoin",transform,new Vector2(300,300),new Vector2(0,-240));
        var art=coin.gameObject.AddComponent<CoinFaceGraphic>();art.raycastTarget=false;
        var faceRect=Rect("CoinFace",coin,new Vector2(230,200),Vector2.zero);
        var faceText=faceRect.gameObject.AddComponent<TextMeshProUGUI>();faceText.alignment=TextAlignmentOptions.Center;
        faceText.fontSize=64;faceText.fontStyle=FontStyles.Bold;faceText.color=new Color(.32f,.16f,.015f);faceText.raycastTarget=false;
        var result=Label("CoinResult","",new Vector2(0,-180),46,Color.white);
        var detail=Label("CoinDetail","",new Vector2(0,-255),25,new Color(.85f,.88f,.94f));
        var audio=FindAnyObjectByType<GameAudio>();if(audio!=null)audio.PlayStone();
        for(float elapsed=0;elapsed<1.3f;elapsed+=Time.unscaledDeltaTime)
        {
            float t=Mathf.Clamp01(elapsed/1.3f),spin=t*10*Mathf.PI;
            coin.anchoredPosition=new Vector2(0,Mathf.Lerp(-240,65,t)+Mathf.Sin(t*Mathf.PI)*360);
            coin.localScale=new Vector3(Mathf.Max(.045f,Mathf.Abs(Mathf.Cos(spin))),1,1);
            coin.localRotation=Quaternion.Euler(0,0,Mathf.Sin(t*Mathf.PI)*18);
            faceText.text=Mathf.Cos(spin)>=0?"앞":"뒤";yield return null;
        }
        coin.anchoredPosition=new Vector2(0,65);coin.localScale=Vector3.one;coin.localRotation=Quaternion.identity;
        faceText.text=success?"앞":"뒤";art.SetFace(success);
        result.text=success?"앞면 · 판 뒤집기 성공!":"뒷면 · 판 뒤집기 실패";
        result.color=success?new Color(1,.86f,.35f):new Color(1,.4f,.34f);
        detail.text=success?"모든 흑돌과 백돌의 색이 서로 바뀝니다":$"내 돌 {removed}개가 사라집니다";
        if(audio!=null)audio.PlayCapturePing(success?7:0);
        for(float elapsed=0;elapsed<1.25f;elapsed+=Time.unscaledDeltaTime)
        {coin.localScale=Vector3.one*(1+Mathf.Sin(elapsed*5)*.025f);yield return null;}
        var group=GetComponent<CanvasGroup>();
        for(float elapsed=0;elapsed<.2f;elapsed+=Time.unscaledDeltaTime)
        {group.alpha=1-Mathf.Clamp01(elapsed/.2f);yield return null;}
        Finish();completed?.Invoke();
    }
    private void Finish(){Cancel();Destroy(gameObject);}
    public void Cancel()
    {
        StopAllCoroutines();IsPlaying=false;
        for(int i=0;i<restoredStones.Count;i++)if(restoredStones[i]!=null)
        {restoredStones[i].sprite=finalSprites[i];restoredStones[i].transform.localScale=finalScales[i];}
        if(screenshot!=null){Destroy(screenshot);screenshot=null;}
        if(shatterMaterial!=null){Destroy(shatterMaterial);shatterMaterial=null;}
        var group=GetComponent<CanvasGroup>();if(group!=null)group.alpha=0;
    }
    private void OnDisable()=>Cancel();
}

[RequireComponent(typeof(CanvasRenderer))]
public sealed class TakeoverReticleGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();float radius=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.5f;
        for(int ring=0;ring<2;ring++)for(int i=0;i<64;i++)
        {
            float a=i*Mathf.PI*2/64,b=(i+1)*Mathf.PI*2/64,r=radius*(ring==0?1:.78f),width=ring==0?4:2;
            int start=mesh.currentVertCount;
            mesh.AddVert(new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r),color,Vector2.zero);
            mesh.AddVert(new Vector3(Mathf.Cos(b)*r,Mathf.Sin(b)*r),color,Vector2.zero);
            mesh.AddVert(new Vector3(Mathf.Cos(b)*(r-width),Mathf.Sin(b)*(r-width)),color,Vector2.zero);
            mesh.AddVert(new Vector3(Mathf.Cos(a)*(r-width),Mathf.Sin(a)*(r-width)),color,Vector2.zero);
            mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
        }
    }
}

[RequireComponent(typeof(CanvasRenderer))]
public sealed class ScreenShatterGraphic : MaskableGraphic
{
    private struct Shard { public Vector2 a,b,c; }
    private readonly List<Shard> shards=new List<Shard>();
    private Texture texture;
    private Vector2 origin;
    private float cracks,scatter;
    public override Texture mainTexture=>texture!=null?texture:Texture2D.whiteTexture;
    public void Initialize(Texture screenshot,Vector2 center)
    {
        texture=screenshot;origin=center;shards.Clear();
        var boundary=new List<Vector2>();
        var corners=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};
        for(int side=0;side<4;side++)for(int part=0;part<5;part++)boundary.Add(Vector2.Lerp(corners[side],corners[(side+1)%4],part/5f));
        for(int i=0;i<boundary.Count;i++)
        {
            Vector2 a=boundary[i],b=boundary[(i+1)%boundary.Count];
            Vector2 innerA=Vector2.Lerp(center,a,.2f+.05f*Mathf.Sin(i*2.7f));
            Vector2 innerB=Vector2.Lerp(center,b,.2f+.05f*Mathf.Sin((i+1)*2.7f));
            shards.Add(new Shard{a=center,b=innerA,c=innerB});
            shards.Add(new Shard{a=innerA,b=a,c=b});shards.Add(new Shard{a=innerA,b=b,c=innerB});
        }
        SetVerticesDirty();SetMaterialDirty();
    }
    public void SetProgress(float fracture,float displacement){cracks=fracture;scatter=displacement;SetVerticesDirty();}
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();var rect=rectTransform.rect;
        for(int i=0;i<shards.Count;i++)
        {
            var shard=shards[i];Vector2 originalA=Local(shard.a,rect),originalB=Local(shard.b,rect),originalC=Local(shard.c,rect);
            Vector2 centroid=(originalA+originalB+originalC)/3;
            float t=Mathf.Clamp01((scatter-(i%7)*.018f)/.88f),angle=(i%2==0?1:-1)*t*t*.8f;
            Vector2 offset=(centroid-Local(origin,rect)).normalized*t*t*(rect.size.magnitude*.8f)+(Vector2.down*t*t*rect.height*.2f);
            Vector2 a=Move(originalA,centroid,offset,angle),b=Move(originalB,centroid,offset,angle),c=Move(originalC,centroid,offset,angle);
            float alpha=1-t;int start=mesh.currentVertCount;
            Add(mesh,a,new Vector4(shard.a.x,shard.a.y),new Color(1,1,1,alpha));Add(mesh,b,new Vector4(shard.b.x,shard.b.y),new Color(1,1,1,alpha));Add(mesh,c,new Vector4(shard.c.x,shard.c.y),new Color(1,1,1,alpha));
            mesh.AddTriangle(start,start+1,start+2);
            if(cracks>i/(float)Mathf.Max(1,shards.Count))
            {Line(mesh,a,b,1.4f,alpha*.9f);Line(mesh,b,c,1.4f,alpha*.9f);Line(mesh,c,a,1.4f,alpha*.9f);}
        }
    }
    private static Vector2 Move(Vector2 p,Vector2 pivot,Vector2 offset,float angle)
    {
        var delta=p-pivot;float c=Mathf.Cos(angle),s=Mathf.Sin(angle);
        return pivot+offset+new Vector2(delta.x*c-delta.y*s,delta.x*s+delta.y*c);
    }
    private static Vector2 Local(Vector2 uv,Rect rect)=>rect.min+Vector2.Scale(uv,rect.size);
    private static void Add(VertexHelper mesh,Vector2 p,Vector4 uv,Color color)
    {var v=UIVertex.simpleVert;v.position=p;v.uv0=uv;v.color=color;mesh.AddVert(v);}
    private static void Line(VertexHelper mesh,Vector2 a,Vector2 b,float width,float alpha)
    {
        Vector2 d=b-a;if(d.sqrMagnitude<.001f)return;var normal=new Vector2(-d.y,d.x).normalized*width*.5f;
        int first=mesh.currentVertCount;var uv=new Vector4(0,0,1,0);var color=new Color(.9f,.95f,1,alpha);
        Add(mesh,a-normal,uv,color);Add(mesh,a+normal,uv,color);Add(mesh,b+normal,uv,color);Add(mesh,b-normal,uv,color);
        mesh.AddTriangle(first,first+1,first+2);mesh.AddTriangle(first,first+2,first+3);
    }
}

[RequireComponent(typeof(CanvasRenderer))]
public sealed class CoinFaceGraphic : MaskableGraphic
{
    private bool front=true;
    public void SetFace(bool value){front=value;SetVerticesDirty();}
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();float radius=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.5f;
        Circle(mesh,radius,new Color(.55f,.29f,.035f),new Color(.25f,.12f,.015f));
        Circle(mesh,radius*.94f,new Color(1,.84f,.34f),new Color(.85f,.56f,.12f));
        Circle(mesh,radius*.82f,front?new Color(1,.88f,.4f):new Color(.78f,.67f,.4f),new Color(.73f,.43f,.07f));
    }
    private static void Circle(VertexHelper mesh,float radius,Color center,Color edge)
    {
        int start=mesh.currentVertCount;mesh.AddVert(Vector3.zero,center,Vector2.zero);
        for(int i=0;i<=64;i++)
        {float angle=i*Mathf.PI*2/64;mesh.AddVert(new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius),edge,Vector2.zero);if(i>0)mesh.AddTriangle(start,start+i,start+i+1);}
    }
}
