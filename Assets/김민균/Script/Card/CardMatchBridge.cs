using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct CardMoveRecord
{
    public int Sequence;
    public CardManager.PlayerSide Player;
    public Vector2Int BoardPosition;
}

/// <summary>
/// 기존 대국 코드를 수정하지 않고 카드 시스템과 연결한다.
/// 플레이어와 착수 데이터를 명시적으로 전달해 추후 네트워크 권한 계층에서도 재사용할 수 있다.
/// </summary>
public sealed class CardMatchBridge : MonoBehaviour
{
    [SerializeField] private CardManager cardManager;
    [SerializeField] private 수_놓기 turnController;
    [SerializeField] private 가이드_돌_코드 guideStone;
    [Min(0.02f)] [SerializeField] private float stoneScanInterval = 0.1f;

    private readonly List<CardMoveRecord> moveHistory = new List<CardMoveRecord>();
    private readonly HashSet<SpriteRenderer> observedStones = new HashSet<SpriteRenderer>();
    private readonly HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>();
    private CardManager.PlayerSide currentTurn = CardManager.PlayerSide.Black;
    private bool subscribed;
    private bool gameEnded;

    public IReadOnlyList<CardMoveRecord> MoveHistory => moveHistory;
    public CardManager.PlayerSide CurrentTurn => currentTurn;
    public bool GameEnded => gameEnded;
    public bool IsFinalDraw { get; private set; }
    public CardManager.PlayerSide? FinalWinner { get; private set; }

    public event Action<CardMoveRecord> MoveRecorded;
    public event Action<CardManager.PlayerSide?, int, int> FinalResultResolved;

    private void Awake()
    {
        if (cardManager == null) cardManager = FindAnyObjectByType<CardManager>();
        if (turnController == null) turnController = FindAnyObjectByType<수_놓기>();
        if (guideStone == null) guideStone = FindAnyObjectByType<가이드_돌_코드>();
    }

    private void Start()
    {
        cardManager?.BeginTurn(currentTurn);
        StartCoroutine(SubscribeAfterExistingControllers());
        StartCoroutine(TrackPlacedStones());
    }

    private IEnumerator SubscribeAfterExistingControllers()
    {
        // 기존 턴 컨트롤러가 먼저 돌놓기완료를 처리하도록 한 프레임 뒤에 연결한다.
        yield return null;
        if (guideStone != null) guideStone.돌놓기완료 += OnNormalStonePlaced;
        if (cardManager != null) cardManager.AcquisitionCompleted += OnAcquisitionCompleted;
        subscribed = true;
        if (cardManager != null && cardManager.TryStartInitialAcquisition())
            PauseForAcquisition();
    }

    private void OnDestroy()
    {
        if (!subscribed) return;
        if (guideStone != null) guideStone.돌놓기완료 -= OnNormalStonePlaced;
        if (cardManager != null) cardManager.AcquisitionCompleted -= OnAcquisitionCompleted;
    }

    private void OnNormalStonePlaced()
    {
        if (gameEnded) return;
        DiscoverNewStones();
        if (gameEnded) return;
        if (turnController != null && turnController.승자 != 돌_색.없음)
        {
            gameEnded = true;
            SetGuideVisible(false);
            return;
        }
        currentTurn = currentTurn == CardManager.PlayerSide.Black
            ? CardManager.PlayerSide.White
            : CardManager.PlayerSide.Black;
        cardManager?.BeginTurn(currentTurn);

        if (cardManager != null && cardManager.NotifyStonePlaced())
            PauseForAcquisition();
    }

    private void PauseForAcquisition()
    {
        // 기존 컨트롤러가 예약한 다음 턴 코루틴을 취소하고 카드 선택이 끝날 때 재개한다.
        turnController?.StopAllCoroutines();
        guideStone?.StopAllCoroutines();
        SetGuideVisible(false);
    }

    private void OnAcquisitionCompleted()
    {
        if (!gameEnded) StartCoroutine(ResumeTurnNextFrame());
    }

    private IEnumerator ResumeTurnNextFrame()
    {
        yield return null;
        cardManager?.BeginTurn(currentTurn);
        turnController?.이제너의턴();
    }

    private IEnumerator TrackPlacedStones()
    {
        var wait = new WaitForSeconds(stoneScanInterval);
        while (!gameEnded)
        {
            DiscoverNewStones();
            yield return wait;
        }
    }

    private void DiscoverNewStones()
    {
        if (guideStone == null || guideStone.돌 == null || Mathf.Approximately(guideStone.gap, 0f)) return;
        string cloneName = guideStone.돌.name + "(Clone)";
        SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude);
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null || renderer.gameObject.name != cloneName) continue;
            if (!observedStones.Add(renderer)) continue;

            Vector3 position = renderer.transform.position;
            var boardPosition = new Vector2Int(
                Mathf.RoundToInt((position.x - guideStone.centerPos.x) / guideStone.gap) + 9,
                9 - Mathf.RoundToInt((position.y - guideStone.centerPos.y) / guideStone.gap));
            if (boardPosition.x < 0 || boardPosition.x >= 19 || boardPosition.y < 0 || boardPosition.y >= 19)
                continue;

            CardManager.PlayerSide player = renderer.sprite == guideStone.검은돌이미지
                ? CardManager.PlayerSide.Black
                : CardManager.PlayerSide.White;
            occupiedPositions.Add(boardPosition);
            var record = new CardMoveRecord
            {
                Sequence = moveHistory.Count + 1,
                Player = player,
                BoardPosition = boardPosition
            };
            moveHistory.Add(record);
            MoveRecorded?.Invoke(record);
        }

        if (occupiedPositions.Count == 19 * 19 && turnController != null && turnController.승자 == 돌_색.없음)
            ResolveDrawByCardGrades();
    }

    /// <summary>보드 무승부가 확정됐을 때 호출한다. 카드 등급 합이 높은 플레이어가 패배한다.</summary>
    public void ResolveDrawByCardGrades()
    {
        if (gameEnded) return;
        gameEnded = true;
        turnController?.StopAllCoroutines();
        guideStone?.StopAllCoroutines();
        SetGuideVisible(false);

        int blackTotal = cardManager == null ? 0 : cardManager.GetCardGradeTotal(CardManager.PlayerSide.Black);
        int whiteTotal = cardManager == null ? 0 : cardManager.GetCardGradeTotal(CardManager.PlayerSide.White);
        if (blackTotal > whiteTotal) FinalWinner = CardManager.PlayerSide.White;
        else if (whiteTotal > blackTotal) FinalWinner = CardManager.PlayerSide.Black;
        else
        {
            FinalWinner = null;
            IsFinalDraw = true;
        }

        string result = FinalWinner.HasValue ? $"{FinalWinner.Value} 승리" : "최종 무승부";
        Debug.Log($"보드 무승부 카드 등급 판정: 흑 {blackTotal}, 백 {whiteTotal}, {result}", this);
        FinalResultResolved?.Invoke(FinalWinner, blackTotal, whiteTotal);
    }

    private void SetGuideVisible(bool visible)
    {
        if (guideStone == null) return;
        SpriteRenderer renderer = guideStone.GetComponent<SpriteRenderer>();
        if (renderer != null) renderer.enabled = visible;
    }
}
