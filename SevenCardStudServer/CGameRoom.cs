using System;
using System.Collections.Generic;
using System.Timers;
using FreeNet;

namespace SevenPokerGameServer
{
    public class CGameRoom
    {
        public int m_RoomIndex;
        public static int MAX_PLAYER = 5;
        public long m_Ante;

        Timer m_Timer = new Timer(1000);
        int sec = 0;

        CGameRoomManager roomManager;

        Stack<int> m_Index;                                     // インデックス(0 ~ 4)を保持するスタック
        CPlayer[] m_Players;                                    // ゲームを進めるプレイヤー(ルームの最大プレイヤー数は5人で固定してあるので配列で宣言)

        CDeck m_Deck;
        CBettingModule m_BettingModule;

        int callCount;

        int m_BettingCount;

        CPlayer hostPlayer;
        CPlayer bossPlayer;
        int beforeTurn;
        int currentTurn;

        int currentStreet;                                      // 現在のストリート(フォースストリート、フィフスストリートなど)


        private byte PlayerIndexIsOccupied(int index)
        {
            if (m_Players[index] != null && !(m_BettingModule.isPlayerState(index, PLAYER_STATE.FOLD) || m_BettingModule.isPlayerState(index, PLAYER_STATE.OBSERVE)))
                return 1;
            else
                return 0;
        }


        public int GetPlayersCount()
        {
            int playerCount = 0;
            for (int i = 0; i < MAX_PLAYER; ++i)
            {
                if (m_Players[i] != null && !(m_BettingModule.isPlayerState(i, PLAYER_STATE.FOLD) || m_BettingModule.isPlayerState(i, PLAYER_STATE.OBSERVE)))
                    playerCount++;
            }

            return playerCount;
        }

        public CGameRoom(int roomIndex, CGameUser user, long ante)
        {
            m_Timer.Interval = 1000;
            m_Timer.Elapsed += new ElapsedEventHandler(TimerElapsed);

            roomManager = Program.gameMain.roomManager;
            m_RoomIndex = roomIndex;
            m_Players = new CPlayer[MAX_PLAYER];
            m_Ante = ante;

            m_Index = new Stack<int>();

            // インデックスをスタックに順番に入れて、Topが0になるようにする。
            // ユーザーがルームに入るたびにスタックにあるインデックスをPop()を行って0から与えられるようにする。
            for (int index = MAX_PLAYER - 1; index >= 0; --index)
            {
                m_Index.Push(index);
            }

            m_Deck = new CDeck();
            m_Deck.SetUpDeck();

            m_BettingModule = new CBettingModule(m_Ante);

            callCount = 0;

            roomManager.ChangeRoomState(this, GAME_STATE.WAITING);

            EnterGameRoom(user);
        }

        /// <summary>
        /// 1秒刻みに呼び出されるメソッド
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            sec += 1;

            Console.WriteLine("Room No {0}'s Elapsed Time : {1}", m_RoomIndex, sec);

            if (m_BettingModule.isAllReady(m_Players, PLAYER_STATE.OPEN_A_CARD))
            {
                ShowCommunityCard(null, null);
            }

            // 10秒経ってもクライアントからリクエストが来なかったら呼び出す。
            if (sec >= 10.0f)
            {
                for (int i = 0; i < MAX_PLAYER; ++i)
                {
                    if (m_Players[i] != null)
                    {
                        if (m_BettingModule.isPlayerState(i, PLAYER_STATE.READY))
                        {
                            DiscardCardTimeOver(m_Players[i]);
                            sec = 0;
                        }

                        else if (m_BettingModule.isPlayerState(i, PLAYER_STATE.DISCARD_CARD))
                        {
                            ShowOpenCardTimeOver(m_Players[i]);
                            sec = 0;
                        }
                    }
                }

                if (m_BettingModule.isPlayerState(currentTurn, PLAYER_STATE.DEAL_CARD))
                {
                    BettingChipsTimeOver(m_Players[currentTurn]);
                }
            }
        }


        /// <summary>
        /// 満席なのか確認
        /// </summary>
        public bool isFull()
        {
            if (GetPlayersCount() < MAX_PLAYER)
            {
                return false;
            }

            else
            {
                return true;
            }
        }

