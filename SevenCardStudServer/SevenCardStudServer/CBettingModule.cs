using System;
using System.Collections.Generic;

namespace SevenPokerGameServer
{
    public enum BET : byte
    {
        Fold = 0,
        Check,
        BringIn,
        Call,
        Complete,
        Raise,
        Bet,
        AllIn
    }

    /// <summary>
    /// ベッティングモジュール
    /// </summary>
    public class CBettingModule
    {
        const int MAX_PLAYER = 5;

        private long m_Total;                           // ポット合計
        private long m_Ante;                            // アンティ
        private long m_RaiseChips;                      // 毎ストリートに賭けられたチップ
        private long m_BeforePlayerBet;                 // 前のターンで賭けたチップ

        private long m_BetChips;                         // プレイヤーが実際賭けるチップ

        public long Total
        {
            get { return m_Total; }
        }

        public long Ante
        {
            get { return m_Ante; }
        }

        public long RaiseChips
        {
            get { return m_RaiseChips; }
            set { m_RaiseChips = value; }
        }

        public long BeforePlayerBet
        {
            get { return m_BeforePlayerBet; }
        }

        public long BetChip
        {
            get { return m_BetChips; }
        }

        Dictionary<int, PLAYER_STATE> m_PlayerState;            // プレイヤーの状態を管理 <プレイヤーに与えたインデックス、プレイヤーの状態>

        Dictionary<int, long> m_EachPlayersChips;               // プレイヤーの所持チップを管理 <プレイヤーに与えたインデックス、所持チップ>

        public CBettingModule(long ante)
        {
            m_Total = 0;                            // ポット合計
            m_Ante = ante;                          // アンティ
            m_RaiseChips = 0;                       // 毎ストリートに賭けられたチップ
            m_BeforePlayerBet = 0;                  // 前のターンで賭けたチップ

            m_BetChips = 0;                         // プレイヤーが実際賭けるチップ

            m_PlayerState = new Dictionary<int, PLAYER_STATE>();
            m_EachPlayersChips = new Dictionary<int, long>();
        }

        public void SetEachPlayerChips(CPlayer player)
        {
            if (player != null)
                m_EachPlayersChips.Add(player.m_PlayerIndex, player.chips);
        }

        public long GetEachPlayerChips(int key)
        {
            if (!m_EachPlayersChips.ContainsKey(key))
            {
                return -1;
            }

            return m_EachPlayersChips[key];
        }

        public void ClearPlayerChips()
        {
            m_EachPlayersChips.Clear();
        }

        public void SetBeforePlayerChips(long chips)
        {
            m_BeforePlayerBet = chips;
        }

        /// <summary>
        /// 現在、全プレイヤーの状態がパラメーターの｢cState｣なのかを確認
        /// </summary>
        /// <param name="cState">プレイヤーの状態</param>
        /// <returns></returns>
        public bool isAllReady(CPlayer[] m_Players, PLAYER_STATE cState)
        {
            foreach (var state in m_PlayerState)
            {
                // 空席やフォールドを宣言したプレイヤー、観戦は除く。
                if (m_Players[state.Key] == null || (isPlayerState(state.Key, PLAYER_STATE.FOLD) || isPlayerState(state.Key, PLAYER_STATE.OBSERVE)))
                    continue;
                if (state.Value != cState)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// プレイヤーの状態をチェック
        /// </summary>
        /// <param name="index"></param>
        /// <param name="cState"></param>
        /// <returns></returns>
        public bool isPlayerState(int index, PLAYER_STATE cState)
        {
            if (m_PlayerState[index] == cState)
                return true;
            else
                return false;
        }

        public void RemovePlayerState(CGameUser user)
        {
            m_PlayerState.Remove(user.gamePlayer.m_PlayerIndex);
        }

        /// <summary>
        /// プレイヤーの状態を変更するメソッド
        /// </summary>
        /// <param name="player"></param>
        /// <param name="state"></param>
        public void ChangePlayerState(CPlayer player, PLAYER_STATE state)
        {
            if (m_PlayerState.ContainsKey(player.m_PlayerIndex))
            {
                m_PlayerState[player.m_PlayerIndex] = state;
            }
            else
            {
                m_PlayerState.Add(player.m_PlayerIndex, state);
            }
        }

        /// <summary>
        /// 最初に各プレイヤーにアンティを払ってもらう。
        /// </summary>
        /// <param name="playerCount"></param>
        /// <param name="players"></param>
        public void InitBettingModule(int playerCount, CPlayer[] players)
        {
            m_Total = m_Ante * playerCount;

            for (int i = 0; i < MAX_PLAYER; ++i)
            {
                if (players[i] == null)
                    continue;

                players[i].chips -= m_Ante;
            }
        }

        public void FoldEvent(CPlayer[] players, int index)
        {
            ChangePlayerState(players[index], PLAYER_STATE.FOLD);
        }

        public void CheckEvent(CPlayer[] players, int index)
        {
            m_BetChips = 0;
        }

        /// <summary>
        /// 最初にチップを賭ける際に発生するイベント
        /// </summary>
        /// <param name="players">プレイヤーたち</param>
        /// <param name="index">プレイヤーのインデックス</param>
        /// <param name="div">ブリングインの場合は2、コンプリートの場合は1</param>
        public void FirstBetEvent(CPlayer[] players, int index, int div)
        {
            m_RaiseChips = (m_Ante / div);
            m_BetChips = m_RaiseChips;
            players[index].prevBetChips += m_BetChips;
            BetChips(players[index], m_BetChips);
        }

        public void CallEvent(CPlayer[] players, int index)
        {
            m_BetChips = m_RaiseChips - players[index].prevBetChips;
            players[index].prevBetChips += m_BetChips;
            BetChips(players[index], m_BetChips);
        }

        public void RaiseEvent(CPlayer[] players, int index)
        {
            m_RaiseChips += m_Ante;

            m_BetChips = m_RaiseChips - players[index].prevBetChips;
            players[index].prevBetChips += m_BetChips;
            BetChips(players[index], m_BetChips);
        }

        void BetChips(CPlayer player, long chips)
        {
            SetBeforePlayerChips(chips);

            if (player.chips < chips)
            {
                m_Total += player.chips;
                player.chips = 0;
            }
            else
            {
                m_Total += chips;
                player.chips -= chips;
            }
        }

        public void InitPlayerBetChip(CPlayer[] players)
        {
            for (int i = 0; i < MAX_PLAYER; ++i)
            {
                if (players[i] == null)
                    continue;

                players[i].prevBetChips = 0;
            }
        }

        /// <summary>
        /// 賭けられたチップ全部を、優勝したプレイヤーに与える。
        /// </summary>
        /// <param name="player">優勝プレイヤー</param>
        public void SendTotalChipsToWinner(CPlayer player)
        {
            player.chips += m_Total;
            player.m_Owner.possessChips = player.chips;
        }

        /// <summary>
        /// ゲームユーザーのチップをプレイヤー所持チップに合わせる。
        /// </summary>
        /// <param name="players"></param>
        public void AlignChipsToUser(CPlayer[] players)
        {
            for (int i = 0; i < 5; ++i)
            {
                if (players[i] != null)
                    players[i].m_Owner.possessChips = players[i].chips;
            }
        }
    }
}