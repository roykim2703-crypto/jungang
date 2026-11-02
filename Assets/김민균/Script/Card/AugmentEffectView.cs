using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Procedural effect placeholders, independent of confirmed card artwork and gameplay randomness.</summary>
public sealed class AugmentEffectView : MonoBehaviour
{
    public bool IsGolemSummon { get; private set; }
    public static bool IsGolemSummoning(Transform parent)
    {
        if(parent==null)return false;
        foreach(var effect in parent.GetComponentsInChildren<AugmentEffectView>())if(effect.IsGolemSummon)return true;
        return false;
    }
    private static AugmentEffectArtworkCatalog artwork;
    public static AugmentEffectArtworkCatalog Artwork => artwork != null ? artwork : artwork = Resources.Load<AugmentEffectArtworkCatalog>("AugmentEffectArtworkCatalog");
    private static Sprite square;
    public static Sprite Square
    {
        get
        {
            if(square!=null)return square;
            var texture=new Texture2D(1,1){name="EffectBoxTexture",filterMode=FilterMode.Point,hideFlags=HideFlags.HideAndDontSave};
            texture.SetPixel(0,0,Color.white);texture.Apply();
            square=Sprite.Create(texture,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);
            square.name="EffectBox";square.hideFlags=HideFlags.HideAndDontSave;return square;
        }
    }
    public static Color Tint(int id)=>Color.HSVToRGB(Mathf.Repeat(id*.137f,.999f),.55f,.95f);
    public static void Play(Transform parent,가이드_돌_코드 guide,int id,int x,int y,int power,int[] before,int[] after,Vector2Int direction=default)
    {
        if(guide==null||AugmentVisibility.IsSecret(id))return;
        var root=new GameObject("AugmentEffect_"+id);root.transform.SetParent(parent,false);
        var view=root.AddComponent<AugmentEffectView>();view.IsGolemSummon=id==14;
        view.StartCoroutine(Animate(root,guide,id,x,y,power,before,after,direction));
    }
    private static Vector3 Position(가이드_돌_코드 guide,int x,int y)=>
        new Vector3(guide.centerPos.x+(x-9)*guide.gap,guide.centerPos.y-(y-9)*guide.gap,-.1f);
    private static SpriteRenderer Box(Transform parent,Color color)
    {
        var go=new GameObject("Box");go.transform.SetParent(parent,false);
        var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=Square;renderer.sortingOrder=100;
        renderer.color=color;return renderer;
    }
    private static SpriteRenderer Art(Transform parent,Sprite sprite,int order=105)
    {
        if(sprite==null)return null;
        var go=new GameObject(sprite.name);go.transform.SetParent(parent,false);
        var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sortingOrder=order;return renderer;
    }
    private static void Fit(SpriteRenderer renderer,float worldWidth)
    {
        if(renderer==null||renderer.sprite==null)return;
        float scale=worldWidth/Mathf.Max(.01f,renderer.sprite.bounds.size.x);
        renderer.transform.localScale=Vector3.one*scale;
    }
    public static Sprite CatapultFrame(int index)
    {
        var frames=Artwork!=null?Artwork.CatapultFrames:null;
        return frames!=null&&index>=0&&index<frames.Length?frames[index]:null;
    }
    public static GameObject CreateBlackHole(Transform parent,Vector3 center,float size)
    {
        if(Artwork==null||Artwork.BlackHole==null)return null;
        var root=new GameObject("BlackHoleLoop");root.transform.SetParent(parent,false);root.transform.position=center;
        root.transform.rotation=Quaternion.Euler(0,0,Time.unscaledTime*38f);
        var renderer=Art(root.transform,Artwork.BlackHole,95);Fit(renderer,size);
        root.AddComponent<AugmentLoopingVisual>().Configure(38f,.055f);
        return root;
    }
    public static GameObject CreateGolemIdle(Transform parent,Vector3 center,float gap)
    {
        if(Artwork==null||Artwork.GolemBody==null)return BuildGolem(parent,center,gap,false);
        var root=new GameObject("GolemBodyOnBoard");root.transform.SetParent(parent,false);root.transform.position=center;
        var body=Art(root.transform,Artwork.GolemBody,96);Fit(body,gap*.92f);
        root.AddComponent<GolemBoardVisual>().Configure(parent,body);
        return root;
    }
    private static IEnumerator Animate(GameObject root,가이드_돌_코드 guide,int id,int x,int y,int power,int[] before,int[] after,Vector2Int direction)
    {
        Vector3 center=Position(guide,x,y);Color color=Tint(id);float gap=guide.gap;
        Vector3 launch=center;
        if(id==3)
        {
            Vector2 precise=AugmentAim.PreciseDirection(direction);
            center+=new Vector3(precise.x,-precise.y,0)*gap*Mathf.Clamp(power,1,8);
        }
        else if(id==9)center+=new Vector3(direction.x,-direction.y,0)*gap*AugmentAim.CatapultDistance(power);
        if(id==1&&Artwork!=null&&Artwork.Fist!=null){yield return AnimateFist(root,center,gap);yield break;}
        if(id==5&&Artwork!=null&&Artwork.Hammer!=null){yield return AnimateHammer(root,guide);yield break;}
        if(id==9&&CatapultFrame(0)!=null){yield return AnimateCatapult(root,guide,center,power,direction);yield break;}
        if(id==11&&Artwork!=null&&Artwork.MeteorRock!=null){yield return AnimateMeteor(root,guide,center,power);yield break;}
        if(id==14&&Artwork!=null&&Artwork.GolemBody!=null){yield return AnimateGolem(root,guide,center,gap);yield break;}
        bool projectile=id==3||id==9||id==11, global=id==4||id==5||id==13||id==20;
        int area=id==6?2:id==11?2*Mathf.Clamp(power,1,5)-1:id==1||id==15||id==19?3:1;
        var pulse=Box(root.transform,color);
        var fragments=new List<SpriteRenderer>();var positions=new List<Vector3>();var removed=new List<bool>();
        for(int p=0;p<361;p++)
        {
            if(before[p]==after[p])continue;
            var fragment=Box(root.transform,after[p]==0?new Color(1,.35f,.2f):after[p]==2?new Color(.75f,.9f,1):color);
            fragments.Add(fragment);positions.Add(Position(guide,p%19,p/19));removed.Add(after[p]==0);
        }
        var rays=new List<SpriteRenderer>();
        for(int i=0;i<8;i++)rays.Add(Box(root.transform,color));
        float duration=projectile?.8f:.55f;
        for(float elapsed=0;elapsed<duration;elapsed+=Time.unscaledDeltaTime)
        {
            float t=elapsed/duration,ease=1-Mathf.Pow(1-t,3);
            pulse.transform.position=id==3||id==9?Vector3.Lerp(launch,center,ease)+(id==9?Vector3.up*Mathf.Sin(t*Mathf.PI)*gap*2:Vector3.zero)
                :center+(projectile?Vector3.up*gap*5*(1-ease):Vector3.zero);
            float size=id==3||id==9?gap*.7f:gap*(id==14?1.8f:area)*(.3f+ease);
            pulse.transform.localScale=new Vector3(size,size,1);
            pulse.transform.rotation=Quaternion.Euler(0,0,(id==15?-1:1)*t*90);
            pulse.color=new Color(color.r,color.g,color.b,(1-t)*.55f);
            for(int i=0;i<fragments.Count;i++)
            {
                var fragment=fragments[i];Vector3 offset=removed[i]?new Vector3(Mathf.Sin(i*2.4f),Mathf.Cos(i*2.4f),0)*gap*ease:Vector3.zero;
                fragment.transform.position=positions[i]+offset;
                float scale=gap*(removed[i]?1-t:Mathf.Sin(t*Mathf.PI))*.85f;
                fragment.transform.localScale=new Vector3(scale,scale,1);
                fragment.transform.rotation=Quaternion.Euler(0,0,t*120);
                Color c=fragment.color;c.a=1-t;fragment.color=c;
            }
            for(int i=0;i<rays.Count;i++)
            {
                float angle=i*Mathf.PI/4+t;
                rays[i].transform.position=center+new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*gap*(global?5:area)*ease;
                rays[i].transform.localScale=Vector3.one*gap*.2f*(1-t);
            }
            yield return null;
        }
        Destroy(root);
    }

