using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Loom.ZombieBattleground.Common;
using Newtonsoft.Json;

namespace Loom.ZombieBattleground.Data
{
    public class CardsLibraryData
    {
        public List<CardSet> Sets { get; private set; }

        public IList<Card> Cards { get; private set; }

        public int CardsInActiveSetsCount { get; private set; }

        public CardsLibraryData(IList<Card> cards)
        {
            Cards = cards ?? throw new ArgumentNullException(nameof(cards));
            InitData();
        }

        public Card GetCardFromName(string name)
        {
            return Cards.First(x => String.Equals(x.Name, name, StringComparison.InvariantCultureIgnoreCase));
        }

        private void InitData()
        {
            Cards = Cards.OrderBy(card => card.CardRank).ToList();
            Sets =
                Cards
                    .GroupBy(card => card.CardSetType)
                    .Select(group => new CardSet(group.Key, group.ToList()))
                    .OrderBy(set => set.Name)
                    .ToList();

            int id = 0;
            foreach (CardSet set in Sets)
            {
                foreach (Card card in set.Cards)
                {
                    // FIXME: why are we setting mould IDs manually?
                    if (card.CardSetType != Enumerators.SetType.OTHERS)
                    {
                        card.MouldId = id;
                        CardsInActiveSetsCount++;
                    }

                    id++;
                }
            }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            InitData();
        }
    }

    public class CardSet
    {
        public Enumerators.SetType Name { get; }
        public List<Card> Cards { get; }

        public CardSet(Enumerators.SetType name, List<Card> cards)
        {
            Name = name;
            Cards = cards;
        }

        public override string ToString()
        {
            return $"({nameof(Name)}: {Name}, {Cards.Count} cards)";
        }
    }

    public class CardList
    {
        [JsonProperty("cards")]
        public List<Card> Cards;
    }
}