        /// <summary>
        /// ユーザーがルームに入場
        /// </summary>
        /// <param name="user"></param>
        public void EnterGameRoom(CGameUser user)
        {
            // 他のプレイヤーの入場を知らせるパケットを作る。
            CPacket msg = CPacket.Create((short)PROTOCOL.PLAYER_ENTER_ROOM);

            // 入場したプレイヤーにインデックスを与える。
            int playerIndex = m_Index.Pop();
            CPlayer player = new CPlayer(user, playerIndex);

            // ホストの権限を授与
            if (GetPlayersCount() == 0)
            {
                hostPlayer = player;
            }

            // プレイヤーを配列に。
            if (m_Players[playerIndex] == null)
                m_Players[playerIndex] = player;

            msg.Push(player.m_PlayerIndex);
            msg.Push(player.chips);
            Broadcast(msg);

            // ユーザーがプレイヤーとしてルームに入る。
            user.EnterRoom(player, this);

            // プレイヤーの状態を変更
            if (roomManager.isGameRoomState(user.gameRoom.m_RoomIndex, GAME_STATE.WAITING))
            {
                m_BettingModule.ChangePlayerState(player, PLAYER_STATE.READY);

                if (player.m_PlayerIndex == hostPlayer.m_PlayerIndex)
                {
                    m_BettingModule.ChangePlayerState(player, PLAYER_STATE.NOT_READY);
                }
            }
            else if (roomManager.isGameRoomState(user.gameRoom.m_RoomIndex, GAME_STATE.PLAYING))
            {
                m_BettingModule.ChangePlayerState(player, PLAYER_STATE.OBSERVE);
            }

            // 入場したルームのデータを送信。
            msg = CPacket.Create((short)PROTOCOL.ENTER_ROOM_OK);
            user.Send(msg);
            CPacket.Destroy(msg);
        }

        /// <summary>
        /// ユーザーがルームを出る時に呼び出す。
        /// </summary>
        /// <param name="user"></param>
        /// TODO : 
        public void ExitGameRoom(CGameUser user)
        {
            CPacket msg = CPacket.Create((short)PROTOCOL.PLAYER_EXIT_ROOM);
            int hostIndex = hostPlayer.m_PlayerIndex;

            msg.Push(user.gamePlayer.m_PlayerIndex);

            // ルームを出たプレイヤーがホストだった場合、次のプレイヤーにホストの権限を譲渡。
            if (hostIndex == user.gamePlayer.m_PlayerIndex)
            {
                NextIndex(user.gamePlayer.m_PlayerIndex, ref hostIndex);
                hostPlayer = m_Players[hostIndex];
                m_BettingModule.ChangePlayerState(m_Players[hostIndex], PLAYER_STATE.NOT_READY);
            }

            Console.WriteLine("Players Count : " + GetPlayersCount());

            msg.Push(hostIndex);
            Broadcast(msg);

            m_Index.Push(user.gamePlayer.m_PlayerIndex);
            m_BettingModule.RemovePlayerState(user);

            m_Players[user.gamePlayer.m_PlayerIndex] = null;
        }

        /// <summary>
        /// 初めてゲームルームに入場した時にルームの情報を取得
        /// </summary>
        /// <param name="message"></param>
        /// <param name="player"></param>
        public void GetRoomInfo(CPacket message, CPlayer player)
        {
            CCard card;
            byte occupied;
            CPacket msg = CPacket.Create((short)PROTOCOL.GAME_ROOM_INFO_RES);
            msg.Push(hostPlayer.m_PlayerIndex);
            msg.Push(player.m_PlayerIndex);
            msg.Push(player.chips);
            msg.Push(currentStreet);

            for (int i = 0; i < MAX_PLAYER; ++i)
            {
                if (m_Players[i] == null)
                {
                    msg.Push(-1);
                    msg.Push(0L);
                }
                else if (m_Players[i].m_PlayerIndex == player.m_PlayerIndex)
                    continue;
                else
                {
                    msg.Push(m_Players[i].m_PlayerIndex);
                    msg.Push(m_Players[i].chips);
                }
            }

            for (int i = 0; i < MAX_PLAYER; ++i)
            {
                occupied = PlayerIndexIsOccupied(i);

                if (occupied != 0)
                {
                    for (int num = 0; num < currentStreet; ++num)
                    {
                        {
                            card = m_Players[i].hand[num];
                            msg.Push(card.Suit);
                            msg.Push(card.Rank);

                            Console.WriteLine("Player " + m_Players[i].m_PlayerIndex + " draws " + (CARD_SUIT)card.Suit + " " + (CARD_RANK)card.Rank + ".");
                        }
                    }

                    Console.WriteLine("Info!!");
                }
            }

            player.Send(msg);
            CPacket.Destroy(msg);
        }

