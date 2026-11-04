using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>Procedural effect placeholders, independent of confirmed card artwork and gameplay randomness.</summary>
public sealed class AugmentEffectView : MonoBehaviour
{
    public bool IsGolemSummon { get; private set; }
    public bool IsAbilityMotion { get; private set; }
    public bool IsSortMotion { get; private set; }
    public bool IsCaptureMotion { get; private set; }
    public static bool IsCapturing(Transform parent)
    {
        if(parent==null)return false;
        foreach(var effect in parent.GetComponentsInChildren<AugmentEffectView>())if(effect.IsCaptureMotion)return true;
        return false;
    }
    public static void PlayCapture(Transform parent,가이드_돌_코드 guide,AugmentBoard.CapturedStone[] captures)
    {
        if(guide==null||captures==null||captures.Length==0)return;
        var root=new GameObject("CaptureStoneSequence");root.transform.SetParent(parent,false);
        var view=root.AddComponent<AugmentEffectView>();view.IsAbilityMotion=true;view.IsCaptureMotion=true;
        view.StartCoroutine(AnimateCapture(root,guide,captures));
    }
    private static IEnumerator AnimateCapture(GameObject root,가이드_돌_코드 guide,AugmentBoard.CapturedStone[] captures)
    {
        var stones=new List<SpriteRenderer>();var rings=new List<LineRenderer>();var positions=new List<Vector3>();
        foreach(var capture in captures)
        {
            if(capture.cell<0||capture.cell>=361||capture.color<1||capture.color>4)continue;
            var sprite=capture.color==2||capture.color==4?guide.흰돌이미지:guide.검은돌이미지;
            var stone=Art(root.transform,sprite,110);if(stone==null)continue;
            stone.gameObject.name="CapturedStone_"+capture.cell;Fit(stone,guide.gap*.9f);
            Vector3 position=Position(guide,capture.cell%19,capture.cell/19);
            stone.transform.position=position;positions.Add(position);stones.Add(stone);
            var ring=PlacementRing(root.transform,new Color(1,.78f,.18f));ring.name="CaptureHighlight_"+capture.cell;
            ring.transform.position=position;ring.transform.localScale=Vector3.one*guide.gap*.52f;
            ring.startWidth=ring.endWidth=guide.gap*.045f;ring.enabled=false;rings.Add(ring);
        }
        int count=stones.Count;if(count==0){Destroy(root);yield break;}
        var scales=stones.ConvertAll(stone=>stone.transform.localScale);
        float interval=Mathf.Min(.18f,1.8f/count),markedAt=(count-1)*interval;
        float removalStart=markedAt+.38f,removeInterval=Mathf.Min(.13f,1.4f/count);
        float duration=removalStart+(count-1)*removeInterval+.32f;
        int announced=0;var audio=FindAnyObjectByType<GameAudio>();
        for(float elapsed=0;elapsed<duration;elapsed+=Time.unscaledDeltaTime)
        {
            while(announced<count&&elapsed>=announced*interval)
            {
                if(audio!=null)audio.PlayCapturePing(announced);
                announced++;
            }
            for(int i=0;i<count;i++)
            {
                float sinceMark=elapsed-i*interval;
                float vanish=Mathf.Clamp01((elapsed-removalStart-i*removeInterval)/.28f);
                float pulse=sinceMark<0?0:Mathf.Sin(Mathf.Clamp01(sinceMark/.28f)*Mathf.PI);
                var stone=stones[i];
                stone.transform.localScale=scales[i]*(1+.16f*pulse)*(1-vanish);
                stone.transform.position=positions[i]+Vector3.up*guide.gap*.36f*vanish;
                Color tint=Color.Lerp(Color.white,new Color(1,.75f,.22f),sinceMark<0?0:.4f+.35f*pulse);
                tint.a=1-vanish;stone.color=tint;
                var ring=rings[i];ring.enabled=sinceMark>=0&&vanish<1;
                ring.transform.localScale=Vector3.one*guide.gap*(.52f+.12f*pulse+.32f*vanish);
                ring.startColor=ring.endColor=new Color(1,.8f,.23f,1-vanish);
            }
            yield return null;
        }
        Destroy(root);
    }
    public static bool IsSorting(Transform parent)
    {
        if(parent==null)return false;
        foreach(var effect in parent.GetComponentsInChildren<AugmentEffectView>())if(effect.IsSortMotion)return true;
        return false;
    }
    public static void PlaySort(Transform parent,가이드_돌_코드 guide,AugmentBoard.SortMotion[] motions,GameObject[] stones)
    {
        if(guide==null||motions==null||motions.Length==0)return;
        var root=new GameObject("SortNumberMotion");root.transform.SetParent(parent,false);
        var view=root.AddComponent<AugmentEffectView>();view.IsAbilityMotion=true;view.IsSortMotion=true;
        view.StartCoroutine(AnimateSort(root,guide,motions,(GameObject[])stones.Clone()));
    }
    private static IEnumerator AnimateSort(GameObject root,가이드_돌_코드 guide,AugmentBoard.SortMotion[] motions,GameObject[] stones)
    {
        var labels=new TextMeshPro[motions.Length];
        var scales=new Vector3[motions.Length];
        for(int i=0;i<motions.Length;i++)
        {
            var motion=motions[i];
            if(motion.to<0||motion.to>=stones.Length||motion.from<0||motion.from>=361||stones[motion.to]==null)continue;
            var stone=stones[motion.to];scales[i]=stone.transform.localScale;
            stone.transform.position=Position(guide,motion.from%19,motion.from/19);
            var number=new GameObject("SortNumber_"+motion.number);number.transform.SetParent(root.transform,false);
            var label=number.AddComponent<TextMeshPro>();labels[i]=label;
            label.text=motion.number.ToString();label.fontSize=5;label.fontStyle=FontStyles.Bold;
            label.alignment=TextAlignmentOptions.Center;label.textWrappingMode=TextWrappingModes.NoWrap;
            label.rectTransform.sizeDelta=new Vector2(guide.gap*.8f,guide.gap*.6f);
            // Fixed world size keeps the number readable regardless of the stone prefab scale.
            label.transform.localScale=Vector3.one*guide.gap;
            label.color=motion.color==1?new Color(1,.9f,.45f):new Color(.18f,.12f,.04f);
            label.renderer.sortingOrder=130;
        }
        const float reveal=.5f,travel=.55f,stagger=.065f,hold=.3f,fade=.22f;
        int maxNumber=1;foreach(var motion in motions)maxNumber=Mathf.Max(maxNumber,motion.number);
        float finish=reveal+travel+(maxNumber-1)*stagger;
        for(float elapsed=0;elapsed<finish+hold+fade;elapsed+=Time.unscaledDeltaTime)
        {
            for(int i=0;i<motions.Length;i++)
            {
                if(labels[i]==null)continue;
                var motion=motions[i];var stone=stones[motion.to];if(stone==null)continue;
                float t=Mathf.Clamp01((elapsed-reveal-(motion.number-1)*stagger)/travel);
                Vector3 from=Position(guide,motion.from%19,motion.from/19),to=Position(guide,motion.to%19,motion.to/19);
                float lift=motion.from==motion.to?0:Mathf.Sin(t*Mathf.PI);
                stone.transform.position=Vector3.Lerp(from,to,Mathf.SmoothStep(0,1,t))+Vector3.up*guide.gap*.32f*lift;
                stone.transform.localScale=scales[i]*(1+.1f*lift);
                labels[i].transform.position=stone.transform.position+Vector3.back*.15f;
                float pop=Mathf.Clamp01(elapsed/.18f);
                labels[i].transform.localScale=Vector3.one*guide.gap*Mathf.Lerp(.55f,1,Mathf.Sin(pop*Mathf.PI*.5f));
                var color=labels[i].color;color.a=1-Mathf.Clamp01((elapsed-finish-hold)/fade);labels[i].color=color;
            }
            yield return null;
        }
        for(int i=0;i<motions.Length;i++)
        {
            if(labels[i]==null)continue;
            var motion=motions[i];var stone=stones[motion.to];if(stone==null)continue;
            stone.transform.position=Position(guide,motion.to%19,motion.to/19);stone.transform.localScale=scales[i];
        }
        Destroy(root);
    }
    public static bool IsAbilityAnimating(Transform parent)
    {
        if(parent==null)return false;
        foreach(var effect in parent.GetComponentsInChildren<AugmentEffectView>())if(effect.IsAbilityMotion)return true;
        foreach(var stack in parent.GetComponentsInChildren<StackStoneView>())if(stack.IsLanding)return true;
        foreach(var box in parent.GetComponentsInChildren<MysteryBoxView>())if(box.IsPlaying)return true;
        if(AbilityScreenView.IsAnimating(parent))return true;
        return false;
    }
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
    private static Material placementMaterial;
    private static LineRenderer PlacementRing(Transform parent,Color color)
    {
        if(placementMaterial==null)placementMaterial=new Material(Shader.Find("Sprites/Default")){hideFlags=HideFlags.HideAndDontSave};
        var go=new GameObject("LandingRing");go.transform.SetParent(parent,false);
        var line=go.AddComponent<LineRenderer>();line.sharedMaterial=placementMaterial;
        line.useWorldSpace=false;line.loop=true;line.positionCount=48;line.sortingOrder=119;
        line.startColor=line.endColor=color;
        for(int i=0;i<48;i++){float angle=i*Mathf.PI*2/48;line.SetPosition(i,new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0));}
        return line;
    }
    public static void PlayPlacement(Transform parent,Vector3 position,float size,int color)
    {
        var root=new GameObject("StonePlacementPulse");root.transform.SetParent(parent,false);root.transform.position=position;
        root.AddComponent<AugmentEffectView>().StartCoroutine(AnimatePlacement(root,size,color));
    }
    public static void PlayTakeoverLanding(Transform parent,Vector3 position,float gap)
    {
        var root=new GameObject("TakeoverLandingPulse");root.transform.SetParent(parent,false);root.transform.position=position;
        var view=root.AddComponent<AugmentEffectView>();view.IsAbilityMotion=true;
        view.StartCoroutine(AnimateTakeoverLanding(root,gap));
    }
    private static IEnumerator AnimateTakeoverLanding(GameObject root,float gap)
    {
        var violet=new Color(.75f,.28f,1);var gold=new Color(1,.8f,.3f);
        var first=PlacementRing(root.transform,violet);var second=PlacementRing(root.transform,gold);
        var rays=new List<SpriteRenderer>();
        for(int i=0;i<20;i++){var ray=Box(root.transform,i%2==0?violet:gold);ray.sortingOrder=120;rays.Add(ray);}
        var corners=new List<SpriteRenderer>();
        for(int i=0;i<4;i++){var corner=Box(root.transform,gold);corner.sortingOrder=121;corners.Add(corner);}
        for(float elapsed=0;elapsed<.85f;elapsed+=Time.unscaledDeltaTime)
        {
            float t=Mathf.Clamp01(elapsed/.85f),ease=1-Mathf.Pow(1-t,3);
            first.transform.localScale=Vector3.one*gap*Mathf.Lerp(.45f,1.8f,ease);
            second.transform.localScale=Vector3.one*gap*Mathf.Lerp(.35f,1.25f,ease);
            first.startWidth=first.endWidth=gap*.07f*(1-t);
            second.startWidth=second.endWidth=gap*.04f*(1-t);
            first.startColor=first.endColor=new Color(violet.r,violet.g,violet.b,1-t);
            second.startColor=second.endColor=new Color(1,.8f,.3f,1-t);
            for(int i=0;i<rays.Count;i++)
            {
                float angle=i*Mathf.PI*2/rays.Count;
                rays[i].transform.localPosition=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*gap*Mathf.Lerp(.4f,2.25f,ease);
                rays[i].transform.localRotation=Quaternion.Euler(0,0,angle*Mathf.Rad2Deg);
                rays[i].transform.localScale=new Vector3(gap*.55f*(1-t),gap*.055f*(1-t),1);
                var color=i%2==0?violet:gold;color.a=1-t;rays[i].color=color;
            }
            for(int i=0;i<corners.Count;i++)
            {
                float angle=i*Mathf.PI*.5f+Mathf.PI*.25f;
                corners[i].transform.localPosition=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*gap*(.8f+.15f*Mathf.Sin(t*Mathf.PI));
                corners[i].transform.localRotation=Quaternion.Euler(0,0,45+t*90);
                corners[i].transform.localScale=Vector3.one*gap*.16f*(1-t);
                corners[i].color=new Color(1,.8f,.3f,1-t);
            }
            yield return null;
        }
        Destroy(root);
    }
    private static IEnumerator AnimatePlacement(GameObject root,float size,int color)
    {
        Color tint=color==2?new Color(.05f,.7f,1):color==4?new Color(.85f,.3f,1):new Color(1,.4f,.05f);
        var first=PlacementRing(root.transform,tint);var second=PlacementRing(root.transform,tint);
        var backFirst=PlacementRing(root.transform,Color.black);var backSecond=PlacementRing(root.transform,Color.black);
        backFirst.sortingOrder=backSecond.sortingOrder=118;
        for(float time=0;time<.65f;time+=Time.unscaledDeltaTime)
        {
            for(int i=0;i<2;i++)
            {
                var ring=i==0?first:second;float t=Mathf.Clamp01((time-i*.1f)/.52f);
                var backing=i==0?backFirst:backSecond;
                ring.enabled=time>=i*.1f;
                backing.enabled=ring.enabled;
                ring.transform.localScale=Vector3.one*size*Mathf.Lerp(.38f,.9f,1-Mathf.Pow(1-t,2));
                backing.transform.localScale=ring.transform.localScale;
                ring.startWidth=ring.endWidth=size*.055f*(1-t*.6f);
                backing.startWidth=backing.endWidth=ring.startWidth+size*.025f;
                var c=tint;c.a=(1-t)*.65f;ring.startColor=ring.endColor=c;
                backing.startColor=backing.endColor=new Color(.12f,.08f,.03f,(1-t)*.45f);
            }
            yield return null;
        }
        Destroy(root);
    }
    public static GameObject CreatePlacementMarker(Transform parent,Vector3 position,float size)
    {
        var root=new GameObject("LastPlacementMarker");root.transform.SetParent(parent,false);root.transform.position=position;
        var edge=Box(root.transform,new Color(1,.96f,.75f));edge.sortingOrder=116;
        edge.transform.localScale=Vector3.one*size*.22f;edge.transform.localRotation=Quaternion.Euler(0,0,45);
        var center=Box(root.transform,new Color(.92f,.18f,.12f));center.sortingOrder=117;
        center.transform.localScale=Vector3.one*size*.13f;center.transform.localRotation=Quaternion.Euler(0,0,45);
        return root;
    }
    public static GameObject CreateAnchoredMarker(Transform parent,Vector3 position,float gap)
    {
        var root=new GameObject("AbsoluteStoneMarker");root.transform.SetParent(parent,false);root.transform.position=position;
        for(int y=-1;y<=1;y+=2)for(int x=-1;x<=1;x+=2)
        {
            var backing=Box(root.transform,new Color(.12f,.15f,.2f));backing.sortingOrder=112;
            backing.transform.localPosition=new Vector3(x*gap*.33f,y*gap*.33f,0);backing.transform.localScale=Vector3.one*gap*.19f;
            var bolt=Box(root.transform,new Color(.3f,.85f,1));bolt.sortingOrder=113;
            bolt.transform.localPosition=backing.transform.localPosition;bolt.transform.localScale=Vector3.one*gap*.1f;
        }
        return root;
    }
    public static void Play(Transform parent,가이드_돌_코드 guide,int id,int x,int y,int power,int[] before,int[] after,Vector2Int direction=default,int[] removedCells=null)
    {
        if(guide==null||AugmentVisibility.IsSecret(id))return;
        var root=new GameObject("AugmentEffect_"+id);root.transform.SetParent(parent,false);
        var view=root.AddComponent<AugmentEffectView>();view.IsGolemSummon=id==14;view.IsAbilityMotion=true;
        view.StartCoroutine(Animate(root,guide,id,x,y,power,before,after,direction));
        if(removedCells!=null&&removedCells.Length>0)
        {
            var markers=new GameObject("RemovedStoneMarkers");markers.transform.SetParent(parent,false);
            var markerView=markers.AddComponent<AugmentEffectView>();
            markerView.StartCoroutine(ShowRemovedCells(markers,guide,removedCells));
        }
    }
    private static IEnumerator ShowRemovedCells(GameObject root,가이드_돌_코드 guide,int[] cells)
    {
        var marks=new List<SpriteRenderer>();
        foreach(int cell in cells)
        {
            if(cell<0||cell>=361)continue;
            Vector3 pos=Position(guide,cell%19,cell/19);
            var fill=Box(root.transform,new Color(1,.08f,.04f,.25f));fill.transform.position=pos;
            fill.transform.localScale=Vector3.one*guide.gap*.95f;marks.Add(fill);
            for(int i=0;i<2;i++)
            {
                var slash=Box(root.transform,new Color(1,.12f,.04f));slash.sortingOrder=115;
                slash.transform.position=pos;slash.transform.rotation=Quaternion.Euler(0,0,i==0?45:-45);
                slash.transform.localScale=new Vector3(guide.gap*.85f,guide.gap*.1f,1);marks.Add(slash);
            }
        }
        for(float t=0;t<3f;t+=Time.unscaledDeltaTime)
        {
            foreach(var mark in marks){var c=mark.color;c.a=(mark.sortingOrder==115?1f:.25f)*Mathf.Clamp01((3f-t)*2);mark.color=c;}
            yield return null;
        }
        Destroy(root);
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
        if(Artwork==null||Artwork.GolemBody==null)
        {
            var fallback=BuildGolem(parent,center,gap,false);
            if(fallback!=null)
            {
                var parts=fallback.GetComponentsInChildren<SpriteRenderer>();
                if(parts.Length>0){var bounds=parts[0].bounds;foreach(var part in parts)bounds.Encapsulate(part.bounds);fallback.transform.localScale*=gap*2.85f/Mathf.Max(.01f,bounds.size.x,bounds.size.y);}
                GolemFootprint(fallback.transform,gap);
            }
            return fallback;
        }
        var root=new GameObject("GolemBodyOnBoard");root.transform.SetParent(parent,false);root.transform.position=center;
        var body=Art(root.transform,Artwork.GolemBody,96);
        body.transform.localScale=Vector3.one*(gap*2.85f/Mathf.Max(.01f,body.sprite.bounds.size.x,body.sprite.bounds.size.y));
        GolemFootprint(root.transform,gap);
        root.AddComponent<GolemBoardVisual>().Configure(parent,body);
        return root;
    }
    private static void GolemFootprint(Transform parent,float gap)
    {
        var ring=PlacementRing(parent,new Color(1,.7f,.18f,.9f));ring.name="GolemFootprint3x3";
        ring.positionCount=4;ring.sortingOrder=94;ring.startWidth=ring.endWidth=gap*.04f;
        ring.SetPosition(0,new Vector3(-1.5f,-1.5f)*gap);ring.SetPosition(1,new Vector3(1.5f,-1.5f)*gap);
        ring.SetPosition(2,new Vector3(1.5f,1.5f)*gap);ring.SetPosition(3,new Vector3(-1.5f,1.5f)*gap);
        var floor=Box(parent,new Color(1,.65f,.1f,.08f));floor.sortingOrder=93;floor.transform.localScale=new Vector3(gap*3,gap*3,1);
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
        else if(id==9){var landing=AugmentAim.CatapultLanding(new Vector2Int(x,y),direction,power);center=Position(guide,landing.x,landing.y);}
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
            float boardScale=2.85f*source.bounds.size.x/(6.4f*Mathf.Max(source.bounds.size.x,source.bounds.size.y));
            portrait.transform.localScale=Vector3.one*Mathf.Lerp(zoom,boardScale,settle);
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
