using UnityEngine;

/// <summary>Nine hoshi at the fourth, tenth and sixteenth lines of the 19-line board.</summary>
public static class BoardStarPoints
{
    private static Sprite dot;
    public static void Create(Transform parent,Vector3 center,float gap)
    {
        var root=new GameObject("BoardStarPoints");root.transform.SetParent(parent,false);
        if(dot==null)
        {
            const int size=32;
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
            texture.name="BoardStarPoint";texture.wrapMode=TextureWrapMode.Clamp;
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float distance=new Vector2(x+0.5f-size*.5f,y+0.5f-size*.5f).magnitude;
                pixels[y*size+x]=new Color(.10f,.075f,.045f,Mathf.Clamp01(size*.5f-distance));
            }
            texture.SetPixels(pixels);texture.Apply(false,true);
            dot=Sprite.Create(texture,new Rect(0,0,size,size),Vector2.one*.5f,size);
        }
        foreach(int y in new[]{3,9,15})foreach(int x in new[]{3,9,15})
        {
            var point=new GameObject($"StarPoint_{x+1}_{y+1}");point.transform.SetParent(root.transform,false);
            point.transform.position=new Vector3(center.x+(x-9)*gap,center.y-(y-9)*gap,0);
            point.transform.localScale=Vector3.one*gap*.23f;
            var renderer=point.AddComponent<SpriteRenderer>();renderer.sprite=dot;renderer.sortingOrder=2;
        }
    }
}
