using UnityEngine;
using FreeNet;
using SevenPokerGameServer;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class CGameLobby : MonoBehaviour
{
    public Button[] anteBtn;
    public Button exitBtn;
    
    long ante;

    void Start ()
    {
        CLogManager.Log("GameLobby Started!");

        SoundManager._instance.isPlaying = false;
        SoundManager._instance.soundState = SoundManager.SoundState.Lobby;

        NetworkManager._instance.messageReceiver = this;

        anteBtn[0].onClick.AddListener(() => StakeBtnOnClick(0));
        anteBtn[1].onClick.AddListener(() => StakeBtnOnClick(1));
        anteBtn[2].onClick.AddListener(() => StakeBtnOnClick(2));
        exitBtn.onClick.AddListener(() => ExitBtnOnClick());
    }

    void StakeBtnOnClick(int index)
    {
        switch(index)
        {
            case 0:
                ante = 500;
                break;
            case 1:
                ante = 1000;
                break;
            case 2:
                ante = 2000;
                break;
        }

        CPacket msg = CPacket.Create((short)PROTOCOL.AUTO_ENTER_REQ);
        msg.Push(ante);
        NetworkManager._instance.Send(msg);
    }

    void ExitBtnOnClick()
    {
        Application.Quit();
    }

    void CreateGameRoom(long stake)
    {
        CPacket msg = CPacket.Create((short)PROTOCOL.CREATE_ROOM_REQ);
        msg.Push(stake);
        NetworkManager._instance.Send(msg);
    }
    
    /// <summary>
    /// パケットを受信するたびに呼び出される。
    /// </summary>
    /// <param name="msg"></param>
    void onRecv(CPacket msg)
    {
        PROTOCOL protocolId = (PROTOCOL)msg.PopProtocolId();
        CLogManager.Log("Protocol ID " + protocolId);

        switch (protocolId)
        {
            // ルームが存在しないと、新しくルームを作成して入場
            case PROTOCOL.AUTO_ENTER_NO:
                CLogManager.Log("No rooms are in the lobby.");
                CreateGameRoom(ante);
                break;

            // 入場に成功
            case PROTOCOL.ENTER_ROOM_OK:
                SceneManager.LoadScene("GameRoom");
                break;

            default:
                CLogManager.Log("Strange Protocol : " + protocolId.ToString());
                break;
        }
    }
}