        public void StartToPlay(CPacket message, CPlayer player)
        {
            m_BettingModule.ChangePlayerState(player, PLAYER_STATE.READY);

            if (GetPlayersCount() > 1 && m_BettingModule.isAllReady(m_Players, PLAYER_STATE.READY))
            {
                CPacket msg = CPacket.Create((short)PROTOCOL.GAME_START_RES);

                Broadcast(msg);

                GameStart();
            }
        }

        public void GameStart()
        {
            roomManager.ChangeRoomState(this, GAME_STATE.PLAYING);
            m_Timer.Start();

            Console.WriteLine("Game Start!");
            m_Deck.ShuffleDeck();
            m_BettingModule.InitBettingModule(GetPlayersCount(), m_Players);

            callCount = GetPlayersCount() - 1;
            currentStreet = 3;

            byte occupied = 0;

            CPacket msg = CPacket.Create((short)PROTOCOL.DEAL_FIRST_CARD_RES);

            msg.Push(m_BettingModule.Total);

            // ゲームを始めて、カードを4枚ずつ配る。
            for (int i = 0; i < MAX_PLAYER; ++i)
            {
                occupied = PlayerIndexIsOccupied(i);
                msg.Push(occupied);                     // インデックス(0~4)の間に空席(null)があることを知らせるため送信。(ex) ルームの中に0、1、4がいて、2、3が空席の場合。

                if (occupied != 0)
                {
                    msg.Push(m_Players[i].chips);
                    m_Players[i].ResetHand();
                    m_Deck.InitEvaluator(m_Players[i]);

                    for (int num = 0; num < 4; ++num)
                    {
                        {
                            CCard card = m_Deck.DealCard(m_Players[i], false);
                            msg.Push(card.Suit);
                            msg.Push(card.Rank);

                            Console.WriteLine("Player " + m_Players[i].m_PlayerIndex + " draws " + (CARD_SUIT)card.Suit + " " + (CARD_RANK)card.Rank + ".");
                        }
                    }
                }
            }

            Broadcast(msg);
        }

        /// <summary>
        /// 4枚のカードの中で1枚捨てる。
        /// </summary>
        /// <param name="message"></param>
        /// <param name="player"></param>
        public void DiscardCard(CPacket message, CPlayer player)
        {
            CPacket msg;

            //クライアントから受け取った｢捨てるカード｣のインデックス
            int index = message.PopInt();

            Console.WriteLine("Player " + player.m_PlayerIndex + " discarded " + (CARD_SUIT)player.hand[index].Suit + " " + (CARD_RANK)player.hand[index].Rank + ".");

            m_BettingModule.ChangePlayerState(player, PLAYER_STATE.DISCARD_CARD);

            player.DiscardCard(index);


            msg = CPacket.Create((short)PROTOCOL.BROADCAST_DISCARD_CARD);
            msg.Push(player.m_PlayerIndex);
            msg.Push(index);
            Broadcast(msg);
        }

        /// <summary>
        /// 時間切れになったらカード1枚を捨てる。
        /// </summary>
        /// <param name="player"></param>
        public void DiscardCardTimeOver(CPlayer player)
        {
            CPacket msg;

            if (player.hand.Count == 4)
            {
                m_BettingModule.ChangePlayerState(player, PLAYER_STATE.DISCARD_CARD);
                player.DiscardCard(0);

                msg = CPacket.Create((short)PROTOCOL.BROADCAST_DISCARD_CARD);
                msg.Push(player.m_PlayerIndex);
                msg.Push(0);
                Broadcast(msg);
            }
        }

        /// <summary>
        /// 表向きにするカード
        /// </summary>
        /// <param name="message"></param>
        /// <param name="player"></param>
        public void ShowOpenCard(CPacket message, CPlayer player)
        {
            CPacket msg;
            CCard card;

            // クライアントから受け取った｢表向きにするカード｣のインデックス
            int index = message.PopInt();
            card = new CCard((CARD_SUIT)player.hand[index].Suit, (CARD_RANK)player.hand[index].Rank);

            Console.WriteLine("Player " + player.m_PlayerIndex + " has chosen the community card " + (CARD_SUIT)player.hand[index].Suit + " " + (CARD_RANK)player.hand[index].Rank + ".");

            // 表向きにするカードを一番後ろに移動させる。
            player.TurnCardToVeryLast(card, index);

            m_BettingModule.ChangePlayerState(player, PLAYER_STATE.OPEN_A_CARD);

            msg = CPacket.Create((short)PROTOCOL.BROADCAST_OPEN_CARD);
            msg.Push(player.m_PlayerIndex);
            msg.Push(index);
            Broadcast(msg);
        }