    private static IEnumerator AnimateHammer(GameObject root,가이드_돌_코드 guide)
    {
        float gap=guide.gap;
        Vector3 center=new Vector3(guide.centerPos.x,guide.centerPos.y,-.18f);
        var hammer=Art(root.transform,Artwork.Hammer,112);Fit(hammer,gap*4f);
        Vector3 impact=center+Vector3.up*gap*.25f;
        Vector3 raised=impact+Vector3.up*gap*1.4f;
        hammer.transform.rotation=Quaternion.Euler(0,0,-42f);
        hammer.transform.position=raised;
        for(int hit=0;hit<2;hit++)
        {
            for(float elapsed=0;elapsed<.09f;elapsed+=Time.unscaledDeltaTime)
            {
                float t=Mathf.Clamp01(elapsed/.09f);
                hammer.transform.position=Vector3.Lerp(raised,impact,t*t);
                yield return null;
            }
            hammer.transform.position=impact;
            for(float elapsed=0;elapsed<.07f;elapsed+=Time.unscaledDeltaTime)
            {
                yield return null;
            }
            if(hit==1)break;
            for(float elapsed=0;elapsed<.16f;elapsed+=Time.unscaledDeltaTime)
            {
                float t=Mathf.Clamp01(elapsed/.16f);
                hammer.transform.position=Vector3.Lerp(impact,raised,Mathf.SmoothStep(0,1,t));
                yield return null;
            }
            hammer.transform.position=raised;
        }
        for(float elapsed=0;elapsed<.1f;elapsed+=Time.unscaledDeltaTime)
        {
            Color c=hammer.color;c.a=1-Mathf.Clamp01(elapsed/.1f);hammer.color=c;
            yield return null;
        }
        Destroy(root);
    }

