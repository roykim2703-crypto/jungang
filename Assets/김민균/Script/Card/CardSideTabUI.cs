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
        public TMP_Text Description;
        public TMP_Text Value;
        public Image Artwork;
    }

    [SerializeField] private CardManager cardManager;
    [SerializeField] private CardMatchBridge matchBridge;
    [Header("기보")]
    [SerializeField] private Button seedButton;
    [SerializeField] private TMP_Text seedLabel;
    [SerializeField] private TMP_Text moveHistoryLabel;
    [SerializeField] private ScrollRect moveHistoryScrollRect;
    [Header("다음 증강 획득 진행도")]
    [SerializeField] private Scrollbar rewardProgressBar;
    [SerializeField] private TMP_Text rewardProgressLabel;
    [Header("사이드 탭 카드 버튼")]
    [SerializeField] private CardButtonSlot[] blackSlots = new CardButtonSlot[4];
    [SerializeField] private CardButtonSlot[] whiteSlots = new CardButtonSlot[4];

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
            seedButton.gameObject.SetActive(false);
        }
        if(seedLabel!=null)seedLabel.gameObject.SetActive(false);
        if(moveHistoryScrollRect!=null)
        {
            RectTransform viewport=moveHistoryScrollRect.GetComponent<RectTransform>();
            if(viewport!=null)viewport.offsetMax=new Vector2(viewport.offsetMax.x,-16f);
        }
        if(moveHistoryLabel!=null)
        {
            moveHistoryLabel.raycastTarget=true;
            moveHistoryLabel.color=new Color(.2f,.15f,.1f);moveHistoryLabel.margin=new Vector4(3,3,3,3);
            var click=moveHistoryLabel.GetComponent<MoveHistoryLineClick>()??moveHistoryLabel.gameObject.AddComponent<MoveHistoryLineClick>();click.Bind(moveHistoryLabel);
        }
        ConfigureRewardGauge();
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
        RefreshRewardProgress();
        RefreshMoveHistory();
        RefreshCards();
    }

    private void RefreshRewardProgress()
    {
        if (cardManager == null) return;
        if (rewardProgressBar != null)
        {
            rewardProgressBar.SetValueWithoutNotify(0f);
            rewardProgressBar.size=cardManager.RewardProgress01;
            rewardProgressBar.interactable = false;
        }
        if (rewardProgressLabel != null)
        {
            rewardProgressLabel.text="";rewardProgressLabel.gameObject.SetActive(false);
        }
    }
    private void ConfigureRewardGauge()
    {
        if(rewardProgressBar==null||rewardProgressBar.handleRect==null)return;
        rewardProgressBar.transition=Selectable.Transition.None;
        rewardProgressBar.direction=Scrollbar.Direction.LeftToRight;
        var handle=rewardProgressBar.handleRect;handle.sizeDelta=Vector2.zero;
        var fill=handle.GetComponent<Image>();if(fill!=null)fill.canvasRenderer.SetColor(Color.white);
        var track=handle.parent.Find("RewardGaugeTrack") as RectTransform;
        if(track==null)
        {
            track=new GameObject("RewardGaugeTrack",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).GetComponent<RectTransform>();
            track.SetParent(handle.parent,false);track.SetAsFirstSibling();
        }
        track.anchorMin=Vector2.zero;track.anchorMax=Vector2.one;track.offsetMin=track.offsetMax=Vector2.zero;
        var image=track.GetComponent<Image>();image.color=new Color(.48f,.48f,.48f);image.raycastTarget=false;
    }

    private void OnMoveRecorded(CardMoveRecord record)
    {
        RefreshMoveHistory();
    }

    private void RefreshMoveHistory()
    {
        if(moveHistoryLabel!=null&&AugmentMatch.Instance!=null&&AugmentMatch.Instance.IsReviewMode)
        {
            var reviewLines=new List<string>();
            for(int i=0;i<MatchReviewSession.Frames.Count;i++)
            {
                string line=MatchReviewSession.Frames[i].label;
                reviewLines.Add(i==AugmentMatch.Instance.ReviewIndex?$"<color=#F4B52C>▶ {line}</color>":$"   {line}");
            }
            moveHistoryLabel.text=reviewLines.Count==0?"기보":string.Join("\n",reviewLines);ResizeMoveHistoryAndShowLatest(false);return;
        }
        if (moveHistoryLabel != null && AugmentMatch.Instance != null)
        {
            string content=AugmentMatch.Instance.History.Count == 0 ? "기보" : string.Join("\n", AugmentMatch.Instance.History);
            bool changed=moveHistoryLabel.text!=content;
            moveHistoryLabel.text=content;
            ResizeMoveHistoryAndShowLatest(changed);
            return;
        }
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

    private void ResizeMoveHistoryAndShowLatest(bool showLatest=true)
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
        if(showLatest)moveHistoryScrollRect.verticalNormalizedPosition = 0f;
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
            if (slot == null || slot.Root == null) continue;
            // Repair existing scenes created before description/grade references were saved.
            foreach (TMP_Text text in slot.Root.GetComponentsInChildren<TMP_Text>(true))
            {
                text.margin = Vector4.zero;
                if (text.name == "설명") slot.Description = text;
                if (text.name == "등급") slot.Value = text;
                if (text.name == "이름") slot.Title = text;
            }
            bool hasCard = i < visibleCards.Length;
            if (slot.Root.activeSelf != hasCard) slot.Root.SetActive(hasCard);
            if (!hasCard) continue;

            CardManager.CardData card = visibleCards[i];
            CardDetails details = slot.Root.GetComponent<CardDetails>();
            if (details == null) details = slot.Root.AddComponent<CardDetails>();
            int cardId = card.Name;
            string title = string.IsNullOrWhiteSpace(card.DisplayName) ? $"카드 {card.Name}" : card.DisplayName;
            var match=AugmentMatch.Instance;
            bool hideUsage=AugmentVisibility.IsSecret(cardId)&&match!=null&&!match.IsReviewMode&&match.LocalSide!=0&&match.LocalSide!=(black?1:2);
            bool showUsed=card.Used&&!hideUsage;
            slot.Title.text = showUsed ? $"{title} (사용됨)" : title;
            if (slot.Description != null) { slot.Description.gameObject.SetActive(true); slot.Description.text = card.explanation; }
            if (slot.Value != null) { slot.Value.gameObject.SetActive(true); slot.Value.text = $"등급 {card.Value:0.#}"; }
            slot.Title.gameObject.SetActive(true);
            slot.Title.fontSizeMax = 9f;
            slot.Title.fontSizeMin = 6f;
            if (slot.Description != null) { slot.Description.fontSizeMax = 6f; slot.Description.fontSizeMin = 4.5f; }
            if (slot.Value != null) { slot.Value.fontSizeMax = 8f; slot.Value.fontSizeMin = 6f; }
            slot.Artwork.sprite = card.ItemImage;
            slot.Artwork.enabled = card.ItemImage != null;
            slot.Button.interactable = card.Have && !card.Used && cardManager.CanUseCard(black)
                && (AugmentMatch.Instance == null || AugmentMatch.Instance.CanControl(black ? 1 : 2));
            var face = slot.Button.targetGraphic as Image;
            details.Bind(slot.Title, card.explanation, $"등급 {card.Value:0.#}" + (showUsed ? " · 사용됨" : ""),
                card.ItemImage, face == null ? null : face.sprite);
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
        if (AugmentMatch.Instance != null) { AugmentMatch.Instance.ChooseAbility(black, cardId); return; }
        if (cardManager == null || !cardManager.UseCard(cardId, black))
        {
            Debug.LogWarning($"{(black ? "흑" : "백")}은 지금 카드를 사용할 수 없습니다.", this);
            return;
        }
        CardButtonClicked?.Invoke(black, cardId);
        Debug.Log($"{(black ? "흑" : "백")} 카드 사용: {cardId}", this);
    }

}
