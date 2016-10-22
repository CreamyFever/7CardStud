using UnityEngine;
using FreeNet;
using FreeNetUnity;
using SingletonPattern;

/// <summary>
/// パケット送受信のためのネットワークマネージャー
/// パケットデータは[header][body]構造を持つ。
/// - header : ヘッダーのサイズは2バイト。プロトコルのIDを持つ。
/// - body : メッセージの本文。
/// 
/// </summary>
public class NetworkManager : SingletonPattern<NetworkManager>
{
    public static NetworkManager _instance;

    FreeNetUnityService gameServer;
    string receivedMsg;

    public MonoBehaviour messageReceiver;

    void Awake()
    {
        _instance = this;

        receivedMsg = "";

        // ネットワーク通信をするためのFreeNetUnityServiceのオブジェクト
        gameServer = gameObject.AddComponent<FreeNetUnityService>();

        // ネットワークの接続と断絶のステータスを受信するデリゲート
        gameServer.appCallbackOnStatusChanged += onStatusChanged;

        // パケットを受信するデリゲート
        gameServer.appCallbackOnMessage += onMessage;

        DontDestroyOnLoad(this);
    }

    public void Connect(string ipString)
    {
        // サーバのIPアドレスとポート番号
        gameServer.Connect(ipString, 5000);
    }

    public bool IsConnected()
    {
        return gameServer.IsConnected();
    }

    void onStatusChanged(NETWORK_EVENT status)
    {
        switch (status)
        {
            // 接続に成功
            case NETWORK_EVENT.connected:
                {
                    CLogManager.Log("Connected");
                    receivedMsg += "on connected\n";

                    // 接続した後、CGameMainのコンポーネントからonConnected()を呼び出す。
                    GameObject.Find("GameMain").GetComponent<CGameMain>().onConnected();
                }
                break;

            // 断絶
            case NETWORK_EVENT.disconnected:
                CLogManager.Log("Disconnected");
                receivedMsg += "disconnected\n";
                break;
        }
    }

    void onMessage(CPacket msg)
    {
        messageReceiver.SendMessage("onRecv", msg);
    }

    public void Send(CPacket msg)
    {
        gameServer.Send(msg);
    }
}