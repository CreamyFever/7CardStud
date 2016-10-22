using System;
using System.Collections.Generic;
using FreeNet;

namespace SevenPokerGameServer
{
    /// <summary>
    /// サーバープログラムのメイン
    /// </summary>
    class Program
    {
        static List<CGameUser> userList;
        public static CGameServer gameMain = new CGameServer();

        static void Main(string[] args)
        {
            CPacketBufferManager.Initialize(2000);
            userList = new List<CGameUser>();

            CNetworkService service = new CNetworkService();
            service.sessionCreatedCallback += onSessionCreated;

            service.Initialize();
            service.Listen("0.0.0.0", 5000, 100);

            Console.WriteLine("Server Started!");
            while (true)
            {
                string input = Console.ReadLine();
                System.Threading.Thread.Sleep(1000);
            }

            Console.ReadKey();
        }

        /// <summary>
		/// クライアントの接続が完了したことを確認して呼び出される。
		/// n個のワーカースレッドから呼び出される可能性があるので、共有資源に接近する際に同期化が必要。
		/// </summary>
		/// <returns></returns>
        static void onSessionCreated(CUserToken token)
        {
            CGameUser user = new CGameUser(token);
            lock (userList)
            {
                userList.Add(user);
            }
        }

        public static void RemoveUser(CGameUser user)
        {
            lock (userList)
            {
                userList.Remove(user);
                gameMain.UserDisconnected(user);

                CGameRoom room = user.gameRoom;

                if (room != null)
                {
                    gameMain.roomManager.RemoveRoom(user.gameRoom);
                }
            }
        }
    }
}