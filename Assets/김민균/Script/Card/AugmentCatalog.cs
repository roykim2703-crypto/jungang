using System;
using UnityEngine;

public static class AugmentCatalog
{
    [Serializable] private class Catalog { public CardManager.CardData[] cards; }
    public static void Populate(CardManager manager)
    {
        TextAsset source = Resources.Load<TextAsset>("AugmentCatalog");
        if (source == null) return;
        var catalog = JsonUtility.FromJson<Catalog>(source.text.TrimStart('\uFEFF'));
        var artwork = Resources.Load<AugmentArtworkCatalog>("AugmentArtworkCatalog");
        foreach (var card in catalog.cards)
        {
            card.ItemImage = artwork == null ? null : artwork.Find(card.DisplayName);
            if (card.ItemImage == null)
                Debug.LogWarning($"증강 이미지 연결을 확인하세요: {card.DisplayName}", manager);
        }
        manager.CardList.Clear(); manager.CardList.AddRange(catalog.cards);
    }
}