        /// <summary>
        /// 時間切れになったらカード1枚を表向きにする。
        /// </summary>
        /// <param name="player"></param>
        public void ShowOpenCardTimeOver(CPlayer player)
        {
            CPacket msg;
            CCard card;

            card = new CCard((CARD_SUIT)player.hand[0].Suit, (CARD_RANK)player.hand[0].Rank);

            if (player.hand.Count == 3)
            {
                m_BettingModule.ChangePlayerState(player, PLAYER_STATE.OPEN_A_CARD);
                player.TurnCardToVeryLast(card, 0);

                msg = CPacket.Create((short)PROTOCOL.BROADCAST_OPEN_CARD);
                msg.Push(player.m_PlayerIndex);
                msg.Push(0);
                Broadcast(msg);
            }
        }

        /// <summary>
        /// 表向きにするカード
        /// </summary>
        /// <param name="player"></param>
        public void ShowCommunityCard(CPacket message, CPlayer player)
        {
            CPacket msg;
            CCard card;
            byte occupied;

            // 全てのプレイヤーが表向きにするカードを選んだか確認
            if (GetPlayersCount() > 1 && m_BettingModule.isAllReady(m_Players, PLAYER_STATE.OPEN_A_CARD))
            {
                msg = CPacket.Create((short)PROTOCOL.SHOW_A_CARD_ON_HAND_RES);

                // 表向きにするカードが一番後ろになったカードリストをクライアントにブロードキャスト
                for (int i = 0; i < MAX_PLAYER; ++i)
                {
                    occupied = PlayerIndexIsOccupied(i);
                    msg.Push(occupied);                     // インデックス(0~4)の間に空席(null)があることを知らせるため送信。

                    if (occupied != 0)
                    {
                        m_BettingModule.ChangePlayerState(m_Players[i], PLAYER_STATE.READY_FOR_BETTING_CHIPS);

                        for (int num = 0; num < 3; ++num)
                        {
                            {
                                card = m_Players[i].hand[num];
                                msg.Push(card.Suit);
                                msg.Push(card.Rank);

                                Console.WriteLine("Player " + m_Players[i].m_PlayerIndex + " draws " + (CARD_SUIT)card.Suit + " " + (CARD_RANK)card.Rank + ".");
                            }
                        }
                    }
                }

                Broadcast(msg);

                for (int i = 0; i < MAX_PLAYER; ++i)
                {
                    if (m_Players[i] != null)
                    {
                        EvaluateSortedCards(m_Players[i]);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// カードリストをソーティングした後、役を判別。
        /// </summary>
        void EvaluateSortedCards(CPlayer player)
        {
            CPacket msg;
            byte occupied;

            msg = CPacket.Create((short)PROTOCOL.EVALUATE_COMMUNITY_CARDS);
            for (int i = 0; i < MAX_PLAYER; ++i)
            {
                occupied = PlayerIndexIsOccupied(i);
                msg.Push(occupied);

                if (occupied != 0)
                {
                    msg.Push(m_Players[i].hand.Count - 1);

                    m_Deck.SortCards(m_Players[i].handToSort);
                    m_Players[i].handInfo = m_Deck.EvaluateHand(m_Players[i].handToSort);

                    m_Deck.SortCards(m_Players[i].communityCards);
                    m_Players[i].communityInfo = m_Deck.EvaluateHand(m_Players[i].communityCards);
                }
            }

            Broadcast(msg);
            DetermineBoss(player);
        }

        /// <summary>
        /// ボス(親)を決めるメソッド
        /// </summary>
        void DetermineBoss(CPlayer player)
        {
            HAND highestHand = 0;
            int highestRank = 0;
            for (int i = 0; i < MAX_PLAYER; ++i)
            {
                if (m_Players[i] == null || (m_BettingModule.isPlayerState(i, PLAYER_STATE.FOLD) || m_BettingModule.isPlayerState(i, PLAYER_STATE.OBSERVE)))
                    continue;

                if (m_Players[i].communityInfo.currentHand == highestHand)
                {
                    if (m_Players[i].communityInfo.highRank > highestRank)
                    {
                        highestRank = m_Players[i].communityInfo.highRank;
                        bossPlayer = m_Players[i];
                        currentTurn = bossPlayer.m_PlayerIndex;
                    }
                }

                else if (m_Players[i].communityInfo.currentHand > highestHand)
                {
                    highestRank = m_Players[i].handInfo.highRank;
                    highestHand = m_Players[i].communityInfo.currentHand;
                    bossPlayer = m_Players[i];
                    currentTurn = bossPlayer.m_PlayerIndex;
                }

                m_BettingModule.ChangePlayerState(m_Players[i], PLAYER_STATE.DEAL_CARD);
            }

            Console.WriteLine("Boss is Player " + bossPlayer.m_PlayerIndex + ".");
            Console.WriteLine("Player HandCount = " + player.hand.Count);

            if (player.hand.Count <= 3)
            {
                DealCard(null, player);
            }
            else
            {
                ReadyToBet(player);
            }
            sec = 0;
        }

        /// <summary>
        /// ボスから時計回りにカードを配る。
        /// </summary>
        public void DealCard(CPacket message, CPlayer player)
        {
            int dealIndex;                                  // カードが配られるプレイヤーのインデックス
            int cardCount = player.hand.Count;
            byte occupied = 0;
            bool community = true;

            if (player.hand.Count >= 6)
                community = false;

            CPacket msg = CPacket.Create((short)PROTOCOL.DEAL_CARD_RES);
            msg.Push(bossPlayer.m_PlayerIndex);

            m_BettingModule.InitPlayerBetChip(m_Players);

            if (GetPlayersCount() > 1 && m_BettingModule.isAllReady(m_Players, PLAYER_STATE.DEAL_CARD))
            {
                for (int i = 0; i < MAX_PLAYER; ++i)
                {
                    dealIndex = i + bossPlayer.m_PlayerIndex;

                    if (dealIndex >= MAX_PLAYER)
                    {
                        dealIndex -= MAX_PLAYER;
                    }

                    occupied = PlayerIndexIsOccupied(dealIndex);
                    msg.Push(occupied);                     // インデックス(0~4)の間に空席(null)があることを知らせるため送信。

                    if (occupied != 0)
                    {
                        CCard card = m_Deck.DealCard(m_Players[dealIndex], community);
                        msg.Push(card.Suit);
                        msg.Push(card.Rank);

                        if (m_Players[dealIndex].hand.Count == 7)
                        {
                            m_Players[dealIndex].bettingCount = 3;
                        }
                        else if (m_Players[dealIndex].hand.Count == 6)
                        {
                            m_Players[dealIndex].bettingCount = 2;
                        }
                        else
                        {
                            m_Players[dealIndex].bettingCount = 1;
                        }

                        Console.WriteLine("Player " + m_Players[dealIndex].m_PlayerIndex + " draws " + (CARD_SUIT)card.Suit + " " + (CARD_RANK)card.Rank + ".");
                    }

                    if (bossPlayer.m_PlayerIndex - 1 == dealIndex)
                        break;
                }
                Broadcast(msg);

                callCount = GetPlayersCount() - 1;
                m_BettingModule.RaiseChips = 0;
                m_BettingCount = 0;
                EvaluateSortedCards(player);

                currentStreet++;
            }
        }

        /// <summary>
        /// ベッティング
        /// </summary>
        /// <param name="message"></param>
        /// <param name="player"></param>
        public void BettingChips(CPacket message, CPlayer player)
        {
            CPacket msg;
            player.betIndex = message.PopByte();
            int bossIndex = bossPlayer.m_PlayerIndex;
            beforeTurn = currentTurn;
            sec = 0;

            Console.WriteLine("Total : " + m_BettingModule.Total);

            switch ((BET)player.betIndex)
            {
                // フォールド
                case BET.Fold:
                    callCount -= 1;
                    m_BettingModule.FoldEvent(m_Players, player.m_PlayerIndex);

                    if (m_BettingCount == 0)
                    {
                        NextIndex(player.m_PlayerIndex, ref bossIndex);
                        bossPlayer = m_Players[bossIndex];
                    }
                    Console.WriteLine("Player " + player.m_PlayerIndex + " fold.");
                    break;

                // チェック
                case BET.Check:
                    callCount -= 1;
                    m_BettingModule.CheckEvent(m_Players, player.m_PlayerIndex);
                    player.bettingCount = 0;
                    Console.WriteLine("Player " + player.m_PlayerIndex + " check.");
                    break;


                // ブリングイン
                case BET.BringIn:
                    callCount = GetPlayersCount() - 1;
                    m_BettingModule.FirstBetEvent(m_Players, player.m_PlayerIndex, 2);
                    m_BettingCount++;
                    player.bettingCount -= 1;
                    Console.WriteLine("Player " + player.m_PlayerIndex + " bring-in.");
                    break;

                // コール
                case BET.Call:
                    callCount -= 1;
                    m_BettingModule.CallEvent(m_Players, player.m_PlayerIndex);
                    player.bettingCount = 0;
                    Console.WriteLine("Player " + player.m_PlayerIndex + " call.");
                    break;

                // ベット(コンプリート)
                case BET.Bet:
                case BET.Complete:
                    callCount = GetPlayersCount() - 1;
                    m_BettingModule.FirstBetEvent(m_Players, player.m_PlayerIndex, 1);
                    m_BettingCount++;
                    player.bettingCount -= 1;
                    Console.WriteLine("Player " + player.m_PlayerIndex + " complete.");
                    break;

                // レイズ
                case BET.Raise:
                    callCount = GetPlayersCount() - 1;
                    m_BettingModule.RaiseEvent(m_Players, player.m_PlayerIndex);
                    m_BettingCount++;
                    player.bettingCount -= 1;
                    Console.WriteLine("Player " + player.m_PlayerIndex + " half.");
                    break;

                default:
                    break;
            }

            Console.WriteLine("Current Boss Player : Player " + bossPlayer.m_PlayerIndex);

            msg = CPacket.Create((short)PROTOCOL.BETTING_RES);
            msg.Push(beforeTurn);
            msg.Push(player.betIndex);

            if (callCount != 0)
                NextIndex(player.m_PlayerIndex, ref currentTurn);

            msg.Push(DetermineClientButtonActive());

            Console.WriteLine("Raised Chips : " + m_BettingModule.RaiseChips);
            Console.WriteLine("Bet : " + m_BettingModule.BeforePlayerBet);
            Console.WriteLine("Total after betting : " + m_BettingModule.Total);
            Console.WriteLine("---------------------------");
            for (int i = 0; i < MAX_PLAYER; ++i)
            {
                if (m_Players[i] != null)
                    Console.WriteLine("Player " + m_Players[i].m_PlayerIndex + " : " + m_Players[i].chips);
            }

            msg.Push(currentTurn);
            msg.Push(m_BettingModule.Total);
            msg.Push(m_BettingModule.RaiseChips);
            msg.Push(player.chips);
            msg.Push(m_BettingModule.BetChip);
            Broadcast(msg);

            if (callCount == 0)
            {
                if (player.hand.Count < 7 && GetPlayersCount() > 1)
                    DealCard(null, player);
                else
                    ShowResult();
                return;
            }
        }

        public void BettingChipsTimeOver(CPlayer player)
        {
            CPacket msg;
            int bossIndex = bossPlayer.m_PlayerIndex;
            beforeTurn = currentTurn;
            sec = 0;

            Console.WriteLine("Total : " + m_BettingModule.Total);

            Console.WriteLine("Current Boss Player : Player " + bossPlayer.m_PlayerIndex);

            msg = CPacket.Create((short)PROTOCOL.BETTING_RES);
            msg.Push(beforeTurn);

            if (m_BettingModule.RaiseChips == 0)
            {
                callCount = GetPlayersCount() - 1;
                m_BettingModule.FirstBetEvent(m_Players, player.m_PlayerIndex, 2);
                m_BettingCount++;
                player.bettingCount -= 1;
                Console.WriteLine("Player " + player.m_PlayerIndex + " bring-in. Not responsed.");
                msg.Push((byte)BET.BringIn);
            }
            else
            {
                callCount -= 1;
                m_BettingModule.FoldEvent(m_Players, player.m_PlayerIndex);

                if (m_BettingCount == 0)
                {
                    NextIndex(player.m_PlayerIndex, ref bossIndex);
                    bossPlayer = m_Players[bossIndex];
                }
                Console.WriteLine("Player " + player.m_PlayerIndex + " fold. Not responsed.");
                msg.Push((byte)BET.Fold);
            }

            if (callCount != 0)
                NextIndex(player.m_PlayerIndex, ref currentTurn);

            msg.Push(DetermineClientButtonActive());

            Console.WriteLine("Raised Chips : " + m_BettingModule.RaiseChips);
            Console.WriteLine("Bet : " + m_BettingModule.BeforePlayerBet);
            Console.WriteLine("Total after betting : " + m_BettingModule.Total);
            Console.WriteLine("---------------------------");
            for (int i = 0; i < MAX_PLAYER; ++i)
            {
                if (m_Players[i] != null)
                    Console.WriteLine("Player " + m_Players[i].m_PlayerIndex + " : " + m_Players[i].chips);
            }

            msg.Push(currentTurn);
            msg.Push(m_BettingModule.Total);
            msg.Push(m_BettingModule.RaiseChips);
            msg.Push(player.chips);
            msg.Push(m_BettingModule.BetChip);
            Broadcast(msg);

            if (callCount == 0)
            {
                if (player.hand.Count < 7 && GetPlayersCount() > 1)
                    DealCard(null, player);
                else
                    ShowResult();
                return;
            }
        }

        /// <summary>
        /// クライアントに表示されるボタンを場合によって、活性化させるメソッド
        /// </summary>
        byte DetermineClientButtonActive()
        {
            // 一つのストリートで賭けられたチップがない場合
            if (m_BettingModule.RaiseChips == 0)
            {
                // 現在、フォースストリートの場合
                if (currentStreet == 3)
                {
                    return 0;
                }
                else
                {
                    // 賭けられるチップがない場合
                    if (m_Players[currentTurn].chips == 0)
                        return 2;
                    else
                        return 1;
                }
            }

            else
            {
                // 賭けられるチップがない場合
                if (m_Players[currentTurn].chips == 0)
                    return 4;
                else
                    return 3;
            }
        }

        void ReadyToBet(CPlayer player)
        {
            CPacket msg;

            msg = CPacket.Create((short)PROTOCOL.READY_TO_BET);
            msg.Push(bossPlayer.m_PlayerIndex);
            msg.Push(DetermineClientButtonActive());
            Console.WriteLine("Player " + player.m_PlayerIndex + "'s handInfo : " + player.handInfo.currentHand);
            Console.WriteLine("Player " + player.m_PlayerIndex + "'s communityInfo : " + player.communityInfo.currentHand);

            Broadcast(msg);
        }

        public void SendCurrentHand(CPacket message, CPlayer player)
        {
            int myIndex = message.PopInt();

            CPacket msg;
            msg = CPacket.Create((short)PROTOCOL.CURRENT_HAND);
            msg.Push((short)m_Players[myIndex].handInfo.currentHand);
            player.Send(msg);
        }

        /// <summary>
        /// ゲームの結果を見せるメソッド
        /// </summary>
        void ShowResult()
        {
            CPacket msg;
            CCard card;
            byte occupied;
            int count = GetPlayersCount();

            roomManager.ChangeRoomState(this, GAME_STATE.WAITING);

            for (int i = 0; i < MAX_PLAYER; i++)
            {
                // 空席やフォールドを宣言したプレイヤー、観戦は除く。
                if (m_Players[i] != null && !(m_BettingModule.isPlayerState(i, PLAYER_STATE.FOLD) || m_BettingModule.isPlayerState(i, PLAYER_STATE.OBSERVE)))
                    Console.WriteLine("Player " + m_Players[i].m_PlayerIndex + "'s handInfo : " + m_Players[i].handInfo.currentHand + ", HighRank : " + m_Players[i].handInfo.highRank);
            }

            msg = CPacket.Create((short)PROTOCOL.SHOW_RESULT);
            m_Timer.Stop();
            sec = 0;

            DetermineWinner();

            msg.Push(bossPlayer.m_PlayerIndex);
            msg.Push((short)bossPlayer.handInfo.currentHand);
            msg.Push(count);

            for (int i = 0; i < MAX_PLAYER; ++i)
            {
                occupied = PlayerIndexIsOccupied(i);
                msg.Push(occupied);

                if (occupied != 0)
                {
                    msg.Push((short)m_Players[i].handInfo.currentHand);
                    msg.Push(m_Players[i].handInfo.highRank);
                }
            }


            // フォールドを宣言したプレイヤーのカードを除いて、全てのカードを表向きにする。
            if (count > 1)
            {
                for (int i = 0; i < MAX_PLAYER; ++i)
                {
                    occupied = PlayerIndexIsOccupied(i);
                    msg.Push(occupied);                     // インデックス(0~4)の間に空席(null)があることを知らせるため送信。

                    if (occupied != 0)
                    {
                        for (int num = 0; num < 7; ++num)
                        {
                            {
                                card = m_Players[i].handToSort[num];
                                msg.Push(card.Suit);
                                msg.Push(card.Rank);

                                Console.WriteLine("Player " + m_Players[i].m_PlayerIndex + " draws " + (CARD_SUIT)card.Suit + " " + (CARD_RANK)card.Rank + ".");
                            }
                        }
                    }
                }
            }

            Console.WriteLine("Total after betting : " + m_BettingModule.Total);
            Console.WriteLine("---------------------------");
            for (int i = 0; i < MAX_PLAYER; ++i)
            {
                if (m_Players[i] != null)
                {
                    m_BettingModule.ChangePlayerState(m_Players[i], PLAYER_STATE.READY);

                    if (m_Players[i].m_PlayerIndex == hostPlayer.m_PlayerIndex)
                    {
                        m_BettingModule.ChangePlayerState(m_Players[i], PLAYER_STATE.NOT_READY);
                    }
                    Console.WriteLine("Player " + m_Players[i].m_PlayerIndex + " : " + m_Players[i].chips);
                }
            }

            Broadcast(msg);
        }

        /// <summary>
        /// 役を判別して勝者を決めるメソッド
        /// </summary>
        void DetermineWinner()
        {
            HAND highestHand = 0;
            int highestRank = 0;
            for (int i = 0; i < MAX_PLAYER; ++i)
            {
                // 空席やフォールドを宣言したプレイヤー、観戦は除く。
                if (m_Players[i] == null || (m_BettingModule.isPlayerState(i, PLAYER_STATE.FOLD) || m_BettingModule.isPlayerState(i, PLAYER_STATE.OBSERVE)))
                    continue;

                if (m_Players[i].handInfo.currentHand == highestHand)
                {
                    if (m_Players[i].handInfo.highRank > highestRank)
                    {
                        highestRank = m_Players[i].handInfo.highRank;
                        bossPlayer = m_Players[i];
                        currentTurn = bossPlayer.m_PlayerIndex;
                    }
                }

                else if (m_Players[i].handInfo.currentHand > highestHand)
                {
                    highestRank = m_Players[i].handInfo.highRank;
                    highestHand = m_Players[i].handInfo.currentHand;
                    bossPlayer = m_Players[i];
                    currentTurn = bossPlayer.m_PlayerIndex;
                }
            }
            
            currentStreet = 0;
            m_BettingModule.SendTotalChipsToWinner(bossPlayer);
            m_BettingModule.AlignChipsToUser(m_Players);

            Console.WriteLine("Winner is Player " + bossPlayer.m_PlayerIndex + "!!!");
        }

        /// <summary>
        /// 次のターンを決めるメソッド
        /// </summary>
        /// <param name="turn"></param>
        void NextIndex(int turn, ref int nextIndex)
        {
            if (m_Players[turn].m_PlayerIndex == nextIndex)
            {
                if (nextIndex == MAX_PLAYER - 1)
                {
                    nextIndex = 0;
                }
                else
                {
                    nextIndex += 1;
                }
            }

            while (true)
            {
                // 空席やフォールドを宣言したプレイヤー、観戦は除く。
                if (m_Players[nextIndex] == null || (m_BettingModule.isPlayerState(nextIndex, PLAYER_STATE.FOLD)
                    || m_BettingModule.isPlayerState(nextIndex, PLAYER_STATE.OBSERVE)))
                {
                    if (nextIndex == MAX_PLAYER - 1)
                    {
                        nextIndex = 0;
                    }
                    else
                    {
                        nextIndex += 1;
                    }
                    continue;
                }
                else
                    break;
            }
        }

        /// <summary>
        /// ルーム内にいるプレイヤーたちにブロードキャスト
        /// </summary>
        /// <param name="msg"></param>
        void Broadcast(CPacket msg)
        {
            foreach (CPlayer player in m_Players)
            {
                if (player != null)
                    player.SendForBroadcasting(msg);
            }

            CPacket.Destroy(msg);
        }
    }
}