    private static IEnumerator AnimateFist(GameObject root,Vector3 target,float gap)
    {
        var fist=Art(root.transform,Artwork.Fist);Fit(fist,gap*2.1f);
        fist.transform.rotation=Quaternion.Euler(0,0,90); // artwork faces up; turn it left as requested
        Vector3 baseScale=fist.transform.localScale;
        Vector3 start=target+Vector3.up*gap*4.5f;
        fist.transform.position=start;
        for(float elapsed=0;elapsed<.12f;elapsed+=Time.unscaledDeltaTime)
        {
            float t=Mathf.Clamp01(elapsed/.12f);
            fist.transform.position=Vector3.Lerp(start,target,t*t);
            yield return null;
        }
        fist.transform.position=target;
        var impact=Box(root.transform,new Color(1f,.78f,.32f,.65f));impact.sortingOrder=104;impact.transform.position=target;
        for(float elapsed=0;elapsed<.09f;elapsed+=Time.unscaledDeltaTime)
        {
            float t=Mathf.Clamp01(elapsed/.09f),hit=1-t;
            fist.transform.localScale=Vector3.Scale(baseScale,new Vector3(1+.16f*hit,1-.18f*hit,1));
            fist.color=new Color(1,1,1,1-t);
            impact.transform.localScale=new Vector3(gap*Mathf.Lerp(1f,3f,t),gap*Mathf.Lerp(.3f,.7f,t),1);
            impact.color=new Color(1f,.78f,.32f,.65f*(1-t));
            yield return null;
        }
        Destroy(root);
    }

