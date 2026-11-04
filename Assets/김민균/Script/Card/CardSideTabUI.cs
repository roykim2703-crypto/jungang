using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
    private readonly List<int> historyStoneColors=new List<int>();
    private readonly List<Image> historyStoneImages=new List<Image>();
    private static readonly Regex HistorySide=new Regex(@"^(\d+\.\s+)?(회색|흑|백)(?=\s|\(|$)");

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
        historyStoneColors.Clear();
        if(moveHistoryLabel!=null&&AugmentMatch.Instance!=null&&AugmentMatch.Instance.IsReviewMode)
        {
            var reviewLines=new List<string>();
            for(int i=0;i<MatchReviewSession.Frames.Count;i++)
            {
                string line=FormatHistoryLine(MatchReviewSession.Frames[i].label);
                reviewLines.Add(i==AugmentMatch.Instance.ReviewIndex?$"<color=#F4B52C>▶ {line}</color>":$"   {line}");
            }
            moveHistoryLabel.text=reviewLines.Count==0?"기보":string.Join("\n",reviewLines);ResizeMoveHistoryAndShowLatest(false);return;
        }
        if (moveHistoryLabel != null && AugmentMatch.Instance != null)
        {
            string content=AugmentMatch.Instance.History.Count == 0 ? "기보" : string.Join("\n", AugmentMatch.Instance.History.Select(FormatHistoryLine));
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
            lines.Add(FormatHistoryLine($"{move.Sequence}. {color} ({move.BoardPosition.x + 1},{move.BoardPosition.y + 1})"));
        }
        moveHistoryLabel.text = lines.Count == 0 ? "기보" : string.Join("\n", lines);
        ResizeMoveHistoryAndShowLatest();
    }
    private string FormatHistoryLine(string line)
    {
        return HistorySide.Replace(line??"",match=>
        {
            historyStoneColors.Add(match.Groups[2].Value=="흑"?1:match.Groups[2].Value=="백"?2:4);
            // Reserve a full icon slot while retaining TMP's original line/click geometry.
            return match.Groups[1].Value+"<mspace=1.4em><color=#00000000>O</color></mspace>";
        });
    }
    private void RefreshHistoryStoneImages()
    {
        var guide=FindAnyObjectByType<가이드_돌_코드>();
        int icon=0;
        if(guide!=null)
        {
            var info=moveHistoryLabel.textInfo;
            for(int i=0;i<info.characterCount&&icon<historyStoneColors.Count;i++)
            {
                var character=info.characterInfo[i];
                if(character.character!='O'||character.color.a!=0)continue;
                if(icon==historyStoneImages.Count)
                {
                    var go=new GameObject("HistoryStone",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
                    go.transform.SetParent(moveHistoryLabel.transform,false);
                    var image=go.GetComponent<Image>();image.raycastTarget=false;image.preserveAspect=true;
                    historyStoneImages.Add(image);
                }
                int color=historyStoneColors[icon];var stone=historyStoneImages[icon++];
                stone.sprite=color==1?guide.검은돌이미지:guide.흰돌이미지;
                stone.color=color==4?Color.gray:Color.white;stone.gameObject.SetActive(true);
                var rect=stone.rectTransform;rect.anchorMin=rect.anchorMax=moveHistoryLabel.rectTransform.pivot;
                rect.pivot=new Vector2(.5f,.5f);
                rect.sizeDelta=Vector2.one*character.pointSize*1.15f;
                rect.anchoredPosition=new Vector2((character.origin+character.xAdvance)*.5f,(character.ascender+character.descender)*.5f);
            }
        }
        for(int i=icon;i<historyStoneImages.Count;i++)historyStoneImages[i].gameObject.SetActive(false);
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
        moveHistoryLabel.ForceMeshUpdate();RefreshHistoryStoneImages();
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
            var match=AugmentMatch.Instance;
            bool privateOpponent=match!=null&&match.LocalSide!=0&&match.LocalSide!=(black?1:2);
            bool hiddenOpponent=privateOpponent&&!card.Used;
            if(hiddenOpponent)card=CardManager.HiddenCard();
            CardDetails details = slot.Root.GetComponent<CardDetails>();
            if (details == null) details = slot.Root.AddComponent<CardDetails>();
            int cardId = card.Name;
            string title = string.IsNullOrWhiteSpace(card.DisplayName) ? $"카드 {card.Name}" : card.DisplayName;
            bool showUsed=card.Used;
            slot.Title.text = title;
            if (slot.Description != null) { slot.Description.gameObject.SetActive(true); slot.Description.text = card.explanation; }
            if (slot.Value != null) { slot.Value.gameObject.SetActive(true); slot.Value.text = hiddenOpponent?"비공개":$"등급 {card.Value:0.#}"; }
            slot.Title.gameObject.SetActive(true);
            slot.Artwork.sprite = card.ItemImage;
            slot.Artwork.enabled = card.ItemImage != null;
            slot.Button.interactable = card.Have && !card.Used && cardManager.CanUseCard(black)
                && (AugmentMatch.Instance == null || AugmentMatch.Instance.CanControl(black ? 1 : 2));
            var face = slot.Button.targetGraphic as Image;
            ConfigureCompactText(slot,face);
            details.Bind(slot.Title, card.explanation, hiddenOpponent?"비공개":$"등급 {card.Value:0.#}",
                card.ItemImage, face == null ? null : face.sprite,showUsed);
            CardUsedAppearance.Apply(slot.Root,showUsed);
            ColorBlock colors = slot.Button.colors;
            colors.fadeDuration = 0f;
            slot.Button.colors = colors;
            slot.Button.transition = Selectable.Transition.None;
            slot.Button.onClick.RemoveAllListeners();
            slot.Button.onClick.AddListener(() => OnCardButtonClicked(black, cardId));
        }
    }
    private static void ConfigureCompactText(CardButtonSlot slot,Image face)
    {
        if(face==null)return;
        var frame=face.rectTransform;float width=frame.rect.width,height=frame.rect.height;
        if(face.preserveAspect&&face.sprite!=null)
        {
            float ratio=face.sprite.rect.width/Mathf.Max(1,face.sprite.rect.height);
            if(width/Mathf.Max(1,height)>ratio)width=height*ratio;else height=width/ratio;
        }
        void Place(TMP_Text text,float top,float slotHeight,float slotWidth,float fontSize)
        {
            if(text==null)return;
            var rect=text.rectTransform;
            rect.SetParent(frame,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
            rect.localScale=Vector3.one;rect.localRotation=Quaternion.identity;
            rect.anchoredPosition=frame.rect.center+new Vector2(0,height*(.5f-top-slotHeight*.5f));
            rect.sizeDelta=new Vector2(width*slotWidth,height*slotHeight);
            text.margin=Vector4.zero;text.enableAutoSizing=false;text.fontSize=fontSize;
            text.textWrappingMode=TextWrappingModes.Normal;text.overflowMode=TextOverflowModes.Ellipsis;
            text.raycastTarget=false;
        }
        Place(slot.Title,.055f,.13f,.84f,8f);
        Place(slot.Description,.57f,.23f,.78f,5.5f);
        Place(slot.Value,.845f,.08f,.72f,6.5f);
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
