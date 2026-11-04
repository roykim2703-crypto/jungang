using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Used cards retain their names and artwork, rendered in gray in both sizes.</summary>
public sealed class CardUsedAppearance : MonoBehaviour
{
    private static Material grayscale;
    private readonly Dictionary<Image,Material> materials=new Dictionary<Image,Material>();
    private readonly Dictionary<Graphic,Color> colors=new Dictionary<Graphic,Color>();
    public static void Apply(GameObject root,bool used)
    {
        var view=root.GetComponent<CardUsedAppearance>();if(view==null)view=root.AddComponent<CardUsedAppearance>();
        view.SetUsed(used);
    }
    private void SetUsed(bool used)
    {
        if(used&&grayscale==null)
            grayscale=new Material(Resources.Load<Shader>("Effects/CardGrayscale")){hideFlags=HideFlags.HideAndDontSave};
        foreach(var image in GetComponentsInChildren<Image>(true))
        {
            if(!materials.ContainsKey(image)){materials.Add(image,image.material);colors[image]=image.color;}
            image.material=used?grayscale:materials[image];
            var color=colors[image];image.color=used?new Color(.63f,.63f,.63f,color.a):color;
        }
        foreach(var text in GetComponentsInChildren<TMP_Text>(true))
        {
            if(!colors.ContainsKey(text))colors.Add(text,text.color);
            var color=colors[text];text.color=used?new Color(.38f,.38f,.38f,color.a):color;
        }
    }
}
