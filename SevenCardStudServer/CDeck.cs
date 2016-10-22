using System;
using System.Collections.Generic;

namespace SevenPokerGameServer
{
    // デッキクラス
    public class CDeck : CCard
    {
        private const int TOTAL_CARDS = 52;
        private const int SHAPES = 4;
        private const int RANKS = 13;

        private CCard[] m_Deck;

        private Stack<CCard> m_DeckStack;

        CCardEvaluator handEvaluator;
        EvaluatedInfo evalInfo;

        public CDeck()
        {
            m_Deck = new CCard[TOTAL_CARDS];
            m_DeckStack = new Stack<CCard>(TOTAL_CARDS);
        }

        // 役を判別するインスタンスを初期化
        public void InitEvaluator(CPlayer player)
        {
            handEvaluator = new CCardEvaluator();
            handEvaluator.Initialize(player.m_PlayerIndex);
        }
        /// <summary>
        /// デッキをセットしてシャッフル
        /// </summary>
        public void SetUpDeck()
        {
            for (int i = 0; i < SHAPES; ++i)
            {
                for (int j = 2; j < RANKS + 2; ++j)
                {
                    m_Deck[i * RANKS + (j - 2)] = new CCard((CARD_SUIT)i, (CARD_RANK)j);
                }
            }

            ShuffleDeck();
        }


        /// <summary>
        /// カードをシャッフル
        /// </summary>
        public void ShuffleDeck()
        {
            Random random = new Random();
            CCard card;
            m_DeckStack.Clear();

            for (int i = 0; i < m_Deck.Length; i++)
            {
                int secondCardIndex = random.Next(51);
                card = m_Deck[i];
                m_Deck[i] = m_Deck[secondCardIndex];
                m_Deck[secondCardIndex] = card;
            }

            for (int i = 0; i < m_Deck.Length; i++)
            {
                m_DeckStack.Push(m_Deck[i]);
            }
        }

        public CCard DealCard(CPlayer player, bool isCommunity)
        {
            CCard card = m_DeckStack.Pop();

            player.DrawCard(card, isCommunity);

            return card;
        }

        public EvaluatedInfo EvaluateHand(List<CCard> cards)
        {
            List<CCard> playerHand = SortCards(cards);

            handEvaluator.AddCard(playerHand);

            evalInfo = handEvaluator.EvaluateHandCards();

            return evalInfo;
        }

        public List<CCard> SortCards(List<CCard> cardList)
        {
            List<CCard> cardsToSort = cardList;

            if (cardsToSort.Count > 1)
                cardsToSort.Sort(delegate (CCard c1, CCard c2) { return c1.Rank.CompareTo(c2.Rank); });

            return cardsToSort;
        }
    }
}