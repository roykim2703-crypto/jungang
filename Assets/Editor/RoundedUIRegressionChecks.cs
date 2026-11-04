using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class RoundedUIRegressionChecks
{
    [MenuItem("Tools/UI/Validate Rounded Artwork Geometry")]
    public static void Run()
    {
        foreach(var size in new[]{new Vector2(50,35),new Vector2(35,50),new Vector2(35,35),new Vector2(10,10),new Vector2(155,109)})
        {
            var root=new GameObject("RoundedArtworkGeometryCheck",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
            root.hideFlags=HideFlags.HideAndDontSave;
            try
            {
                var image=root.GetComponent<Image>();image.rectTransform.sizeDelta=size;RoundedUI.Apply(image,18);
                using(var mesh=new VertexHelper())
                {
                    var half=size*.5f;
                    mesh.AddVert(new Vector3(-half.x,-half.y),Color.white,Vector2.zero);
                    mesh.AddVert(new Vector3(-half.x,half.y),Color.white,Vector2.up);
                    mesh.AddVert(new Vector3(half.x,half.y),Color.white,Vector2.one);
                    mesh.AddVert(new Vector3(half.x,-half.y),Color.white,Vector2.right);
                    mesh.AddTriangle(0,1,2);mesh.AddTriangle(2,3,0);
                    root.GetComponent<RoundedUI>().ModifyMesh(mesh);
                    var vertices=new List<UIVertex>();mesh.GetUIVertexStream(vertices);float area=0;
                    for(int i=0;i<vertices.Count;i+=3)
                    {
                        Vector3 a=vertices[i].position,b=vertices[i+1].position,c=vertices[i+2].position;
                        area+=Mathf.Abs(Vector3.Cross(b-a,c-a).z)*.5f;
                    }
                    if(float.IsNaN(area)||area<size.x*size.y*.6f)
                        throw new InvalidOperationException($"Rounded artwork collapsed at {size}: area {area}");
                    foreach(var vertex in vertices)
                    {
                        var p=vertex.position;
                        if(float.IsNaN(p.x)||float.IsNaN(p.y)||Mathf.Abs(p.x)>half.x+.001f||Mathf.Abs(p.y)>half.y+.001f)
                            throw new InvalidOperationException($"Rounded artwork escaped its bounds at {size}: {p}");
                    }
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        Debug.Log("Rounded artwork geometry: all 5 sizes passed.");
    }
}
