using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardSideTabUI : MonoBehaviour
{
    [Serializable]
    public class CardButtonSlot
    {
        public GameObject Root;
        public Button Button;
        public TMP_Text Title;
        public Image Artwork;
    }

    [SerializeField] private CardManager cardManager;
    [SerializeField] private CardMatchBridge matchBridge;
    [Header("기보 Seed 버튼")]
    [SerializeField] private Button seedButton;
    [SerializeField] private TMP_Text seedLabel;
    [SerializeField] private TMP_Text moveHistoryLabel;
    [SerializeField] private ScrollRect moveHistoryScrollRect;
    [Header("다음 증강 획득 진행도")]
    [SerializeField] private Scrollbar rewardProgressBar;
    [SerializeField] private TMP_Text rewardProgressLabel;
    [Header("사이드 탭 카드 버튼")]
    [SerializeField] private CardButtonSlot[] blackSlots = new CardButtonSlot[3];
    [SerializeField] private CardButtonSlot[] whiteSlots = new CardButtonSlot[3];

    public event Action<bool, int> CardButtonClicked;
    public int DisplayedSeed => cardManager == null ? 0 : cardManager.CurrentSeed;
    private bool subscribed;
    private bool matchSubscribed;

    private void Awake()
    {
        FindManager();
    }

    private void OnEnable()
    {
        FindManager();
        FindMatchBridge();
        Subscribe();
    }

    private void Start()
    {
        FindManager();
        FindMatchBridge();
        Subscribe();
        if (seedButton != null)
        {
            seedButton.onClick.RemoveAllListeners();
            seedButton.onClick.AddListener(CopySeed);
        }
        RefreshAll();
    }

    private void OnDisable()
    {
        if (subscribed && cardManager != null)
        {
            cardManager.CardAcquired -= OnCardAcquired;
            cardManager.CardAvailabilityChanged -= RefreshCards;
            cardManager.RewardProgressChanged -= RefreshRewardProgress;
            subscribed = false;
        }
        if (matchSubscribed && matchBridge != null)
        {
            matchBridge.MoveRecorded -= OnMoveRecorded;
            matchSubscribed = false;
        }
    }

    private void FindManager()
    {
        if (cardManager == null) cardManager = FindAnyObjectByType<CardManager>();
    }

    private void FindMatchBridge()
    {
        if (matchBridge == null) matchBridge = FindAnyObjectByType<CardMatchBridge>();
    }

    private void Subscribe()
    {
        if (subscribed || cardManager == null) return;
        cardManager.CardAcquired += OnCardAcquired;
        cardManager.CardAvailabilityChanged += RefreshCards;
        cardManager.RewardProgressChanged += RefreshRewardProgress;
        subscribed = true;

        if (!matchSubscribed && matchBridge != null)
        {
            matchBridge.MoveRecorded += OnMoveRecorded;
            matchSubscribed = true;
        }
    }

    private void OnCardAcquired(bool black, CardManager.CardData card)
    {
        RefreshCards();
    }

    public void RefreshAll()
    {
        if (cardManager == null) return;
        if (seedLabel != null) seedLabel.text = $"SEED {cardManager.CurrentSeed}";
        RefreshRewardProgress();
        RefreshMoveHistory();
        RefreshCards();
    }

    private void RefreshRewardProgress()
    {
        if (cardManager == null) return;
        if (rewardProgressBar != null)
        {
            rewardProgressBar.SetValueWithoutNotify(cardManager.RewardProgress01);
            rewardProgressBar.interactable = false;
        }
        if (rewardProgressLabel != null)
        {
            if (!cardManager.HasMoreRewards) rewardProgressLabel.text = "증강 획득 완료";
            else if (cardManager.RewardRoundCount == 0) rewardProgressLabel.text = "게임 시작 증강 획득";
            else rewardProgressLabel.text = $"다음 증강까지 {cardManager.StonesUntilNextReward}수";
        }
    }

    private void OnMoveRecorded(CardMoveRecord record)
    {
        RefreshMoveHistory();
    }

    private void RefreshMoveHistory()
    {
        if (moveHistoryLabel == null || matchBridge == null) return;
        IReadOnlyList<CardMoveRecord> history = matchBridge.MoveHistory;
        var lines = new List<string>();
        for (int i = 0; i < history.Count; i++)
        {
            CardMoveRecord move = history[i];
            string color = move.Player == CardManager.PlayerSide.Black ? "흑" : "백";
            lines.Add($"{move.Sequence}. {color} ({move.BoardPosition.x + 1},{move.BoardPosition.y + 1})");
        }
        moveHistoryLabel.text = lines.Count == 0 ? "기보" : string.Join("\n", lines);
        ResizeMoveHistoryAndShowLatest();
    }

    private void ResizeMoveHistoryAndShowLatest()
    {
        if (moveHistoryLabel == null || moveHistoryScrollRect == null) return;
        moveHistoryLabel.ForceMeshUpdate();
        RectTransform textRect = moveHistoryLabel.rectTransform;
        float viewportHeight = moveHistoryScrollRect.viewport == null
            ? 0f
            : moveHistoryScrollRect.viewport.rect.height;
        float contentHeight = Mathf.Max(viewportHeight, moveHistoryLabel.preferredHeight + 8f);
        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
        Canvas.ForceUpdateCanvases();
        moveHistoryScrollRect.verticalNormalizedPosition = 0f;
    }

    public void RefreshCards()
    {
        if (cardManager == null) return;
        BindInventory(cardManager.BlackInventory, blackSlots, true);
        BindInventory(cardManager.WhiteInventory, whiteSlots, false);
    }

    private void BindInventory(IReadOnlyList<CardManager.CardData> inventory, CardButtonSlot[] slots, bool black)
    {
        CardManager.CardData[] visibleCards = inventory.TakeLast(slots.Length).ToArray();

        for (int i = 0; i < slots.Length; i++)
        {
            CardButtonSlot slot = slots[i];
            bool hasCard = i < visibleCards.Length;
            if (slot.Root.activeSelf != hasCard) slot.Root.SetActive(hasCard);
            if (!hasCard) continue;

            CardManager.CardData card = visibleCards[i];
            int cardId = card.Name;
            string title = string.IsNullOrWhiteSpace(card.DisplayName) ? $"카드 {card.Name}" : card.DisplayName;
            slot.Title.text = card.Used ? $"{title} (사용됨)" : title;
            slot.Artwork.sprite = card.ItemImage;
            slot.Artwork.enabled = card.ItemImage != null;
            slot.Button.interactable = card.Have && !card.Used && cardManager.CanUseCard(black);
            ColorBlock colors = slot.Button.colors;
            colors.fadeDuration = 0f;
            slot.Button.colors = colors;
            slot.Button.transition = Selectable.Transition.None;
            slot.Button.onClick.RemoveAllListeners();
            slot.Button.onClick.AddListener(() => OnCardButtonClicked(black, cardId));
        }
    }

    private void OnCardButtonClicked(bool black, int cardId)
    {
        if (cardManager == null || !cardManager.UseCard(cardId, black))
        {
            Debug.LogWarning($"{(black ? "흑" : "백")}은 지금 카드를 사용할 수 없습니다.", this);
            return;
        }
        CardButtonClicked?.Invoke(black, cardId);
        Debug.Log($"{(black ? "흑" : "백")} 카드 사용: {cardId}", this);
    }

    private void CopySeed()
    {
        if (cardManager == null) return;
        GUIUtility.systemCopyBuffer = cardManager.CurrentSeed.ToString();
        Debug.Log($"Seed {cardManager.CurrentSeed}를 클립보드에 복사했습니다.", this);
    }
}
