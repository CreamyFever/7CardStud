using System;
using System.Collections.Generic;

namespace SevenPokerGameServer
{
    public enum HAND : short
    {
        HIGH_CARD = 0,
        ONE_PAIR,
        TWO_PAIRS,
        THREE_OF_A_KIND,
        STRAIGHT,
        FLUSH,
        FULL_HOUSE,
        FOUR_OF_A_KIND,
        STRAIGHT_FLUSH,
        ROYAL_FLUSH
    }

    public struct EvaluatedInfo
    {
        public int highRank;
        public int highShape;

        public int sumOfClovers;
        public int sumOfHearts;
        public int sumOfDiamonds;
        public int sumOfSpades;

        public HAND currentHand;

        public List<CCard> handCard;
    }

    /// <summary>
    /// 手持ちのカードの組み合わせと役を比べるクラス
    /// </summary>
    class CCardEvaluator : CCard
    {
        private EvaluatedInfo info;

        public CCardEvaluator()
        {
            info = new EvaluatedInfo();
        }

        public void Initialize(int playerIndex)
        {
            info.highRank = 0;
            info.highShape = 0;

            info.sumOfClovers = 0;
            info.sumOfHearts = 0;
            info.sumOfDiamonds = 0;
            info.sumOfSpades = 0;

            info.currentHand = HAND.HIGH_CARD;

            info.handCard = new List<CCard>();
        }

        public void AddCard(List<CCard> card)
        {
            info.handCard = card;
        }

        public void ClearInfoHand()
        {
            info.handCard.Clear();
        }

        public EvaluatedInfo EvaluateHandCards()
        {
            if (CheckRoyalFlush(info.handCard))
            {
                info.currentHand = HAND.ROYAL_FLUSH;
            }
            else if (CheckStraightFlush(info.handCard))
            {
                info.currentHand = HAND.STRAIGHT_FLUSH;
            }

            // フラッシュの場合、フォーカードやフルハウスの組み合わせは出来ないゆえにフラッシュを先にチェック
            else if (CheckFlush(info.handCard))
            {
                info.currentHand = HAND.FLUSH;
            }
            else if (CheckFourOfAKind(info.handCard))
            {
                info.currentHand = HAND.FOUR_OF_A_KIND;
            }
            else if (CheckFullHouse(info.handCard))
            {
                info.currentHand = HAND.FULL_HOUSE;
            }
            else if (CheckStraight(info.handCard))
            {
                info.currentHand = HAND.STRAIGHT;
            }
            else if (CheckThreeOfAKind(info.handCard))
            {
                info.currentHand = HAND.THREE_OF_A_KIND;
            }
            else if (CheckTwoPairs(info.handCard))
            {
                info.currentHand = HAND.TWO_PAIRS;
            }
            else if (CheckPair(info.handCard))
            {
                info.currentHand = HAND.ONE_PAIR;
            }
            else
            {
                info.highRank = HighRankOnHand(info.handCard);
                info.currentHand = HAND.HIGH_CARD;
            }

            return info;
        }

        /// <summary>
        /// 同じスートのカードを数える
        /// </summary>
        /// <param name="cards"></param>
        void CountCardsSuit(List<CCard> cards)
        {
            for (int i = 0; i < cards.Count; ++i)
            {
                switch ((CARD_SUIT)cards[i].Suit)
                {
                    case CARD_SUIT.CLOVER:
                        info.sumOfClovers++;
                        break;
                    case CARD_SUIT.HEART:
                        info.sumOfHearts++;
                        break;
                    case CARD_SUIT.DIAMOND:
                        info.sumOfDiamonds++;
                        break;
                    case CARD_SUIT.SPADE:
                        info.sumOfSpades++;
                        break;
                }
            }
        }

        void InitializeSumOfSuit()
        {
            info.sumOfClovers = 0;
            info.sumOfHearts = 0;
            info.sumOfDiamonds = 0;
            info.sumOfSpades = 0;
        }

        /// <summary>
        /// 作った役で一番大きな数字のカード
        /// </summary>
        /// <param name="cards"></param>
        /// <returns></returns>
        int HighRankOnHand(List<CCard> cards)
        {
            int rank = 0;

            if (cards.Count == 1)
            {
                rank = cards[0].Rank;
                return rank;
            }

            for (int i = 0; i < cards.Count - 1; ++i)
            {
                if (cards[i].Rank < cards[i + 1].Rank)
                {
                    rank = cards[i + 1].Rank;
                }
                else
                {
                    rank = cards[i].Rank;
                }
            }

            return rank;
        }

