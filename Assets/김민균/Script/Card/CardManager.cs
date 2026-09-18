using System;
using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour

{
    [Serializable]
    public class CardData
    {
        public int Name;
        public int Value;
        public Sprite ItemImage;
        public string explanation;
        public bool Have;
        public bool Used;
}
    public List<CardData> CardList = new List<CardData>();

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
