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
    private Transform starPoints;

    public int 획득한증강수 => 규칙.획득한_증강_수;
    public 돌_색 승자 => 규칙.승자;
    public 돌_색 패자 => 규칙.패자;

    void Start()
    {
        isBlack = true;
        규칙.증강_획득 += 증강획득알림;
        규칙.게임_종료 += 게임종료알림;
        guideStoneScript.돌놓기완료 += 턴종료;
        이제너의턴();
    }

    void OnDestroy()
    {
        규칙.증강_획득 -= 증강획득알림;
        규칙.게임_종료 -= 게임종료알림;
        if (guideStoneScript != null) guideStoneScript.돌놓기완료 -= 턴종료;
    }
    public void 이제너의턴()
    {
        if (규칙.승자 != 돌_색.없음) return;
        //동기화
        guideStoneScript.centerPos = centerPos;
        guideStoneScript.gap = gap;
        EnsureStarPoints();
        guideStoneScript.isBlack = isBlack;
        guideStoneScript.증강사용중 = 증강사용중;
        guideStoneScript.규칙 = 규칙;
        
        
        guideStoneScript.격자로놓기시작();
        
    }

    private void EnsureStarPoints()
    {
        if (starPoints != null) return;
        var sprite = guideStoneScript.검은돌이미지;
        if (sprite == null) return;

        starPoints = new GameObject("BoardStarPoints").transform;
        starPoints.position = centerPos;
        starPoints.SetParent(transform, true);
        var stoneRenderer = guideStoneScript.GetComponent<SpriteRenderer>();
        // Standard 19x19 hoshi: fourth, tenth and sixteenth intersections.
        for (int y = -6; y <= 6; y += 6)
        for (int x = -6; x <= 6; x += 6)
        {
            var point = new GameObject($"StarPoint_{x + 10}_{10 - y}");
            point.transform.SetParent(starPoints, false);
            point.transform.localPosition = new Vector3(x * gap, y * gap, 0f);
            point.transform.localScale = new Vector3(
                gap * .30f / sprite.bounds.size.x,
                gap * .30f / sprite.bounds.size.y, 1f);
            var renderer = point.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(.12f, .09f, .06f, 1f);
            renderer.sortingLayerID = stoneRenderer.sortingLayerID;
            renderer.sortingOrder = stoneRenderer.sortingOrder - 1;
        }
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
        if (규칙.승자 != 돌_색.없음) return;
        isBlack = !isBlack;
        증강사용중 = false;
        StartCoroutine(다음턴시작());
    }

    private void 게임종료알림(돌_색 이긴색)
    {
        Debug.Log($"게임 종료: {이긴색} 승리, {규칙.패자} 패배");
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