        /// <summary>
        /// 同じスートの10、J、Q、K、Aを揃える。
        /// </summary>
        /// <param name="cards"></param>
        /// <returns></returns>
        public bool CheckRoyalFlush(List<CCard> cards)
        {
            int num = 0;

            if (CheckFlush(cards))
            {
                for (int i = 0; i < cards.Count - 1; ++i)
                {
                    if (info.highShape == cards[i].Suit && cards[i].Rank == 10 && cards[i].Rank == cards[i + 1].Rank - 1)
                    {
                        num++;
                    }
                }
            }

            if (num == 4)
            {
                if (CheckFlush(cards))
                {
                    info.highRank = 14;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 同じスートで、5枚の数字が連続する。
        /// </summary>
        /// <param name="cards"></param>
        /// <returns></returns>
        public bool CheckStraightFlush(List<CCard> cards)
        {
            int num = 0;
            int offset = 0;

            // ソーティングされたカードのリストの最後のインデックスに該当するカードがエースの場合
            if (cards[cards.Count - 1].Rank == 14)
            {
                for (int i = 0; i < cards.Count - 2; ++i)
                {
                    if (cards[i].Rank == 2)
                    {
                        if (cards[i].Rank == cards[i + 1].Rank - 1 && cards[i].Suit == cards[i + 1].Suit)
                        {
                            num++;
                        }
                        else if (cards[i].Rank == cards[i + 1].Rank)
                        {
                            offset++;
                            continue;
                        }
                        else if (cards[i].Rank != cards[i + 1].Rank - 1)
                        {
                            break;
                        }
                    }

                    info.highRank = cards[num].Rank;
                }

                if (num >= 3)
                {
                    return true;
                }

                num = 0;
                offset = 0;
            }


            for (int i = 0; i < cards.Count - 1; ++i)
            {
                if (cards[i].Rank == cards[i + 1].Rank - 1 && cards[i].Suit == cards[i + 1].Suit)
                {
                    num++;
                }
                else if (cards[i].Rank == cards[i + 1].Rank)
                {
                    offset++;
                    continue;
                }
                else if (cards[i].Rank != cards[i + 1].Rank - 1)
                {
                    offset++;
                    num = 0;
                }

                info.highRank = cards[num + offset].Rank;
            }

            if (num >= 4)
            {
                return true;
            }


            return false;
        }

        /// <summary>
        /// 同じ数字のカードが4枚ある。
        /// </summary>
        /// <param name="cards"></param>
        /// <returns></returns>
        public bool CheckFourOfAKind(List<CCard> cards)
        {
            int k = 0;

            for (int compareCount = 0; compareCount < cards.Count - 3; ++compareCount)
            {
                k = compareCount;
                while (k < (compareCount + 3) && cards[k].Rank == cards[k + 1].Rank)
                {
                    k++;
                }
                if (k == (compareCount + 3))
                {
                    info.highRank = cards[k].Rank;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// スリーカードとワンペアが1組ずつできる。
        /// </summary>
        /// <param name="cards"></param>
        /// <returns></returns>
        public bool CheckFullHouse(List<CCard> cards)
        {
            int k = 0;

            for (int compareCount = 0; compareCount < cards.Count - 2; ++compareCount)
            {
                k = compareCount;
                while (k < (compareCount + 2) && cards[k].Rank == cards[k + 1].Rank)
                {
                    k++;
                }

                // ソーティングされたカードのリストでスリーカードが先に出た場合、残りのカードでワンペアをチェック
                if (k == (compareCount + 2))
                {
                    info.highRank = cards[k].Rank;

                    for (int j = k + 1; j < cards.Count - 1; ++j)
                    {
                        if (cards[j].Rank == cards[j + 1].Rank)
                        {
                            return true;
                        }
                    }
                }

                // ワンペアが先に出た場合、残りのカードでスリーカードをチェック
                else if (k == (compareCount + 1))
                {
                    for (int j = k + 1; j < cards.Count - 2; ++j)
                    {
                        if (cards[j].Rank == cards[j + 2].Rank)
                        {
                            info.highRank = cards[j].Rank;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 同じスートのカードが5枚ある。
        /// </summary>
        /// <param name="cards"></param>
        /// <returns></returns>
        public bool CheckFlush(List<CCard> cards)
        {
            CountCardsSuit(cards);

            if (info.sumOfClovers >= 5)
            {
                info.highShape = (int)CARD_SUIT.CLOVER;
                InitializeSumOfSuit();
                return true;
            }
            else if (info.sumOfHearts >= 5)
            {
                info.highShape = (int)CARD_SUIT.HEART;
                InitializeSumOfSuit();
                return true;
            }
            else if (info.sumOfDiamonds >= 5)
            {
                info.highShape = (int)CARD_SUIT.DIAMOND;
                InitializeSumOfSuit();
                return true;
            }
            else if (info.sumOfSpades >= 5)
            {
                info.highShape = (int)CARD_SUIT.SPADE;
                InitializeSumOfSuit();
                return true;
            }

            InitializeSumOfSuit();

            return false;
        }

        /// <summary>
        /// スートに関係なく、5枚の数字が連続する。(10-J-Q-K-Aはストレートとなるが、Q-K-A-2-3はストレートにならない。
        /// すなわち、KとAは連続するが、K-A-2含むものはストレートにはならない)
        /// </summary>
        /// <param name="cards"></param>
        /// <returns></returns>
        public bool CheckStraight(List<CCard> cards)
        {
            int num = 0;
            int offset = 0;

            // ソーティングされたカードのリストの最後のインデックスに該当するカードがエースの場合
            if (cards[cards.Count - 1].Rank == 14)
            {
                for (int i = 0; i < cards.Count - 2; ++i)
                {
                    if (cards[i].Rank == 2)
                    {
                        if (cards[i].Rank == cards[i + 1].Rank - 1)
                        {
                            num++;
                        }
                        else if (cards[i].Rank == cards[i + 1].Rank)
                        {
                            offset++;
                            continue;
                        }
                        else if (cards[i].Rank != cards[i + 1].Rank - 1)
                        {
                            break;
                        }
                    }

                    info.highRank = cards[num].Rank;
                }

                if (num >= 3)
                {
                    return true;
                }

                num = 0;
                offset = 0;
            }


            for (int i = 0; i < cards.Count - 1; ++i)
            {
                if (cards[i].Rank == cards[i + 1].Rank - 1)
                {
                    num++;
                }
                else if (cards[i].Rank == cards[i + 1].Rank)
                {
                    offset++;
                    continue;
                }
                else if (cards[i].Rank != cards[i + 1].Rank - 1)
                {
                    offset++;
                    num = 0;
                }

                info.highRank = cards[num + offset].Rank;
            }

            if (num >= 4)
            {
                return true;
            }


            return false;
        }

        /// <summary>
        /// 同じ数字のカードが3枚ある。
        /// </summary>
        /// <param name="cards"></param>
        /// <returns></returns>
        public bool CheckThreeOfAKind(List<CCard> cards)
        {
            int k = 0;

            for (int compareCount = 0; compareCount < cards.Count - 2; ++compareCount)
            {
                k = compareCount;
                while (k < (compareCount + 2) && cards[k].Rank == cards[k + 1].Rank)
                {
                    k++;
                }
                if (k == (compareCount + 2))
                {
                    info.highRank = cards[k].Rank;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 同じ数字のカードが2組ある。
        /// </summary>
        /// <param name="cards"></param>
        /// <returns></returns>
        public bool CheckTwoPairs(List<CCard> cards)
        {
            int pair = 0;
            info.highRank = 0;
            for (int i = 0; i < cards.Count - 1; ++i)
            {
                if (cards[i].Rank == cards[i + 1].Rank)
                {
                    if (cards[i].Rank >= info.highRank)
                        info.highRank = cards[i].Rank;
                    pair++;
                    i++;
                }
            }

            if (pair >= 2)
                return true;
            else
                return false;
        }

        /// <summary>
        /// 同じ数字のカードが1組だけある。
        /// </summary>
        /// <param name="cards"></param>
        /// <returns></returns>
        public bool CheckPair(List<CCard> cards)
        {
            int pair = 0;
            for (int i = 0; i < cards.Count - 1; ++i)
            {
                if (cards[i].Rank == cards[i + 1].Rank)
                {
                    info.highRank = cards[i].Rank;
                    pair++;
                    break;
                }
            }

            if (pair == 1)
                return true;
            else
                return false;
        }

    }
}