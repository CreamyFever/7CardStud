using System;
using System.Collections.Generic;
using FreeNet;

namespace SevenPokerGameServer
{
    /// <summary>
    /// プレイヤーに関するデータを持つクラス。
    /// プレイヤーのインデックスと手札の数、そして、チップの情報
    /// </summary>
    public class CPlayer
    {
        public CGameUser m_Owner;
        public int m_PlayerIndex { get; private set; }

        public List<CCard> hand;
        public List<CCard> communityCards;                  // 表にした4枚のカード
        public List<CCard> handToSort;                     // 手札のカードをソートしてリストに入れる。

        public EvaluatedInfo handInfo;                      // 手持ちカード7枚の組み合わせで出来る役
        public EvaluatedInfo communityInfo;                 // 表にした4枚の組み合わせで出来る役

        public long chips;
        public long prevBetChips;                           // 前のターンでベットしたチップ
        public int bettingCount;
        public byte betIndex;                                // ｢ベット｣、｢レイズ｣、｢コール｣、｢フォールド｣などのバイトデータ

        public CPlayer()
        {

        }

        public CPlayer(CGameUser user, int playerIndex)
        {
            m_Owner = user;
            m_PlayerIndex = playerIndex;

            hand = new List<CCard>();
            communityCards = new List<CCard>();
            handToSort = new List<CCard>();

            handInfo = new EvaluatedInfo();
            communityInfo = new EvaluatedInfo();

            chips = user.possessChips;
            prevBetChips = 0;
            betIndex = 255;
        }

        public void ResetHand()
        {
            hand.Clear();
            communityCards.Clear();
            handToSort.Clear();
        }

        public void DrawCard(CCard card, bool isCommunity)
        {
            hand.Add(card);
            handToSort.Add(card);

            if (isCommunity)
                communityCards.Add(card);
        }

        public void DiscardCard(int index)
        {
            hand.RemoveAt(index);
            handToSort.RemoveAt(index);
        }

        /// <summary>
        /// 表にするカードをリストの一番後ろに移動
        /// </summary>
        /// <param name="card"></param>
        /// <param name="index"></param>
        public void TurnCardToVeryLast(CCard card, int index)
        {
            hand.Add(card);
            hand.RemoveAt(index);
            communityCards.Add(card);
        }

        public void Send(CPacket msg)
        {
            m_Owner.Send(msg);
            //CPacket.Destroy(msg);
        }

        public void SendForBroadcasting(CPacket msg)
        {
            m_Owner.Send(msg);
        }
    }
}