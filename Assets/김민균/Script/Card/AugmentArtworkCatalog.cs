using System;
using UnityEngine;

/// <summary>Explicit name-to-sprite references, included in player builds through Resources.</summary>
public sealed class AugmentArtworkCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string CardName;
        public Sprite Artwork;
    }

    public Entry[] Entries = Array.Empty<Entry>();

    public Sprite Find(string cardName)
    {
        foreach (Entry entry in Entries)
            if (entry != null && string.Equals(entry.CardName, cardName, StringComparison.Ordinal))
                return entry.Artwork;
        return null;
    }
}
