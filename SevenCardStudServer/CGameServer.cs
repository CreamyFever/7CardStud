using System;
using System.Collections.Generic;
using FreeNet;
using System.Threading;

namespace SevenPokerGameServer
{
    /// <summary>
    /// ゲームサーバーオブジェクト。クライアントの要求に応答する。
    /// </summary>
    class CGameServer
    {
        object operationLock;
        Queue<CPacket> userOperations;

        // ロジックはシングルスレッドで処理
        Thread logicThread;
        AutoResetEvent loopEvent;

        public CGameRoomManager roomManager { get; private set; }       // ゲームルームを管理するマネジャー
        List<CGameUser> lobbyUsers;                                     // ロビーにいるユーザーのリスト

        public CGameServer()
        {
            operationLock = new object();
            userOperations = new Queue<CPacket>();                      // メッセージキュー

            logicThread = new Thread(GameLoop);
            loopEvent = new AutoResetEvent(false);

            roomManager = new CGameRoomManager();
            lobbyUsers = new List<CGameUser>();

            logicThread.Start();
        }

        /// <summary>
        /// ユーザーから取得したパケットを処理するループ
        /// </summary>
        void GameLoop()
        {
            while (true)
            {
                CPacket packet = null;
                lock (operationLock)
                {
                    if (userOperations.Count > 0)
                    {
                        packet = userOperations.Dequeue();
                    }
                }

                if (packet != null)
                {
                    // パケットを処理
                    ProcessReceive(packet);
                }

                if (userOperations.Count <= 0)       // キューに処理すべきのパケットがないならスレッドを待機状態にする。
                {
                    loopEvent.WaitOne();
                }
            }
        }

        public void EnqueuePacket(CPacket packet, CGameUser user)
        {
            lock (operationLock)
            {
                userOperations.Enqueue(packet);
                loopEvent.Set();
            }
        }

        void ProcessReceive(CPacket msg)
        {
            msg.owner.ProcessUserOperation(msg);
        }

        // ユーザーとの連結が切れた時に呼び出される
        public void UserDisconnected(CGameUser user)
        {
            if (lobbyUsers.Contains(user))
            {
                lobbyUsers.Remove(user);
            }
        }

        public int GetUserCountInLobby()
        {
            return lobbyUsers.Count;
        }

        public void ResponseEnterLobby(CGameUser user)
        {
            if (lobbyUsers.Contains(user))
            {
                return;
            }
            else
            {
                lobbyUsers.Add(user);
            }
        }

        public void ResponseCreateRoom(CGameUser user, int roomIndex, long stake)
        {
            roomManager.CreateRoom(user, roomIndex, stake);
            lobbyUsers.Remove(user);
        }

        public bool ResponseEnterRoomAutomatically(CGameUser user, long stake)
        {
            bool isOccupied = roomManager.EnterRoomAutomatically(user, stake);

            lobbyUsers.Remove(user);

            return isOccupied;
        }

        public void ResponseExitRoom(CGameUser user)
        {
            roomManager.ExitRoom(user);
            lobbyUsers.Add(user);
        }
    }
}