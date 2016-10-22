using UnityEngine;
using System.Collections.Generic;
using FreeNet;
using SevenPokerGameServer;
using UnityEngine.UI;

/// <summary>
/// 捨てるカードを選ぶパネル。
/// </summary>
public class DiscardCardPanel : MonoBehaviour
{
    NetworkManager networkManager;
    ShowCommunityCardPanel showCardPanel;
    public List<Button> cardBtns;

    public int selectedIndex;

    // ディスカードパネルが何度もポップすることを防ぐためのフラグ。
    public bool discardCallFlag = false;

    void Start()
    {
        networkManager = GameObject.Find("NetworkManager").GetComponent<NetworkManager>();
    }

    void OnEnable()
    {
        showCardPanel = GameObject.Find("Canvas").transform.FindChild("SelectCommunityCardPanel").GetComponent<ShowCommunityCardPanel>();

        //for文にするとなぜか配列の最大インデックスを超えてしまう…
        cardBtns[0].onClick.AddListener(() => DetermineDiscardCard(0));
        cardBtns[1].onClick.AddListener(() => DetermineDiscardCard(1));
        cardBtns[2].onClick.AddListener(() => DetermineDiscardCard(2));
        cardBtns[3].onClick.AddListener(() => DetermineDiscardCard(3));
    }

    // ゲームが終わって、新しくゲームを始めたらパネルオブジェクトがそのまま残存してるか、パネルが重なってしまうので、フラグを与え、一度だけ呼び出すようにする。
    void DetermineDiscardCard(int index)
    {
        if (discardCallFlag)
        {
            gameObject.SetActive(false);

            selectedIndex = index;
            CPacket msg = CPacket.Create((short)PROTOCOL.SELECT_DISCARD_CARD_REQ);

            msg.Push(index);

            networkManager.Send(msg);
            CPacket.Destroy(msg);

            discardCallFlag = false;
        }
        CLogManager.Log("Discard!");
    }	
}
