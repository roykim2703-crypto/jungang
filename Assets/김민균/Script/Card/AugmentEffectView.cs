using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Procedural effect placeholders, independent of confirmed card artwork and gameplay randomness.</summary>
public sealed class AugmentEffectView : MonoBehaviour
{
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
        if(guide==null)return;
        var root=new GameObject("AugmentEffect_"+id);root.transform.SetParent(parent,false);
        root.AddComponent<AugmentEffectView>().StartCoroutine(Animate(root,guide,id,x,y,power,before,after,direction));
    }
    private static Vector3 Position(가이드_돌_코드 guide,int x,int y)=>
        new Vector3(guide.centerPos.x+(x-9)*guide.gap,guide.centerPos.y-(y-9)*guide.gap,-.1f);
    private static SpriteRenderer Box(Transform parent,Color color)
    {
        var go=new GameObject("Box");go.transform.SetParent(parent,false);
        var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=Square;renderer.sortingOrder=100;
        renderer.color=color;return renderer;
    }
    private static IEnumerator Animate(GameObject root,가이드_돌_코드 guide,int id,int x,int y,int power,int[] before,int[] after,Vector2Int direction)
    {
        Vector3 center=Position(guide,x,y);Color color=Tint(id);float gap=guide.gap;
        Vector3 launch=center;
        if(id==3||id==9)center+=new Vector3(direction.x,-direction.y,0)*gap*Mathf.Clamp(power,1,3);
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
}
