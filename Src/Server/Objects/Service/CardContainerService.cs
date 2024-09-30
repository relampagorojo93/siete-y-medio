using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _7YMedioServer.Objects.Service
{
    public class CardContainerService
    {

        private Dictionary<Card, int> cards = new Dictionary<Card, int>();

        public CardContainerService() { }

        public void ResetDeck()
        {
            foreach (Card card in Card.CARDS)
            {
                cards.Add(card, 4);
            }
        }

        public Card PullCard()
        {
            Card card = this.cards.Keys.ElementAt((int) (this.cards.Keys.Count * new Random().Next()));

            this.cards[card] = this.cards[card] - 1;

            if (this.cards[card] == 0)
            {
                this.cards.Remove(card);
            }

            return card;
        }


    }

    public class Card
    {

        public static Card CARD_1 = new Card("Card 1", 1);
        public static Card CARD_2 = new Card("Card 2", 2);
        public static Card CARD_3 = new Card("Card 3", 3);
        public static Card CARD_4 = new Card("Card 4", 4);
        public static Card CARD_5 = new Card("Card 5", 5);
        public static Card CARD_6 = new Card("Card 6", 6);
        public static Card CARD_7 = new Card("Card 7", 7);
        public static Card CARD_10 = new Card("Card 10", 0.5);
        public static Card CARD_11 = new Card("Card 11", 0.5);
        public static Card CARD_12 = new Card("Card 11", 0.5);

        public static List<Card> CARDS = new List<Card>()
            {
                CARD_1, CARD_2, CARD_3, CARD_4, CARD_5, CARD_6, CARD_7, CARD_10, CARD_11, CARD_12
            };

        private String name;
        private double value;

        public Card(string name, double value)
        {
            this.name = name;
            this.value = value;
        }

        public String GetName() { return name; }
        public double GetValue() { return value; }

    }
}
