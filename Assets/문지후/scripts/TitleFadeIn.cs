using System.Collections;
using UnityEngine;

/// <summary>UI의 배치 위치를 기억하고 아래에서 올라오며 페이드인합니다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
[AddComponentMenu("UI/Title Fade In")]
public class TitleFadeIn : MonoBehaviour
{
    [Header("재생")]
    [Tooltip("활성화될 때마다 자동 재생합니다.")]
    [SerializeField] private bool playOnEnable = true;
    [Tooltip("Time.timeScale이 0이어도 재생합니다.")]
    [SerializeField] private bool useUnscaledTime = true;
    [Min(0f), Tooltip("투명한 상태로 기다릴 시간(초)")]
    [SerializeField] private float startDelay = 0.3f;
    [Min(0f), Tooltip("이동과 페이드에 걸리는 시간(초). 0이면 즉시 완료합니다.")]
    [SerializeField] private float duration = 0.8f;

    [Header("이동")]
    [Min(0f), Tooltip("원래 위치보다 아래에서 시작할 거리(UI 단위)")]
    [SerializeField] private float moveDistance = 60f;
    [Tooltip("가로축: 진행 시간(0~1), 세로축: 이동 진행률. 1을 넘으면 오버슈트합니다.")]
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("페이드")]
    [Range(0f, 1f)]
    [SerializeField] private float startAlpha = 0f;
    [Tooltip("가로축: 진행 시간(0~1), 세로축: 페이드 진행률")]
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("대기 및 등장 중에는 버튼 입력과 레이캐스트를 막습니다.")]
    [SerializeField] private bool blockInputDuringAnimation = true;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector3 originalPosition;
    private float originalAlpha;
    private bool originalInteractable;
    private bool originalBlocksRaycasts;
    private bool initialized;
    private Coroutine animationRoutine;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        if (playOnEnable)
            Play();
    }

    private void Initialize()
    {
        if (initialized)
            return;

        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        // 레이아웃 그룹이 계산한 최종 배치를 기준으로 위치를 기억합니다.
        Canvas.ForceUpdateCanvases();
        originalPosition = rectTransform.anchoredPosition3D;
        originalAlpha = canvasGroup.alpha;
        originalInteractable = canvasGroup.interactable;
        originalBlocksRaycasts = canvasGroup.blocksRaycasts;
        initialized = true;
    }

    /// <summary>처음 기억한 위치를 기준으로 처음부터 다시 재생합니다.</summary>
    public void Play()
    {
        if (!isActiveAndEnabled)
            return;

        Initialize();
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine = StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        Vector3 startPosition = originalPosition + Vector3.down * moveDistance;
        rectTransform.anchoredPosition3D = startPosition;
        canvasGroup.alpha = startAlpha;
        if (blockInputDuringAnimation)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        float elapsed = 0f;
        while (elapsed < startDelay)
        {
            yield return null;
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        }

        elapsed = 0f;
        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            float movement = movementCurve == null ? progress : movementCurve.Evaluate(progress);
            float fade = fadeCurve == null ? progress : fadeCurve.Evaluate(progress);
            rectTransform.anchoredPosition3D = Vector3.LerpUnclamped(startPosition, originalPosition, movement);
            canvasGroup.alpha = Mathf.Clamp01(Mathf.LerpUnclamped(startAlpha, originalAlpha, fade));
            yield return null;
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        }

        Restore();
        animationRoutine = null;
    }

    private void OnDisable()
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);
        animationRoutine = null;
        if (initialized)
            Restore();
    }

    private void Restore()
    {
        rectTransform.anchoredPosition3D = originalPosition;
        canvasGroup.alpha = originalAlpha;
        canvasGroup.interactable = originalInteractable;
        canvasGroup.blocksRaycasts = originalBlocksRaycasts;
    }
}
