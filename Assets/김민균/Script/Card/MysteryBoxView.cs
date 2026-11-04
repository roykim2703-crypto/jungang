using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Separate body/lid sprites: entrance, anticipation, opening, then the actual rolled augment.</summary>
public sealed class MysteryBoxView : MonoBehaviour
{
    public bool IsPlaying { get; private set; }
    private CanvasGroup group,prizeGroup;
    private RectTransform pose,lid,prize;
    private Image lidImage,flash;
    private TMP_Text title,result;
    private readonly RectTransform[] rays=new RectTransform[16];
    private static readonly Color Gold=new Color(1,.79f,.28f);
    private Action completed;

    private RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position)
    {
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);
        var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
        rect.sizeDelta=size;rect.anchoredPosition=position;return rect;
    }
    private Image Picture(string name,Transform parent,Sprite sprite,Vector2 size,Vector2 position,Color color)
    {
        var rect=Rect(name,parent,size,position);var image=rect.gameObject.AddComponent<Image>();
        image.sprite=sprite;image.color=color;image.raycastTarget=false;return image;
    }
    private TMP_Text Label(string name,Transform parent,Vector2 position,float size,Color color)
    {
        var rect=Rect(name,parent,new Vector2(1000,100),position);
        var label=rect.gameObject.AddComponent<TextMeshProUGUI>();label.fontSize=size;label.fontStyle=FontStyles.Bold;
        label.alignment=TextAlignmentOptions.Center;label.color=color;label.raycastTarget=false;
        label.enableAutoSizing=true;label.fontSizeMin=22;label.fontSizeMax=size;return label;
    }
    private void Build(Sprite artwork)
    {
        var canvas=GetComponent<Canvas>();if(canvas==null)canvas=gameObject.AddComponent<Canvas>();
        canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=2400;
        var scaler=GetComponent<CanvasScaler>();if(scaler==null)scaler=gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        group=GetComponent<CanvasGroup>();if(group==null)group=gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts=false;group.interactable=false;
        var dim=Picture("Backdrop",transform,null,Vector2.zero,Vector2.zero,new Color(.015f,.02f,.045f,.88f));
        dim.rectTransform.anchorMin=Vector2.zero;dim.rectTransform.anchorMax=Vector2.one;dim.rectTransform.sizeDelta=Vector2.zero;
        for(int i=0;i<rays.Length;i++)
        {
            var ray=Picture("RevealRay",transform,null,new Vector2(140,3),Vector2.zero,Gold);
            rays[i]=ray.rectTransform;
        }
        pose=Rect("BoxPose",transform,new Vector2(620,620),Vector2.zero);
        Picture("BoxBody",pose,Resources.Load<Sprite>("Effects/MysteryBoxBody"),new Vector2(620,620),Vector2.zero,Color.white);
        lidImage=Picture("BoxLid",pose,Resources.Load<Sprite>("Effects/MysteryBoxLid"),new Vector2(620,620),new Vector2(0,65),Color.white);
        lid=lidImage.rectTransform;
        prize=Rect("AugmentPrize",transform,new Vector2(400,400),Vector2.zero);
        prizeGroup=prize.gameObject.AddComponent<CanvasGroup>();
        var border=Picture("PrizeBorder",prize,null,new Vector2(204,204),Vector2.zero,Gold);RoundedUI.Apply(border,22);
        var mat=Picture("PrizeBacking",prize,null,new Vector2(194,194),Vector2.zero,new Color(.04f,.05f,.09f));RoundedUI.Apply(mat,18);
        var icon=Picture("AugmentArtwork",prize,artwork,new Vector2(180,180),Vector2.zero,Color.white);icon.preserveAspect=true;
        icon.enabled=artwork!=null;
        if(artwork==null)Label("PrivatePrize",prize,Vector2.zero,70,Gold).text="?";
        title=Label("MysteryBoxTitle",transform,new Vector2(0,350),32,Gold);
        result=Label("AugmentName",transform,new Vector2(0,30),44,Color.white);
        flash=Picture("OpeningFlash",transform,null,Vector2.zero,Vector2.zero,Color.clear);
        flash.rectTransform.anchorMin=Vector2.zero;flash.rectTransform.anchorMax=Vector2.one;flash.rectTransform.sizeDelta=Vector2.zero;
    }
    public void Play(string augmentName,Sprite artwork,Action onComplete)
    {
        Cancel();
        // Rebuild only when a new activation starts so card artwork cannot carry over.
        foreach(Transform child in transform){child.gameObject.SetActive(false);Destroy(child.gameObject);}
        Build(artwork);completed=onComplete;IsPlaying=true;
        StartCoroutine(Animate(augmentName));
    }
    public void Cancel()
    {
        StopAllCoroutines();completed=null;IsPlaying=false;if(group!=null)group.alpha=0;
    }
    private void OnDisable()=>Cancel();
    private IEnumerator Animate(string augmentName)
    {
        var audio=FindAnyObjectByType<GameAudio>();
        title.text="개꿀잼 상자";result.text=augmentName;result.alpha=0;prizeGroup.alpha=0;
        foreach(var ray in rays)ray.gameObject.SetActive(false);
        if(audio!=null)audio.PlayCapturePing(0);
        for(float elapsed=0;elapsed<.42f;elapsed+=Time.unscaledDeltaTime)
        {
            float t=Mathf.Clamp01(elapsed/.42f),ease=1-Mathf.Pow(1-t,3);
            group.alpha=Mathf.Clamp01(t*3);
            pose.anchoredPosition=new Vector2(0,Mathf.Lerp(-650,0,ease));
            pose.localScale=Vector3.one*(Mathf.Lerp(.25f,1,ease)+Mathf.Sin(t*Mathf.PI)*.14f);
            pose.localRotation=Quaternion.Euler(0,0,Mathf.Lerp(-16,0,ease));
            yield return null;
        }
        group.alpha=1;pose.anchoredPosition=Vector2.zero;pose.localRotation=Quaternion.identity;pose.localScale=Vector3.one;
        int shakeBeat=-1;
        for(float elapsed=0;elapsed<.85f;elapsed+=Time.unscaledDeltaTime)
        {
            float t=elapsed/.85f,envelope=Mathf.Sin(t*Mathf.PI);
            pose.anchoredPosition=new Vector2(Mathf.Sin(elapsed*65)*10*envelope,Mathf.Abs(Mathf.Sin(elapsed*34))*12*envelope);
            pose.localRotation=Quaternion.Euler(0,0,Mathf.Sin(elapsed*48)*7*envelope);
            lid.anchoredPosition=new Vector2(0,65+Mathf.Abs(Mathf.Sin(elapsed*60))*8*t);
            int beat=Mathf.FloorToInt(elapsed/.22f);if(beat!=shakeBeat){shakeBeat=beat;if(audio!=null)audio.PlayStone();}
            yield return null;
        }
        pose.localRotation=Quaternion.identity;
        title.text="상자에서 나온 증강";
        if(audio!=null)audio.PlayCapturePing(7);
        foreach(var ray in rays)ray.gameObject.SetActive(true);
        for(float elapsed=0;elapsed<.55f;elapsed+=Time.unscaledDeltaTime)
        {
            float t=Mathf.Clamp01(elapsed/.55f),ease=1-Mathf.Pow(1-t,3);
            pose.anchoredPosition=Vector2.down*140*ease;
            lid.anchoredPosition=new Vector2(240*ease,65+300*ease);
            lid.localRotation=Quaternion.Euler(0,0,-38*ease);lidImage.color=new Color(1,1,1,1-.8f*ease);
            prize.anchoredPosition=new Vector2(0,Mathf.Lerp(-40,190,ease));
            prize.localScale=Vector3.one*Mathf.Lerp(.2f,1,ease);
            prizeGroup.alpha=ease;result.alpha=ease;
            flash.color=new Color(1,.9f,.65f,Mathf.Sin(t*Mathf.PI)*.22f);
            SetRays(ease,elapsed);
            yield return null;
        }
        prize.anchoredPosition=new Vector2(0,190);prize.localScale=Vector3.one;
        pose.anchoredPosition=Vector2.down*140;lid.anchoredPosition=new Vector2(240,365);
        lid.localRotation=Quaternion.Euler(0,0,-38);lidImage.color=new Color(1,1,1,.2f);
        prizeGroup.alpha=1;result.alpha=1;flash.color=Color.clear;
        for(float elapsed=0;elapsed<1.35f;elapsed+=Time.unscaledDeltaTime)
        {
            prize.localScale=Vector3.one*(1+Mathf.Sin(elapsed*4)*.025f);
            SetRays(1,elapsed+.55f);yield return null;
        }
        for(float elapsed=0;elapsed<.25f;elapsed+=Time.unscaledDeltaTime)
        {group.alpha=1-Mathf.Clamp01(elapsed/.25f);yield return null;}
        group.alpha=0;IsPlaying=false;var callback=completed;completed=null;callback?.Invoke();
    }
    private void SetRays(float progress,float time)
    {
        for(int i=0;i<rays.Length;i++)
        {
            float angle=i*Mathf.PI*2/rays.Length+time*.18f;
            rays[i].anchoredPosition=new Vector2(0,140)+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*Mathf.Lerp(110,285,progress);
            rays[i].sizeDelta=new Vector2((i%2==0?125:65)*progress,i%2==0?3:1.5f);
            rays[i].localRotation=Quaternion.Euler(0,0,angle*Mathf.Rad2Deg);
            rays[i].GetComponent<Image>().color=new Color(1,.8f,.3f,.55f*progress);
        }
    }
}
