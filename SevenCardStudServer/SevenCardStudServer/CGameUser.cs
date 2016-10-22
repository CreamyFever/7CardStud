using System;
using FreeNet;

namespace SevenPokerGameServer
{
    // ゲームに接続したユーザーのクラス
    // ユーザーからの要求を受け入れ、現在入っているルームとプレイヤーの情報をクラスメンバーとして持つ。

    /// <summary>
    /// 一つのsessionオブジェクト
    /// </summary>
    public class CGameUser : IPeer
    {
        CUserToken token;
        const int DELEGATE_MAX = 20;

        public CGameRoom gameRoom { get; private set; }
        public CPlayer gamePlayer { get; private set; }
        public long possessChips;

        public delegate void UserDelegate(CPacket packet, CPlayer player);
        UserDelegate[] userMethod;

        public CGameUser(CUserToken _token)         // コンストラクタ
        {
            token = _token;
            token.SetPeer(this);

            possessChips = 500000;
        }

        // Socketのバッファからコピーされた｢CUserToken｣のパッファを参照
        void IPeer.onMessage(Const<byte[]> buffer)
        {
            byte[] clone = new byte[1024];
            Array.Copy(buffer.Value, clone, buffer.Value.Length);
            CPacket msg = new CPacket(clone, this);
            Program.gameMain.EnqueuePacket(msg, this);
        }

        // クライアントとの連結が切れた時に呼び出され、このメソッドが呼び出された以後はデータの送受信が不可能。
        void IPeer.onRemoved()
        {
            if (gameRoom != null)
            {
                gameRoom.ExitGameRoom(this);
            }


            Console.WriteLine("The client disconnected.");

            Program.RemoveUser(this);
        }

        public void Send(CPacket msg)
        {
            token.Send(msg);
        }

        void IPeer.Disconnect()
        {
            token.socket.Disconnect(false);
        }

        /// <summary>
        /// クライアントから取得したメッセージを処理
        /// </summary>
        /// <param name="msg"></param>
        void IPeer.ProcessUserOperation(CPacket msg)
        {
            PROTOCOL protocol = (PROTOCOL)msg.PopProtocolId();
            Console.WriteLine("Protocol ID : " + protocol);

            if (gameRoom != null)
            {
                userMethod = new UserDelegate[]{
                    new UserDelegate(EnterLobbyMethod),
                    new UserDelegate(CreateRoomMethod),
                    new UserDelegate(EnterRoomAutoMethod),
                    new UserDelegate(ExitRoomMethod),
                    new UserDelegate(gameRoom.GetRoomInfo),
                    new UserDelegate(gameRoom.StartToPlay),
                    new UserDelegate(gameRoom.DiscardCard),
                    new UserDelegate(gameRoom.ShowOpenCard),
                    new UserDelegate(gameRoom.ShowCommunityCard),
                    new UserDelegate(gameRoom.DealCard),
                    new UserDelegate(gameRoom.SendCurrentHand),
                    new UserDelegate(gameRoom.BettingChips)
                };
            }
            else
            {
                userMethod = new UserDelegate[]{
                    new UserDelegate(EnterLobbyMethod),
                    new UserDelegate(CreateRoomMethod),
                    new UserDelegate(EnterRoomAutoMethod),
                    new UserDelegate(ExitRoomMethod),
                };
            }

            userMethod[(int)protocol - 1](msg, gamePlayer);
        }

        void EnterLobbyMethod(CPacket message, CPlayer player)
        {
            int count;

            CPacket sendMsg = CPacket.Create((short)PROTOCOL.ENTER_LOBBY_RES);
            Program.gameMain.ResponseEnterLobby(this);
            count = Program.gameMain.GetUserCountInLobby();
            Console.WriteLine(count + " user(s) in Lobby.");

            Send(sendMsg);
            CPacket.Destroy(sendMsg);
        }

        void CreateRoomMethod(CPacket message, CPlayer player)
        {
            int roomIndex = 0;
            long ante = message.PopLong();

            while (true)
            {
                if (Program.gameMain.roomManager.GetRoomsIndex().Contains(roomIndex))
                {
                    roomIndex++;
                }
                else
                {
                    Program.gameMain.ResponseCreateRoom(this, roomIndex, ante);
                    break;
                }
            }
        }

        /// <summary>
        /// ルームが存在しないと、新しくルームを作成して入場する。
        /// </summary>
        /// <param name="message"></param>
        /// <param name="player"></param>
        void EnterRoomAutoMethod(CPacket message, CPlayer player)
        {
            CPacket sendMsg;
            long stake = message.PopLong();

            if (!Program.gameMain.ResponseEnterRoomAutomatically(this, stake))
            {
                Console.WriteLine("All rooms has been occupied or are not exist! Create new room.");
                sendMsg = CPacket.Create((short)PROTOCOL.AUTO_ENTER_NO);
            }
            else
            {
                sendMsg = CPacket.Create((short)PROTOCOL.ENTER_ROOM_OK);
            }
            Send(sendMsg);
            CPacket.Destroy(sendMsg);
        }

        void ExitRoomMethod(CPacket message, CPlayer player)
        {
            int count;

            CPacket sendMsg = CPacket.Create((short)PROTOCOL.EXIT_ROOM_OK);
            Program.gameMain.ResponseExitRoom(this);

            count = Program.gameMain.GetUserCountInLobby();
            Console.WriteLine(count + " user(s) in Lobby.");

            Send(sendMsg);
            CPacket.Destroy(sendMsg);
        }

        public void EnterRoom(CPlayer player, CGameRoom room)
        {
            gamePlayer = player;
            gameRoom = room;
        }

        public void ExitRoom()
        {
            gamePlayer = null;
            gameRoom = null;
        }
    }
}