using UnityEngine;
using System;
using System.Net;
using FreeNet;

namespace FreeNetUnity
{
    /// <summary>
    /// CSharpServerにUNITYアプリケーションを繋ぐクラス
    /// CSharpServerから取得した接続イベント、メッセージ受信イベントなどをアプリケーションに伝達する役目
    /// MonoBehaviourを継承し、UNITYアプリケーションと同じスレッドで動くように実装
    /// </summary>
    public class FreeNetUnityService : MonoBehaviour
    {
        FreeNetEventManager eventManager;

        // 繋がったゲームサーバーのオブジェクト
        IPeer gameServer;

        // TCP通信するためのサービスオブジェクト
        CNetworkService service;

        // 接続完了の後、呼び出されるデリゲート。アプリケーションからコールバックメソッドを設定して使用。
        public delegate void StatusChangedHandler(NETWORK_EVENT status);
        public StatusChangedHandler appCallbackOnStatusChanged;

        // ネットワークメッセージを受信するたびに呼び出されるデリゲート。アプリケーションからコールバックを設定して使用。
        public delegate void MessageHandler(CPacket msg);
        public MessageHandler appCallbackOnMessage;

        void Awake()
        {
            CPacketBufferManager.Initialize(10);
            eventManager = new FreeNetEventManager();
        }

        public void Connect(string host, int port)
        {
            if (service != null)
            {
                Debug.LogError("Already connected.");
                return;
            }

            // CNetworkService オブジェクトは非同期メッセージ送受信の処理を行う。
            service = new CNetworkService();

            // EndPointの情報を持っているConnectorを生み出す。作成しておいた｢NetworkService｣のオブジェクトを入れる。
            CConnector connector = new CConnector(service);

            // 接続に成功した場合、呼び出されるコールバックメソッドを指定
            connector.connectedCallback += onConnectedGameServer;

            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(host), port);
            connector.Connect(endPoint);
        }

        public bool IsConnected()
        {
            return gameServer != null;
        }

        /// <summary>
        /// 接続に成功した場合、呼び出されるコールバックメソッド
        /// </summary>
        /// <param name="serverToken"></param>
        void onConnectedGameServer(CUserToken serverToken)
        {
            gameServer = new CRemoteServerPeer(serverToken);
            ((CRemoteServerPeer)gameServer).SetEventManager(eventManager);

            eventManager.EnqueueNetworkEvent(NETWORK_EVENT.connected);
        }


        /// <summary>
        /// ネットワークで発生する全てのイベントをクライアントに知らせる役をUpdate()で進める。
        /// CSharpServerのメッセージ送受信の処理はワーカースレッドで行われるが、UNITYロジックの処理はメインスレッドで行われる。
        /// メッセージキューイングを通じてメインスレッドで全ての処理が行われるように構築
        /// </summary>
        void Update()
        {
            // 受信したメッセージに対してのコールバック
            if (eventManager.HasMessage())
            {
                CPacket msg = eventManager.DequeueNetworkMessage();
                if (appCallbackOnMessage != null)
                {
                    appCallbackOnMessage(msg);
                }
            }

            // ネットワークで発生するイベントに対してのコールバック
            if (eventManager.HasEvent())
            {
                NETWORK_EVENT status = eventManager.DequeueNetworkEvent();
                if (appCallbackOnStatusChanged != null)
                {
                    appCallbackOnStatusChanged(status);
                }
            }
        }

        public void Send(CPacket msg)
        {
            try
            {
                gameServer.Send(msg);
                CPacket.Destroy(msg);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        void OnApplicationQuit()
        {
            if (gameServer != null)
            {
                ((CRemoteServerPeer)gameServer).token.Disconnect();
            }
        }
    }
}
