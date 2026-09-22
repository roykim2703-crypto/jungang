using System.Collections.Generic;
using UnityEngine;

public class 수_놓기 : MonoBehaviour
{

    [SerializeField] Vector3 centerPos;

    [SerializeField] float gap = 1f;

    [Header("색")]
    [SerializeField] private bool isBlack = true;

    bool 검은색임 => isBlack;


    [SerializeField] 가이드_돌_코드 guideStoneScript;

    void Start()
    {
        이제너의턴();
    }
    public void 이제너의턴()
    {
        //동기화
        guideStoneScript.centerPos = centerPos;
        guideStoneScript.gap = gap;
        guideStoneScript.isBlack = isBlack;
        
        
        guideStoneScript.격자로놓기시작();
        
    }

    public void 너흑()
    {
        isBlack = true;
    }


    public void 너백()
    {
        isBlack = false;
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
