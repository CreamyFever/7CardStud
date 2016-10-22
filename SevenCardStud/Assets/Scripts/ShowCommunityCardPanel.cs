using UnityEngine;
using System.Collections.Generic;
using FreeNet;
using SevenPokerGameServer;
using UnityEngine.UI;

/// <summary>
/// 表向きにするカードを選ぶパネル。
/// </summary>
public class ShowCommunityCardPanel : MonoBehaviour
{
    public NetworkManager networkManager;
    DiscardCardPanel discardPanel;

    public CPlayer player;

    public List<Button> cardBtns;

    // オープンカード(表向きにするカード)選択のパネルが何度もポップすることを防ぐためのフラグ。
    public bool commuCallFlag = false;

    void Start()
    {
        networkManager = GameObject.Find("NetworkManager").GetComponent<NetworkManager>();
    }

    void OnEnable()
    {
        discardPanel = GameObject.Find("Canvas").transform.FindChild("DiscardCardPanel").GetComponent<DiscardCardPanel>();
        player = GameObject.Find("Player").GetComponent<CPlayer>();

        //for文にするとなぜか配列の最大インデックスを超えてしまう…
        CopyButtonImage();
        cardBtns[0].onClick.AddListener(() => DetermineCommunityCard(0));
        cardBtns[1].onClick.AddListener(() => DetermineCommunityCard(1));
        cardBtns[2].onClick.AddListener(() => DetermineCommunityCard(2));
    }

    // ディスカードパネルで選んだカードのインデックスを除いて、残り3枚のイメージをコピー。
    void CopyButtonImage()
    {
        for(int i = 0; i < 3; ++i)
            cardBtns[i].image.sprite = player.myCardImg[i].sprite;
    }

    // ゲームが終わって、新しくゲームを始めたらパネルオブジェクトがそのまま残存してるか、パネルが重なってしまうので、フラグを与え、一度だけ呼び出すようにする。
    void DetermineCommunityCard(int index)
    {
        if (commuCallFlag)
        {
            gameObject.SetActive(false);
            CPacket msg = CPacket.Create((short)PROTOCOL.SELECT_OPEN_CARD_REQ);

            msg.Push(index);

            networkManager.Send(msg);
            CPacket.Destroy(msg);

            commuCallFlag = false;
        }
    }
}
