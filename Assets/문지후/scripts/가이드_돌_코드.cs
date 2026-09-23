using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class 가이드_돌_코드 : MonoBehaviour
{
    [HideInInspector] public Vector3 centerPos;
    [HideInInspector] public float gap = 1f;
    [HideInInspector] public bool isToggle = true; // 토글 또는 홀드
    [HideInInspector] public bool isBlack = true;
    [HideInInspector] public bool 증강사용중;
    [HideInInspector] public 육목_규칙 규칙;

    [Header("가이드용 이미지")]
    public Sprite 검은돌이미지;
    public Sprite 흰돌이미지;
    public Sprite 금지이미지;
    [Header("프리팹")]
    public GameObject 돌;

    private bool isPlacing = false;
    private Coroutine placeRoutine;

    SpriteRenderer spriteRenderer;

    Vector2 cellPos;
    private 착수_금지_사유 현재금지사유;

    public event Action 돌놓기완료;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    void Start()
    {
        //실험용
        //격자로놓기시작();
    }
    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (isToggle && isPlacing) isPlacing = false;
        }
    }

    public void 격자로놓기시작()
    {
        spriteRenderer.enabled = true;

        isPlacing = true;
        //가이드 돌 이미지 정하기
        if (isBlack) spriteRenderer.sprite = 검은돌이미지;
        else spriteRenderer.sprite = 흰돌이미지;
        //반투명하게
        spriteRenderer.color = new Color(1f, 1f, 1f, 0.5f);


        if (isToggle)
        {

            if (isPlacing)
                placeRoutine = StartCoroutine(격자로놓기());
            else if (placeRoutine != null)
                StopCoroutine(placeRoutine);
        }
        else
        {
            // 홀드 모드: 누르고 있는 동안만 실행
            placeRoutine = StartCoroutine(홀드로놓기());
        }
    }

    private IEnumerator 격자로놓기()
    {
        while (isPlacing)
        {
            돌놓일위치_가이드();
            yield return new WaitForSeconds(0.05f);
        }
        돌한개놓기();
    }

    private IEnumerator 홀드로놓기()
    {
        while (Mouse.current.leftButton.isPressed)
        {
            돌놓일위치_가이드();
            yield return new WaitForSeconds(0.05f);
        }
        돌한개놓기();
    }

    private void 돌놓일위치_가이드()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        // centerPos 기준으로 gap 간격에 스냅
        float x = Mathf.Round((mouseWorldPos.x - centerPos.x) / gap) * gap + centerPos.x;
        float y = Mathf.Round((mouseWorldPos.y - centerPos.y) / gap) * gap + centerPos.y;

        transform.position = new Vector3(x, y, 0f);

        Vector2Int 보드좌표 = 보드좌표계산();
        현재금지사유 = 규칙 == null
            ? 착수_금지_사유.없음
            : 규칙.착수검사(보드좌표, 현재돌색(), 증강사용중);
        spriteRenderer.sprite = 현재금지사유 == 착수_금지_사유.없음
            ? (isBlack ? 검은돌이미지 : 흰돌이미지)
            : 금지이미지;
    }

    private void 돌한개놓기()
    {
        // 격자 인덱스 계산 (중심 10,10 / 좌상단 1,1 / 우하단 19,19)
        float xOffset = Mathf.Round((transform.position.x - centerPos.x) / gap);
        float yOffset = Mathf.Round((transform.position.y - centerPos.y) / gap);

        cellPos.x = xOffset + 10;
        cellPos.y = 10 - yOffset; // 표시용 좌표는 1~19

        Vector2Int 보드좌표 = 보드좌표계산();
        if (규칙 != null && !규칙.돌놓기(보드좌표, 현재돌색(), 증강사용중, false, out 현재금지사유))
        {
            Debug.Log($"{cellPos}에는 놓을 수 없습니다: {현재금지사유}");
            // 금지 수를 클릭해도 턴을 소비하지 않고 같은 색으로 다시 고른다.
            isPlacing = true;
            placeRoutine = StartCoroutine(격자로놓기());
            return;
        }

        Debug.Log(cellPos + "에 " + (isBlack ? "검은" : "흰") + " 돌 놓음 (cellPos: " + cellPos + ")");
        GameObject stone = Instantiate(돌, transform.position, transform.rotation);
        SpriteRenderer stoneSR = stone.GetComponent<SpriteRenderer>();
        if (isBlack) stoneSR.sprite = 검은돌이미지;
        else stoneSR.sprite = 흰돌이미지;

        //할거 끝나고 모양 숨기기
        spriteRenderer.enabled = false;
        돌놓기완료?.Invoke();
    }

    /// <summary>증강 효과가 돌을 직접 만들 때 사용한다. 금수 조합은 허용하지만 중복/보드 밖은 막는다.</summary>
    public bool 증강효과로돌놓기(Vector2Int 보드좌표)
    {
        if (규칙 == null)
        {
            Debug.LogError("육목 규칙이 연결되지 않아 증강 돌을 만들 수 없습니다.");
            return false;
        }

        if (!규칙.돌놓기(보드좌표, 현재돌색(), true, true, out 착수_금지_사유 사유))
        {
            Debug.Log($"증강 돌 생성 실패: {사유}");
            return false;
        }

        Vector3 위치 = new Vector3(
            centerPos.x + (보드좌표.x - 9) * gap,
            centerPos.y - (보드좌표.y - 9) * gap,
            0f);
        GameObject stone = Instantiate(돌, 위치, transform.rotation);
        stone.GetComponent<SpriteRenderer>().sprite = isBlack ? 검은돌이미지 : 흰돌이미지;
        return true;
    }

    private Vector2Int 보드좌표계산()
    {
        int x = Mathf.RoundToInt((transform.position.x - centerPos.x) / gap) + 9;
        int y = 9 - Mathf.RoundToInt((transform.position.y - centerPos.y) / gap);
        return new Vector2Int(x, y);
    }

    private 돌_색 현재돌색()
    {
        return isBlack ? 돌_색.검정 : 돌_색.흰색;
    }
}
