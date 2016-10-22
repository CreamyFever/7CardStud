using UnityEngine;
using System.Collections;
using FreeNet;
using SevenPokerGameServer;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CGameMain : MonoBehaviour
{
    enum USER_STATE
    {
        NOT_CONNECTED,
        CONNECTED
    }
    
    USER_STATE userState;              

    public Button startBtn;             // スタートボタン

    public Transform inputIPAddressPanel;
    public InputField inputIPField;
    public Text messageText;

    public Button okBtn;
    public Button cancelBtn;

    bool isStart;
    
    void Start()
    {
        CLogManager.Log("CGameMain Start!");

        SoundManager._instance.soundState = SoundManager.SoundState.Main;
        
        userState = USER_STATE.NOT_CONNECTED;

        isStart = false;
        startBtn.onClick.AddListener(delegate { startBtnOnClick(); });
        //startBtn.onClick.AddListener(() => startBtnOnClick());

        inputIPAddressPanel = GameObject.Find("Canvas").transform.FindChild("IPMaskPanel");
        inputIPField = inputIPAddressPanel.GetChild(0).FindChild("InputField").GetComponent<InputField>();
        messageText = inputIPAddressPanel.GetChild(0).FindChild("MessageText").GetComponent<Text>();

        okBtn.onClick.AddListener(delegate { okBtnOnClick(); });
        cancelBtn.onClick.AddListener(delegate { cancelBtnOnClick(); });
    }

    /// <summary>
    /// スタートボタンを押した際に呼び出す。
    /// </summary>
    void startBtnOnClick()
    {
        inputIPAddressPanel.gameObject.SetActive(true);
    }

    /// <summary>
    /// OKボタンを押した際に呼び出す。
    /// </summary>
    void okBtnOnClick()
    {
        if (IsValidIPv4Address(inputIPField.text))
        {
            StartCoroutine(CheckConnection(inputIPField.text));
        }
        else
        {
            messageText.text = "有効なIPアドレスではありません。";
        }
    }

    /// <summary>
    /// キャンセルボタンを押した際に呼び出す。
    /// </summary>
    void cancelBtnOnClick()
    {
        inputIPAddressPanel.gameObject.SetActive(false);
    }

    /// <summary>
    /// 有効なIPアドレスかをチェック。
    /// </summary>
    bool IsValidIPv4Address(string ipString)
    {
        int num;

        if(string.IsNullOrEmpty(ipString))
            return false;

        string[] splitValues = ipString.Split('.');

        if (splitValues.Length != 4)
            return false;

        foreach(var value in splitValues)
        {
            num = System.Convert.ToInt32(value);
            if(num < 0 || num > 255)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// サーバの生存チェック
    /// </summary>
    IEnumerator CheckConnection(string ipString)
    {
        const float timeout = 10.0f;
        float startTime = Time.timeSinceLevelLoad;
        Ping ping = new Ping(ipString);
        
        while (true)
        {
            if(ping.isDone)
            {
                StopAllCoroutines();
                CLogManager.Log("Server is available!");
                Enter(ipString);
                isStart = true;
            }

            else if (Time.timeSinceLevelLoad - startTime > timeout)
            {
                StopAllCoroutines();
                messageText.text = "IPアドレスを再入力してください。";
                CLogManager.Log("Timed out.");
            }

            else
            {
                messageText.text = "Pingテスト中…";
                CLogManager.Log("Not connected.");
            }

            yield return 0;            
        }
    }

    public void Enter(string validIP)
    {
        CLogManager.Log("Enter");
        StopCoroutine("afterConnected");

        NetworkManager._instance.messageReceiver = this;

        if (!NetworkManager._instance.IsConnected())
        {
            userState = USER_STATE.CONNECTED;
            NetworkManager._instance.Connect(validIP);

            messageText.text = "ゲームを再起動してください。";
        }
        else
        {
            onConnected();
        }
    }

    /// <summary>
    /// サーバーに接続した後、処理を行うループ
    /// ボタン入力が入ったら、｢ENTER_LOBBY_REQ｣をサーバーに送る。
    /// クライアントからの要求が重複することを防ぐためコルーチンを停止。
    /// </summary>
    /// <returns></returns>
    IEnumerator afterConnected()
    {
        yield return new WaitForEndOfFrame();

        while (true)
        {
            if (userState == USER_STATE.CONNECTED)
            {
                if (isStart)
                {
                    CLogManager.Log("GO LOBBY");
                    CPacket msg = CPacket.Create((short)PROTOCOL.ENTER_LOBBY_REQ);
                    NetworkManager._instance.Send(msg);

                    StopCoroutine("afterConnected");
                }
            }

            yield return 0;
        }
    }

    void OnGUI()
    {
        switch (userState)
        {
            case USER_STATE.NOT_CONNECTED:
                break;

            case USER_STATE.CONNECTED:
                startBtn.gameObject.SetActive(true);
                break;
        }
    }

    /// <summary>
    /// サーバーに接続したら呼び出す。
    /// </summary>
    public void onConnected()
    {
        userState = USER_STATE.CONNECTED;

        StartCoroutine("afterConnected");
    }

    /// <summary>
    /// パケットを受信するたびに呼び出される。
    /// </summary>
    /// <param name="msg"></param>
    public void onRecv(CPacket msg)
    {
        // サーバーからのメッセージからプロトコルのIDを確認。
        PROTOCOL protocolId = (PROTOCOL)msg.PopProtocolId();
        CLogManager.Log("Protocol ID " + protocolId);

        switch (protocolId)
        {
            // ロビーに入場しろと応答を受けた場合
            case PROTOCOL.ENTER_LOBBY_RES:
                CLogManager.Log("Enter the Lobby.");

                SceneManager.LoadScene("Lobby");
                break;

            default:
                CLogManager.Log("Strange Protocol : " + protocolId.ToString());
                break;
        }
    }
}