    private static IEnumerator AnimateCatapult(GameObject root,가이드_돌_코드 guide,Vector3 target,int power,Vector2Int direction)
    {
        float gap=guide.gap;Vector3 basePos=guide.centerPos+Vector3.down*gap*8.1f;
        var machine=Art(root.transform,CatapultFrame(1));Fit(machine,gap*2.25f);machine.transform.position=basePos;
        Vector2 worldDirection=new Vector2(direction.x,-direction.y);
        machine.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(worldDirection.y,worldDirection.x)*Mathf.Rad2Deg+90f);
        var stone=Art(root.transform,Artwork!=null?Artwork.MeteorRock:null,106);Fit(stone,gap*.72f);stone.transform.position=basePos+Vector3.up*gap*.8f;
        float distance=AugmentAim.CatapultDistance(power);
        float duration=.68f+distance*.025f;
        for(float elapsed=0;elapsed<duration;elapsed+=Time.unscaledDeltaTime)
        {
            float t=elapsed/duration;
            machine.sprite=t<.18f?CatapultFrame(1):CatapultFrame(2);
            float ease=1-Mathf.Pow(1-t,3);
            stone.transform.position=Vector3.Lerp(basePos,target,ease)+Vector3.up*Mathf.Sin(t*Mathf.PI)*gap*(3f+distance*.18f);
            stone.transform.Rotate(0,0,360*Time.unscaledDeltaTime);
            yield return null;
        }
        Destroy(root);
    }

    private static IEnumerator AnimateMeteor(GameObject root,가이드_돌_코드 guide,Vector3 center,int power)
    {
        float gap=guide.gap;float diameter=(Mathf.Clamp(power,1,5)*2-1)*gap;
        var meteor=Art(root.transform,Artwork.MeteorRock);Fit(meteor,diameter*.78f);
        Vector3 start=center+Vector3.up*(gap*11+diameter*.35f)+Vector3.right*gap*2;
        for(float t=0;t<1;t+=Time.unscaledDeltaTime/.82f)
        {
            float ease=t*t;
            meteor.transform.position=Vector3.Lerp(start,center,ease);
            meteor.transform.localScale=Vector3.one*(diameter/Mathf.Max(.01f,meteor.sprite.bounds.size.x))*(.35f+.65f*ease);
            meteor.transform.Rotate(0,0,-150*Time.unscaledDeltaTime);yield return null;
        }
        meteor.enabled=false;
        if(Artwork.MeteorImpact!=null)
        {
            var impact=Art(root.transform,Artwork.MeteorImpact,107);Fit(impact,diameter*1.35f);impact.transform.position=center;
            for(float t=0;t<1;t+=Time.unscaledDeltaTime/.38f){Color c=impact.color;c.a=1-t;impact.color=c;impact.transform.localScale*=1+Time.unscaledDeltaTime*.8f;yield return null;}
        }
        Destroy(root);
    }

    private static readonly Vector2[] GolemTargets={
        new Vector2(0,2.55f),new Vector2(0,1.9f),new Vector2(0,1.55f),new Vector2(0,.75f),new Vector2(0,.05f),
        new Vector2(-1.05f,1.15f),new Vector2(1.05f,1.15f),new Vector2(0,-.28f),new Vector2(0,-.55f),
        new Vector2(-.62f,-.82f),new Vector2(-1.05f,-1.45f),new Vector2(-.35f,-1.45f),new Vector2(.35f,-1.45f),new Vector2(1.05f,-1.45f),new Vector2(.62f,-.82f)
    };
    private static GameObject BuildGolem(Transform parent,Vector3 center,float gap,bool scattered)
    {
        if(Artwork==null||Artwork.GolemParts==null||Artwork.GolemParts.Length<15)return null;
        var root=new GameObject(scattered?"GolemSummon":"GolemIdle");root.transform.SetParent(parent,false);root.transform.position=center;
        for(int i=0;i<15;i++)
        {
            var part=Art(root.transform,Artwork.GolemParts[i],100+i);
            part.transform.localScale=Vector3.one*gap*.72f;
            part.transform.localPosition=scattered?(Vector3)(GolemTargets[i].normalized*gap*7f):(Vector3)(GolemTargets[i]*gap);
            part.transform.localRotation=Quaternion.Euler(0,0,scattered?(i%2==0?1:-1)*(35+i*13):0);
            part.name="GolemPart_"+i;
        }
        return root;
    }
    private static IEnumerator AnimateGolem(GameObject root,가이드_돌_코드 guide,Vector3 target,float gap)
    {
        Vector3 stage=new Vector3(guide.centerPos.x,guide.centerPos.y,-.25f);
        var dim=Box(root.transform,new Color(.025f,.015f,.07f,0));dim.sortingOrder=120;
        dim.transform.position=stage;dim.transform.localScale=new Vector3(gap*19,gap*19,1);
        var ring=new GameObject("SummoningCircle");ring.transform.SetParent(root.transform,false);ring.transform.position=stage;
        var runes=new List<SpriteRenderer>();
        for(int i=0;i<48;i++)
        {
            float angle=i*Mathf.PI*2/48;
            var rune=Box(ring.transform,new Color(1,.71f,.2f,0));rune.sortingOrder=125;
            rune.transform.localPosition=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*gap*5;
            rune.transform.localRotation=Quaternion.Euler(0,0,angle*Mathf.Rad2Deg);
            rune.transform.localScale=new Vector3(gap*(i%4==0?.55f:.18f),gap*.09f,1);runes.Add(rune);
        }
        var golem=BuildGolem(root.transform,stage,gap,true);
        var parts=golem!=null?golem.GetComponentsInChildren<SpriteRenderer>():new SpriteRenderer[0];
        foreach(var part in parts){part.sortingOrder+=35;part.color=new Color(1,.74f,.32f,0);}
        // A portrait made from separate bands preserves the authored complete pose while
        // head, shoulders and lower body move independently during the summon cut-in.
        var portrait=new GameObject("GolemLivePortrait");portrait.transform.SetParent(root.transform,false);portrait.transform.position=stage;
        var layers=new List<SpriteRenderer>();var layerPositions=new List<Vector3>();var transientSprites=new List<Sprite>();
        Sprite source=Artwork.GolemBody;Rect sourceRect=source.rect;
        float portraitScale=gap*6.4f/source.bounds.size.x;
        for(int i=0;i<3;i++)
        {
            float bottom=sourceRect.height*i/3f,height=sourceRect.height/3f;
            var sprite=Sprite.Create(source.texture,new Rect(sourceRect.x,sourceRect.y+bottom,sourceRect.width,height),new Vector2(.5f,.5f),source.pixelsPerUnit,0,SpriteMeshType.FullRect);
            transientSprites.Add(sprite);
            var layer=Art(portrait.transform,sprite,155+i);layer.name=i==2?"HeadLayer":i==1?"ShoulderLayer":"LowerBodyLayer";
            layer.transform.localScale=Vector3.one*portraitScale;
            Vector3 pos=Vector3.up*((bottom+height*.5f-sourceRect.height*.5f)/source.pixelsPerUnit)*portraitScale;
            layer.transform.localPosition=pos;layerPositions.Add(pos);layer.color=new Color(1,1,1,0);layers.Add(layer);
        }
        var flash=Box(root.transform,new Color(1,.9f,.58f,0));flash.sortingOrder=160;flash.transform.position=stage;flash.transform.localScale=Vector3.one*gap*19;
        const float duration=3.1f;
        for(float elapsed=0;elapsed<duration;elapsed+=Time.unscaledDeltaTime)
        {
            float reveal=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.85f,1.3f,elapsed));
            float settle=Mathf.SmoothStep(0,1,Mathf.InverseLerp(2.45f,duration,elapsed));
            dim.color=new Color(.025f,.015f,.07f,.82f*Mathf.Clamp01(elapsed/.3f)*(1-settle));
            ring.transform.rotation=Quaternion.Euler(0,0,elapsed*85);
            ring.transform.localScale=Vector3.one*(.35f+.65f*Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.65f)))*(1-settle);
            foreach(var rune in runes)rune.color=new Color(1,.71f,.2f,Mathf.Clamp01(elapsed/.3f)*(1-settle));
            float assemble=1-Mathf.Pow(1-Mathf.Clamp01((elapsed-.25f)/.95f),3);
            for(int i=0;i<parts.Length;i++)
            {
                parts[i].transform.localPosition=Vector3.Lerp((Vector3)(GolemTargets[i].normalized*gap*9f),(Vector3)(GolemTargets[i]*gap),assemble);
                parts[i].transform.localRotation=Quaternion.Euler(0,0,(1-assemble)*(i%2==0?150:-150));
                parts[i].color=new Color(1,.78f,.4f,Mathf.Clamp01(elapsed/.3f)*(1-reveal));
            }
            portrait.transform.position=Vector3.Lerp(stage,target,settle);
            float zoom=1f+Mathf.Sin(Mathf.Clamp01((elapsed-1.1f)/1.35f)*Mathf.PI)*.07f;
            portrait.transform.localScale=Vector3.one*Mathf.Lerp(zoom,.92f/6.4f,settle);
            for(int i=0;i<layers.Count;i++)
            {
                float motion=Mathf.Sin(elapsed*3.5f+i*.65f)*(1-settle);
                layers[i].transform.localPosition=layerPositions[i]+new Vector3(motion*gap*.045f,i==2?motion*gap*.07f:0,0)*reveal;
                layers[i].transform.localRotation=Quaternion.Euler(0,0,motion*(i==2?1.4f:.65f)*reveal);
                layers[i].color=new Color(1,1,1,reveal);
            }
            float flashAlpha=Mathf.Clamp01(1-Mathf.Abs(elapsed-1.12f)/.13f)*.85f;
            flash.color=new Color(1,.9f,.58f,flashAlpha);
            yield return null;
        }
        foreach(var sprite in transientSprites)Destroy(sprite);
        Destroy(root);
    }
}

public sealed class GolemBoardVisual : MonoBehaviour
{
    private Transform matchRoot;private SpriteRenderer body;
    public void Configure(Transform owner,SpriteRenderer renderer){matchRoot=owner;body=renderer;Update();}
    private void Update(){if(body!=null)body.enabled=!AugmentEffectView.IsGolemSummoning(matchRoot);}
}

public sealed class AugmentLoopingVisual : MonoBehaviour
{
    private float degreesPerSecond,breathe;private Vector3 baseScale;
    public void Configure(float degrees,float amount){degreesPerSecond=degrees;breathe=amount;baseScale=transform.localScale;}
    private void Update()
    {
        transform.Rotate(0,0,degreesPerSecond*Time.unscaledDeltaTime);
        transform.localScale=baseScale*(1+Mathf.Sin(Time.unscaledTime*2.4f)*breathe);
    }
}
