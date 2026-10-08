using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardAcquisitionUI : MonoBehaviour
{
    [Serializable]
    public class CardSlot
    {
        public Button Button;
        public TMP_Text Title;
        public TMP_Text Description;
        public TMP_Text Value;
        public TMP_Text Timing;
        public Image Artwork;
    }

    public TMP_Text Heading;
    public TMP_Text BlackStatus;
    public TMP_Text WhiteStatus;
    public GameObject BlackGroup;
    public GameObject WhiteGroup;
    [Tooltip("표시 카드의 원본 프리팹. 슬롯은 이 프리팹의 인스턴스입니다.")]
    public GameObject CardPrefab;
    public CardSlot[] BlackSlots = new CardSlot[3];
    public CardSlot[] WhiteSlots = new CardSlot[3];
    public bool IsConfigured => Heading != null && BlackStatus != null && WhiteStatus != null
        && BlackGroup != null && WhiteGroup != null && ValidSlots(BlackSlots) && ValidSlots(WhiteSlots);

    private static bool ValidSlots(CardSlot[] slots)
    {
        return slots != null && slots.Length == 3 && Array.TrueForAll(slots,
            slot => slot != null && slot.Button != null && slot.Title != null && slot.Description != null && slot.Value != null && slot.Timing != null && slot.Artwork != null);
    }

    public void Show(CardManager manager, CardManager.CardData[] black, CardManager.CardData[] white,
        CardManager.AcquisitionPhase phase, int stoneCount)
    {
        gameObject.SetActive(true);
        string timing = stoneCount == 0 ? "게임 시작" : $"{stoneCount}수";
        Heading.text = $"흑 카드 선택  ·  {PhaseName(phase)}  ·  {timing}";
        BlackStatus.text = "흑 · 카드 1장을 선택하세요";
        WhiteStatus.text = "백 · 카드 1장을 선택하세요";
        Bind(manager, BlackSlots, black, true);
        Bind(manager, WhiteSlots, white, false);
        ShowPlayerStage(true);
        SetInteractable(BlackSlots, true);
        SetInteractable(WhiteSlots, false);
    }

    private static void Bind(CardManager manager, CardSlot[] slots, CardManager.CardData[] cards, bool black)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            int index = i;
            CardSlot slot = slots[i];
            CardManager.CardData card = cards[i];
            slot.Button.transition = Selectable.Transition.None;
            slot.Title.text = string.IsNullOrWhiteSpace(card.DisplayName) ? $"카드 {card.Name}" : card.DisplayName;
            slot.Description.text = card.explanation;
            slot.Value.text = $"등급 {card.Value}";
            slot.Timing.text = card.AvailableFrom == card.AvailableUntil
                ? $"{PhaseName(card.AvailableFrom)} 획득"
                : $"{PhaseName(card.AvailableFrom)} ~ {PhaseName(card.AvailableUntil)} 획득";
            slot.Artwork.sprite = card.ItemImage;
            slot.Artwork.enabled = card.ItemImage != null;
            slot.Button.interactable = true;
            slot.Button.onClick.RemoveAllListeners();
            slot.Button.onClick.AddListener(() => manager.SelectCard(black, index));
        }
    }

    public void MarkSelected(bool black, int index)
    {
        CardSlot[] slots = black ? BlackSlots : WhiteSlots;
        SetInteractable(slots, false);
        (black ? BlackStatus : WhiteStatus).text = $"{(black ? "흑" : "백")} · {slots[index].Title.text} 획득 완료";
        if (black)
        {
            Heading.text = Heading.text.Replace("흑 카드 선택", "백 카드 선택");
            ShowPlayerStage(false);
            SetInteractable(WhiteSlots, true);
        }
    }

    private void ShowPlayerStage(bool black)
    {
        BlackStatus.gameObject.SetActive(black);
        BlackGroup.SetActive(black);
        WhiteStatus.gameObject.SetActive(!black);
        WhiteGroup.SetActive(!black);
    }

    private static void SetInteractable(CardSlot[] slots, bool interactable)
    {
        foreach (CardSlot slot in slots) slot.Button.interactable = interactable;
    }

    public void Hide() => gameObject.SetActive(false);

    public static string PhaseName(CardManager.AcquisitionPhase phase)
    {
        switch (phase)
        {
            case CardManager.AcquisitionPhase.Early: return "초반";
            case CardManager.AcquisitionPhase.Middle: return "중반";
            default: return "후반";
        }
    }
}
