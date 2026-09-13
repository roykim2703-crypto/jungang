using System.Collections.Generic;
using UnityEngine;

public class 수_놓기 : MonoBehaviour
{

    [SerializeField] Vector3 centerPos;

    [SerializeField] float gap = 1f;

    [Header("색")]
    [SerializeField] bool isBlack = true;


    가이드_돌_코드 guideStoneScript;

    void Start()
    {
        
    }
    public void 이제너의턴()
    {
        //동기화
        guideStoneScript.centerPos = centerPos;
        guideStoneScript.gap = gap;
        guideStoneScript.isBlack = isBlack;
        
        
        guideStoneScript.격자로놓기시작();
        
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
