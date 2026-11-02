using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class MoveHistoryLineClick : MonoBehaviour,IPointerClickHandler
{
    private TMP_Text label;
    public void Bind(TMP_Text value){label=value;}
    public void OnPointerClick(PointerEventData eventData)
    {
        if(label==null||AugmentMatch.Instance==null||!AugmentMatch.Instance.IsReviewMode)return;
        int line=TMP_TextUtilities.FindIntersectingLine(label,eventData.position,eventData.pressEventCamera);
        if(line>=0)AugmentMatch.Instance.ShowReviewMove(line);
    }
}
