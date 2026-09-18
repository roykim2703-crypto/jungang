using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class 가이드_돌_코드 : MonoBehaviour
{
    [HideInInspector] public Vector3 centerPos;
    [HideInInspector] public float gap = 1f;
    [HideInInspector] public bool isToggle = true; // 토글 또는 홀드
    [HideInInspector] public bool isBlack = true;

    [Header("가이드용 이미지")]
    public Sprite 검은돌이미지;
    public Sprite 흰돌이미지;
    [Header("프리팹")]
    public GameObject 돌;

    private bool isPlacing = false;
    private Coroutine placeRoutine;

    SpriteRenderer spriteRenderer;

    Vector2 cellPos;

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
        spriteRenderer.color = new Color(255, 255, 255, 0.5f);


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
    }

    private void 돌한개놓기()
    {
        // 격자 인덱스 계산 (중심 10,10 / 좌상단 1,1 / 우하단 19,19)
        float xOffset = Mathf.Round((transform.position.x - centerPos.x) / gap);
        float yOffset = Mathf.Round((transform.position.y - centerPos.y) / gap);

        cellPos.x = xOffset + 10;
        cellPos.y = 10 - yOffset; // y는 위로 갈수록 값이 커지므로 row는 반대로

        Debug.Log(cellPos + "에 " + (isBlack ? "검은" : "흰") + " 돌 놓음 (cellPos: " + cellPos + ")");
        GameObject stone = Instantiate(돌, transform.position, transform.rotation);
        SpriteRenderer stoneSR = stone.GetComponent<SpriteRenderer>();
        if (isBlack) stoneSR.sprite = 검은돌이미지;
        else stoneSR.sprite = 흰돌이미지;

        //할거 끝나고 모양 숨기기
        spriteRenderer.enabled = false;
    }
}