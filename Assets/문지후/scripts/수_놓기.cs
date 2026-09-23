using System.Collections;
using UnityEngine;

public class 수_놓기 : MonoBehaviour
{

    [SerializeField] Vector3 centerPos;

    [SerializeField] float gap = 1f;

    [Header("색")]
    [SerializeField] private bool isBlack = true;

    bool 검은색임 => isBlack;


    [SerializeField] 가이드_돌_코드 guideStoneScript;

    private readonly 육목_규칙 규칙 = new 육목_규칙();
    private bool 증강사용중;

    public int 획득한증강수 => 규칙.획득한_증강_수;

    void Start()
    {
        isBlack = true;
        규칙.증강_획득 += 증강획득알림;
        guideStoneScript.돌놓기완료 += 턴종료;
        이제너의턴();
    }

    void OnDestroy()
    {
        규칙.증강_획득 -= 증강획득알림;
        if (guideStoneScript != null) guideStoneScript.돌놓기완료 -= 턴종료;
    }
    public void 이제너의턴()
    {
        //동기화
        guideStoneScript.centerPos = centerPos;
        guideStoneScript.gap = gap;
        guideStoneScript.isBlack = isBlack;
        guideStoneScript.증강사용중 = 증강사용중;
        guideStoneScript.규칙 = 규칙;
        
        
        guideStoneScript.격자로놓기시작();
        
    }

    /// <summary>돌을 두기 전, 현재 턴에 증강을 쓸지 지정한다.</summary>
    public void 증강사용설정(bool 사용)
    {
        증강사용중 = 사용;
        guideStoneScript.증강사용중 = 사용;
    }

    private void 증강획득알림(int 보유수)
    {
        Debug.Log($"증강 획득! 현재 획득 수: {보유수}");
    }

    private void 턴종료()
    {
        isBlack = !isBlack;
        증강사용중 = false;
        StartCoroutine(다음턴시작());
    }

    private IEnumerator 다음턴시작()
    {
        yield return null;
        이제너의턴();
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
