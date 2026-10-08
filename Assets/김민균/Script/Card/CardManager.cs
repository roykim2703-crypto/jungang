using System;
using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour

{
    public enum AcquisitionPhase { Early, Middle, Late }
    public enum PlayerSide { Black, White }

    [Serializable]
    public class CardData
    {
        public int Name;
        public int Value;
        public Sprite ItemImage;
        public string explanation;
        public bool Have;
        public bool Used;
        public string DisplayName;
        public AcquisitionPhase AvailableFrom = AcquisitionPhase.Early;
        public AcquisitionPhase AvailableUntil = AcquisitionPhase.Late;
}
    public List<CardData> CardList = new List<CardData>();

    [Header("카드 획득: 흑·백 합산 착수 수")]
    [Min(1)] public int StonesPerReward = 20;
    [Min(1)] public int MaxRewardRounds = 3;
    [Min(1)] public int EarlyPhaseLastStone = 20;
    [Min(2)] public int MiddlePhaseLastStone = 40;
    [SerializeField] private CardAcquisitionUI acquisitionUI;
    [Header("같은 시드와 카드 설정은 같은 순서로 추첨")]
    public bool UseFixedSeed;
    public int FixedSeed = 12345;
    [SerializeField] private int currentSeed;
    [SerializeField] private int placedStoneCount;
    [SerializeField] private int rewardRoundCount;
    [SerializeField] private List<CardData> blackInventory = new List<CardData>();
    [SerializeField] private List<CardData> whiteInventory = new List<CardData>();
    [Header("턴당 카드 사용 제한")]
    [SerializeField] private bool currentTurnIsBlack = true;
    [SerializeField] private bool cardUsedThisTurn;

    private System.Random random;
    private CardData[] blackOffers = Array.Empty<CardData>();
    private CardData[] whiteOffers = Array.Empty<CardData>();
    private bool blackSelected;
    private bool whiteSelected;
    private bool turnInitialized;
    public bool IsChoosing { get; private set; }
    public int CurrentSeed => currentSeed;
    public int PlacedStoneCount => placedStoneCount;
    public int RewardRoundCount => rewardRoundCount;
    public bool HasMoreRewards => rewardRoundCount < Mathf.Max(1, MaxRewardRounds);
    public int StonesUntilNextReward
    {
        get
        {
            if (!HasMoreRewards) return 0;
            int targetStoneCount = rewardRoundCount * Mathf.Max(1, StonesPerReward);
            return Mathf.Max(0, targetStoneCount - placedStoneCount);
        }
    }
    public float RewardProgress01
    {
        get
        {
            if (!HasMoreRewards) return 1f;
            if (rewardRoundCount == 0) return 0f;
            int interval = Mathf.Max(1, StonesPerReward);
            int previousTarget = (rewardRoundCount - 1) * interval;
            return Mathf.Clamp01((placedStoneCount - previousTarget) / (float)interval);
        }
    }
    public bool CurrentTurnIsBlack => currentTurnIsBlack;
    public bool CardUsedThisTurn => cardUsedThisTurn;
    public IReadOnlyList<CardData> BlackInventory => blackInventory;
    public IReadOnlyList<CardData> WhiteInventory => whiteInventory;
    public event Action AcquisitionCompleted;
    public event Action<bool, CardData> CardAcquired;
    public event Action<PlayerSide, CardData> CardAcquiredForPlayer;
    public event Action CardAvailabilityChanged;
    public event Action RewardProgressChanged;

    private void Awake()
    {
        InitializeSeed(UseFixedSeed ? FixedSeed : Guid.NewGuid().GetHashCode());
        if (acquisitionUI != null) acquisitionUI.Hide();
    }

    public void InitializeSeed(int seed)
    {
        currentSeed = seed;
        random = new System.Random(seed);
    }

    public AcquisitionPhase GetPhase(int stoneCount)
    {
        if (stoneCount <= EarlyPhaseLastStone) return AcquisitionPhase.Early;
        return stoneCount <= MiddlePhaseLastStone ? AcquisitionPhase.Middle : AcquisitionPhase.Late;
    }

    /// <summary>시드 기반 범위 난수. 시작값 포함, 끝값 제외.</summary>
    public int RandomInRange(int startInclusive, int endExclusive)
    {
        if (endExclusive <= startInclusive) throw new ArgumentOutOfRangeException(nameof(endExclusive));
        if (random == null) InitializeSeed(UseFixedSeed ? FixedSeed : Guid.NewGuid().GetHashCode());
        return random.Next(startInclusive, endExclusive);
    }

    /// <summary>카드 목록의 [시작, 끝) 범위와 획득 시기를 모두 적용한다. Name이 같은 카드는 같은 종류다.</summary>
    public bool TryDrawOffers(AcquisitionPhase phase, int startInclusive, int endExclusive,
        out CardData[] black, out CardData[] white)
    {
        black = Array.Empty<CardData>();
        white = Array.Empty<CardData>();
        if (startInclusive < 0 || endExclusive > CardList.Count || startInclusive >= endExclusive) return false;
        var candidates = new List<CardData>();
        var uniqueNames = new HashSet<int>();
        for (int i = startInclusive; i < endExclusive; i++)
        {
            CardData card = CardList[i];
            if (card != null && card.AvailableFrom <= phase && phase <= card.AvailableUntil && uniqueNames.Add(card.Name))
                candidates.Add(card);
        }
        // 후보가 부족하면 중복으로 채우거나 무한 재추첨하지 않는다.
        if (candidates.Count < 6) return false;
        var drawn = new CardData[6];
        for (int i = 0; i < drawn.Length; i++)
        {
            int index = RandomInRange(i, candidates.Count);
            CardData selected = candidates[index];
            candidates[index] = candidates[i];
            candidates[i] = selected;
            drawn[i] = selected;
        }
        black = new[] { drawn[0], drawn[1], drawn[2] };
        white = new[] { drawn[3], drawn[4], drawn[5] };
        return true;
    }

    /// <summary>일반 착수가 성공한 뒤 한 번 호출. 선택 중 호출은 카운트하지 않는다.</summary>
    public bool NotifyStonePlaced()
    {
        if (IsChoosing) return false;
        placedStoneCount++;
        RewardProgressChanged?.Invoke();
        if (!HasMoreRewards) return false;
        int targetStoneCount = rewardRoundCount * Mathf.Max(1, StonesPerReward);
        if (placedStoneCount != targetStoneCount) return false;
        return TryOpenAcquisition();
    }

    /// <summary>대국 시작 시 첫 번째 카드 획득을 연다.</summary>
    public bool TryStartInitialAcquisition()
    {
        if (IsChoosing || rewardRoundCount != 0 || placedStoneCount != 0) return false;
        return TryOpenAcquisition();
    }

    private bool TryOpenAcquisition()
    {
        AcquisitionPhase phase = GetPhase(placedStoneCount);
        if (acquisitionUI == null || !acquisitionUI.IsConfigured)
        {
            Debug.LogError("카드 획득 Canvas 연결을 확인하세요.", this);
            return false;
        }
        if (!TryDrawOffers(phase, 0, CardList.Count, out blackOffers, out whiteOffers))
        {
            Debug.LogWarning($"{phase} 시기에 획득 가능한 서로 다른 카드가 6장 이상 필요합니다.", this);
            return false;
        }
        blackSelected = whiteSelected = false;
        IsChoosing = true;
        rewardRoundCount++;
        CardAvailabilityChanged?.Invoke();
        RewardProgressChanged?.Invoke();
        acquisitionUI.Show(this, blackOffers, whiteOffers, phase, placedStoneCount);
        return true;
    }

    public bool SelectCard(bool black, int slot)
    {
        if (!IsChoosing || slot < 0 || slot >= 3 || (black ? blackSelected : whiteSelected)) return false;
        if (!black && !blackSelected) return false;
        CardData source = (black ? blackOffers : whiteOffers)[slot];
        // 소유 상태는 카드 원본과 분리해 두 플레이어가 각각 보관한다.
        var acquired = new CardData
        {
            Name = source.Name, Value = source.Value, ItemImage = source.ItemImage,
            explanation = source.explanation, DisplayName = source.DisplayName,
            AvailableFrom = source.AvailableFrom, AvailableUntil = source.AvailableUntil, Have = true
        };
        (black ? blackInventory : whiteInventory).Add(acquired);
        CardAcquired?.Invoke(black, acquired);
        CardAcquiredForPlayer?.Invoke(black ? PlayerSide.Black : PlayerSide.White, acquired);
        if (black) blackSelected = true;
        else whiteSelected = true;
        acquisitionUI.MarkSelected(black, slot);
        if (blackSelected && whiteSelected)
        {
            IsChoosing = false;
            acquisitionUI.Hide();
            AcquisitionCompleted?.Invoke();
        }
        return true;
    }

    /// <summary>착수 가능한 턴이 시작될 때 호출한다. 같은 턴을 다시 준비해도 사용 횟수는 초기화하지 않는다.</summary>
    public void BeginTurn(bool black)
    {
        BeginTurn(black ? PlayerSide.Black : PlayerSide.White);
    }

    public void BeginTurn(PlayerSide side)
    {
        bool black = side == PlayerSide.Black;
        if (!turnInitialized || currentTurnIsBlack != black)
        {
            currentTurnIsBlack = black;
            cardUsedThisTurn = false;
            turnInitialized = true;
        }
        CardAvailabilityChanged?.Invoke();
    }

    public bool CanUseCard(bool black) =>
        CanUseCard(black ? PlayerSide.Black : PlayerSide.White);

    public bool CanUseCard(PlayerSide side) =>
        turnInitialized && !IsChoosing && currentTurnIsBlack == (side == PlayerSide.Black) && !cardUsedThisTurn;

    public bool CheckCard(int name, bool black) =>
        (black ? blackInventory : whiteInventory).Exists(card => card.Name == name && card.Have && !card.Used);

    public bool UseCard(int name, bool black)
    {
        return UseCard(name, black ? PlayerSide.Black : PlayerSide.White);
    }

    public bool UseCard(int name, PlayerSide side)
    {
        bool black = side == PlayerSide.Black;
        if (!CanUseCard(side)) return false;
        CardData card = (black ? blackInventory : whiteInventory).Find(c => c.Name == name && c.Have && !c.Used);
        if (card == null) return false;
        card.Used = true;
        card.Have = false;
        cardUsedThisTurn = true;
        CardAvailabilityChanged?.Invoke();
        return true;
    }

    /// <summary>사용 여부와 관계없이 이번 대국에서 획득한 모든 카드의 등급 합계.</summary>
    public int GetCardGradeTotal(PlayerSide side)
    {
        List<CardData> inventory = side == PlayerSide.Black ? blackInventory : whiteInventory;
        int total = 0;
        foreach (CardData card in inventory)
            if (card != null) total += card.Value;
        return total;
    }

    /// <summary>양수면 흑의 등급 합이 높고, 음수면 백의 등급 합이 높다.</summary>
    public int CompareCardGradeTotals() =>
        GetCardGradeTotal(PlayerSide.Black).CompareTo(GetCardGradeTotal(PlayerSide.White));

    public bool CheckCard(int name)
    {
        foreach (var card in CardList)
        {
            if (card.Name == name)
            {
                return card.Have;
            }
        }
        return false;
    }

    public void GetCard(int name)
    {
        foreach (var card in CardList)
        {
            if (card.Name == name)
            {
                if (!card.Have && !card.Used)
                {
                    card.Have = true;
                }
                break;
            }
        }
    }

    public bool UseCard(int name)
    {
        foreach (var card in CardList)
        {
            if (card.Name == name && card.Have && !card.Used)
            {
                if(card.Name == name)
                {
                    if (card.Have && !card.Used)
                    {
                        card.Used = true;
                        card.Have = false;
                        return true;
                    }
                }
            }
        }
        return false;
    }

    
   
}
