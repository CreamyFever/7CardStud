using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FreeNet;
using SevenPokerGameServer;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CGameRoom : MonoBehaviour
{
    const int MAX_PLAYER = 5;
    private bool isRestart = false;
    
    GameObject canvas;

    public Button startBtn;             // スタートボタンのオブジェクト
    private Text totalChipText;         // ポット合計を表示するテキスト
    private Text raiseAmountText;
    private Text evaluatedResultText;   // 役を表示するテキスト

    public Image[] chipImg;
    private long[] digits;
    private Sprite[] bettingSprites;
    public GameObject fourCardEffect;
    public GameObject fullHouseEffect;
    public GameObject flushEffect;

    public Button exitBtn;

    public Button[] betBtn;

    public Text[] playerName;

    int myIndex;                                // 自分のインデックス
    public int[] otherPlayerIndex;              // 他のプレイヤーたちのインデックス
    public List<GameObject> otherPlayersObj;
    public CPlayer[] otherPlayers;

    // インデックスの比較のために用意
    int hostIndex;                              // ホストのインデックス
    int bossIndex;                              // ボス(舞ストリートでベッティングを始めるプレイヤー)のインデックス
    int currentTurn;                            // 誰の番なのか

    CPlayer gamePlayer;                         // プレイヤーのオブジェクト

    DiscardCardPanel discardPanel;
    ShowCommunityCardPanel showCardPanel;
    public TimerBar timerBar;
    const float TIME_SEC = 10.0f;

    private int GetOtherPlayerCount()
    {
        int otherCount = 0;

        for (int i = 0; i < MAX_PLAYER - 1; ++i)
        {
            if (otherPlayerIndex[i] != -1)
                otherCount++;
        }

        return otherCount;
    }

    /// <summary>
    /// 他のプレイヤーを追加
    /// </summary>
    /// <param name="otherPlayerPos"></param>
    /// <param name="playerIndex"></param>
    private void AddOtherPlayer(int otherPlayerPos, int playerIndex)
    {
        if (otherPlayerIndex[otherPlayerPos] == -1)
        {
            otherPlayerIndex[otherPlayerPos] = playerIndex;
            otherPlayersObj[otherPlayerPos].SetActive(true);
        }
    }

    private void RemoveOtherPlayer(int otherIndex)
    {
        otherPlayerIndex[otherIndex] = -1;
        otherPlayersObj[otherIndex].SetActive(false);
    }

    void Start()
    {
        CLogManager.Log("GameRoom Start!");

        SoundManager._instance.isPlaying = false;
        SoundManager._instance.soundState = SoundManager.SoundState.Room;

        NetworkManager._instance.messageReceiver = this;
        canvas = GameObject.Find("Canvas");

        CPacket msg = CPacket.Create((short)PROTOCOL.GAME_ROOM_INFO_REQ);
        NetworkManager._instance.Send(msg);
        CPacket.Destroy(msg);
        bettingSprites = Resources.LoadAll<Sprite>("Images/UI/Room/BettingAtlasJ");

        startBtn.onClick.AddListener(() => StartBtnOnClick());

        totalChipText = canvas.transform.FindChild("TotalChipText").GetComponent<Text>();
        raiseAmountText = canvas.transform.FindChild("CallAmountText").GetComponent<Text>();
        evaluatedResultText = canvas.transform.FindChild("EvaluatedResultText").GetComponent<Text>();

        chipImg = new Image[90];
        digits = new long[10];

        int j = 0;
        int k = 0;
        for (int i = 0; i < 90; ++i)
        {
            j = i / 9;
            k = (i % 9) + 1;
            chipImg[i] = canvas.transform.FindChild("Chips" + j + "/Chip0" + k).GetComponent<Image>();
        }

        exitBtn.onClick.AddListener(() => ExitBtnOnClick());

        //for文にするとなぜか配列の最大インデックスを超えてしまう…
        betBtn[0].onClick.AddListener(() => BettingBtnOnClick(0));
        betBtn[1].onClick.AddListener(() => BettingBtnOnClick(1));
        betBtn[2].onClick.AddListener(() => BettingBtnOnClick(2));
        betBtn[3].onClick.AddListener(() => BettingBtnOnClick(3));
        betBtn[4].onClick.AddListener(() => BettingBtnOnClick(4));
        betBtn[5].onClick.AddListener(() => BettingBtnOnClick(5));
        betBtn[6].onClick.AddListener(() => BettingBtnOnClick(6));

        DeactivateAllButton();

        otherPlayerIndex = new int[MAX_PLAYER - 1];
        otherPlayers = new CPlayer[MAX_PLAYER - 1];

        for (int i = 0; i < MAX_PLAYER - 1; ++i)
        {
            otherPlayerIndex[i] = -1;
            otherPlayers[i] = GameObject.Find("OtherPlayer" + (i + 1).ToString()).GetComponent<CPlayer>();
        }

        gamePlayer = GameObject.Find("Player").GetComponent<CPlayer>();
        discardPanel = canvas.transform.FindChild("DiscardCardPanel").GetComponent<DiscardCardPanel>();
        showCardPanel = canvas.transform.FindChild("SelectCommunityCardPanel").GetComponent<ShowCommunityCardPanel>();
        timerBar = canvas.transform.FindChild("TimerBar").GetComponent<TimerBar>();
    }
    
    void StartBtnOnClick()
    {
        CPacket msg;

        msg = CPacket.Create((short)PROTOCOL.GAME_START_REQ);

        NetworkManager._instance.Send(msg);
        CPacket.Destroy(msg);
    }

    void ExitBtnOnClick()
    {
        CPacket msg;

        msg = CPacket.Create((short)PROTOCOL.EXIT_ROOM_REQ);
        NetworkManager._instance.Send(msg);
        CPacket.Destroy(msg);
    }

    void BettingBtnOnClick(byte index)
    {
        CPacket msg;

        msg = CPacket.Create((short)PROTOCOL.BETTING_REQ);
        msg.Push(index);
        NetworkManager._instance.Send(msg);
        CPacket.Destroy(msg);

        DeactivateAllButton();
    }

    IEnumerator RequireInitializeGame(float duration)
    {
        WaitForEndOfFrame wait = new WaitForEndOfFrame();
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            yield return wait;
        }

        InitializeClient();
        if (hostIndex == myIndex)
        {
            startBtn.gameObject.SetActive(true);
        }

        isRestart = false;

        yield break;
    }

    /// <summary>
    /// パケットを受信するたびに呼び出される。
    /// </summary>
    /// <param name="msg"></param>
    void onRecv(CPacket msg)
    {
        PROTOCOL protocolId = (PROTOCOL)msg.PopProtocolId();
        CPacket msgToServer;
        CLogManager.Log("Protocol ID " + protocolId);

        switch (protocolId)
        {
            // サーバーに要求したルームの情報に応答のメッセージ。
            // 入場した順番どおり、自分を中心として時計回りに他のプレイヤーを配置
            case PROTOCOL.GAME_ROOM_INFO_RES:
                int otherIndex;
                hostIndex = msg.PopInt();
                myIndex = msg.PopInt();
                long myChips = msg.PopLong();
                int currentStreet = msg.PopInt();

                CLogManager.Log("MyIndex : " + myIndex);
                CLogManager.Log("HostIndex : " + hostIndex);
                playerName[0].text = "GAMER " + (myIndex + 1).ToString();
                gamePlayer.myChips.text = myChips.ToString();

                if (hostIndex == myIndex)
                {
                    startBtn.gameObject.SetActive(true);
                }

                for (int i = 0; i < MAX_PLAYER - 1; ++i)
                {
                    otherIndex = msg.PopInt();
                    CLogManager.Log("OtherIndex Check " + otherIndex);

                    if (myIndex > otherIndex)
                    {
                        otherPlayerIndex[(MAX_PLAYER - 1) + otherIndex - myIndex] = otherIndex;
                        otherPlayers[(MAX_PLAYER - 1) + otherIndex - myIndex].myChips.text = msg.PopLong().ToString();
                        playerName[(MAX_PLAYER - 1) + otherIndex - myIndex + 1].text = "GAMER " + (otherIndex + 1).ToString();
                    }
                    else if(myIndex < otherIndex)
                    {
                        CLogManager.Log("OtherIndex " + otherIndex);
                        otherPlayerIndex[otherIndex - (myIndex + 1)] = otherIndex;
                        otherPlayers[otherIndex - (myIndex + 1)].myChips.text = msg.PopLong().ToString();
                        playerName[otherIndex - myIndex].text = "GAMER " + (otherIndex + 1).ToString();
                    }
                }

                for (int i = 0; i < MAX_PLAYER - 1; ++i)
                {
                    if (otherPlayerIndex[i] != -1)
                    {
                        otherPlayersObj[i].SetActive(true);
                        DrawCardImages(msg, i, currentStreet);
                        CLogManager.Log("Info!!!");
                    }
                }
                break;

            case PROTOCOL.EXIT_ROOM_OK:
                SceneManager.LoadScene("Lobby");
                break;

            // ルームに入場した順番どおり、時計回りに配置
            case PROTOCOL.PLAYER_ENTER_ROOM:

                int playerIndex = msg.PopInt();
                long playerChips = msg.PopLong();
                CLogManager.Log("Player " + playerIndex + " comes here.");

                if (myIndex == playerIndex)
                    break;

                else if (myIndex > playerIndex)
                {
                    AddOtherPlayer((MAX_PLAYER - 1) + playerIndex - myIndex, playerIndex);
                    playerName[(MAX_PLAYER - 1) + playerIndex - myIndex + 1].text = "GAMER " + (playerIndex + 1).ToString();
                    otherPlayers[(MAX_PLAYER - 1) + playerIndex - myIndex].myChips.text = playerChips.ToString();
                }
                else
                {
                    AddOtherPlayer(playerIndex - (myIndex + 1), playerIndex);
                    playerName[playerIndex - myIndex].text = "GAMER " + (playerIndex + 1).ToString();
                    otherPlayers[playerIndex - (myIndex + 1)].myChips.text = playerChips.ToString();
                }

                break;

            // 他のプレイヤーがルームを出た場合
            case PROTOCOL.PLAYER_EXIT_ROOM:
                playerIndex = msg.PopInt();
                hostIndex = msg.PopInt();

                CLogManager.Log("MyIndex : " + myIndex);
                CLogManager.Log("HostIndex : " + hostIndex);
                
                if (hostIndex == myIndex)
                    startBtn.gameObject.SetActive(true);

                CLogManager.Log("Player " + playerIndex + " went out here.");

                if (myIndex == playerIndex)
                    break;

                else if (myIndex > playerIndex)
                {
                    RemoveOtherPlayer((MAX_PLAYER - 1) + playerIndex - myIndex);
                }
                else
                {
                    RemoveOtherPlayer(playerIndex - (myIndex + 1));
                }

                break;


            // サーバーからゲームスタートしていいと応答がきた。
            case PROTOCOL.GAME_START_RES:
                ClearCardImages();
                discardPanel.discardCallFlag = true;
                showCardPanel.commuCallFlag = true;
                if (GetOtherPlayerCount() > 0)
                    startBtn.gameObject.SetActive(false);

                DeactivateChipImage();
                break;

            case PROTOCOL.DEAL_FIRST_CARD_RES:
                long totalchip = msg.PopLong();

                totalChipText.text = totalchip.ToString();

                ActivateChipImage(totalchip);

                for (int i = 0; i < MAX_PLAYER; ++i)
                {
                    // 空席は飛ばす。
                    if (msg.PopByte() == 0)
                        continue;

                    playerChips = msg.PopLong();

                    DrawCardImages(msg, i, 4, false);

                    for (int num = 0; num < 4; ++num)
                    {
                        if (myIndex == i)
                        {
                            gamePlayer.myChips.text = playerChips.ToString();
                            discardPanel.cardBtns[num].image.sprite = gamePlayer.myCardImg[num].sprite;
                        }
                        else if (myIndex > i)
                        {
                            otherPlayers[(MAX_PLAYER - 1) + i - myIndex].myChips.text = playerChips.ToString();
                        }
                        else
                        {
                            otherPlayers[i - (myIndex + 1)].myChips.text = playerChips.ToString();
                        }
                    }
                }

                discardPanel.gameObject.SetActive(true);
                break;

            // カードを捨てたことをブロードキャスト
            case PROTOCOL.BROADCAST_DISCARD_CARD:
                playerIndex = msg.PopInt();

                if (myIndex == playerIndex)
                {
                    gamePlayer.DiscardCard(msg.PopInt());
                    discardPanel.gameObject.SetActive(false);
                    showCardPanel.gameObject.SetActive(true);
                }
                else if (myIndex > playerIndex)
                {
                    otherPlayers[(MAX_PLAYER - 1) + playerIndex - myIndex].DiscardCard(msg.PopInt());
                }
                else
                {
                    otherPlayers[playerIndex - (myIndex + 1)].DiscardCard(msg.PopInt());
                }
                Debug.Log("[DISCARD]PlayerIndex " + playerIndex);
                break;

            // 表向きにしたカードを選んだ場合
            case PROTOCOL.BROADCAST_OPEN_CARD:
                playerIndex = msg.PopInt();

                if (myIndex == playerIndex)
                {
                    gamePlayer.DiscardCard(msg.PopInt());
                    showCardPanel.gameObject.SetActive(false);
                }
                else if (myIndex > playerIndex)
                {
                    otherPlayers[(MAX_PLAYER - 1) + playerIndex - myIndex].DiscardCard(msg.PopInt());
                }
                else
                {
                    otherPlayers[playerIndex - (myIndex + 1)].DiscardCard(msg.PopInt());
                }
                Debug.Log("[OPEN]PlayerIndex " + playerIndex);
                break;

            // 表向きにしたカードを選んだ場合
            case PROTOCOL.SHOW_A_CARD_ON_HAND_RES:
                ClearCardImages();

                for (int i = 0; i < MAX_PLAYER; ++i)
                {
                    // 空席は飛ばす。
                    if (msg.PopByte() == 0)
                        continue;

                    DrawCardImages(msg, i, 3, false);
                }
                break;

            // ボス(親)を決めるために、表向きにされたカードリストと役を比較した結果をサーバーから取得
            case PROTOCOL.EVALUATE_COMMUNITY_CARDS:
                int cardIndex;

                for (int i = 0; i < MAX_PLAYER; ++i)
                {
                    // 空席は飛ばす。
                    if (msg.PopByte() == 0)
                        continue;

                    cardIndex = msg.PopInt();

                    if (myIndex == i)
                    {
                        gamePlayer.ShowCard(cardIndex, true);
                    }
                    else if (myIndex > i)
                    {
                        if (cardIndex >= 6)
                            otherPlayers[(MAX_PLAYER - 1) + i - myIndex].ShowCard(cardIndex, false);
                        else
                            otherPlayers[(MAX_PLAYER - 1) + i - myIndex].ShowCard(cardIndex, true);

                    }
                    else
                    {
                        if (cardIndex >= 6)
                            otherPlayers[i - (myIndex + 1)].ShowCard(cardIndex, false);
                        else
                            otherPlayers[i - (myIndex + 1)].ShowCard(cardIndex, true);
                    }
                }
                break;

            // カードが配られる。
            case PROTOCOL.DEAL_CARD_RES:
                CCard card = null;
                int cardNo = 0;
                bossIndex = msg.PopInt();
                int dealIndex = 0;

                for (int i = 0; i < MAX_PLAYER; ++i)
                {
                    // 空席は飛ばす。
                    if (msg.PopByte() == 0)
                        continue;
                    dealIndex = i + bossIndex;

                    if (dealIndex >= MAX_PLAYER)
                    {
                        dealIndex -= MAX_PLAYER;
                    }

                    card = new CCard((CARD_SUIT)msg.PopInt(), (CARD_RANK)msg.PopInt());
                    cardNo = card.Suit * 13 + (card.Rank - 2);

                    if (myIndex == dealIndex)
                    {
                        gamePlayer.DrawCard(card);
                    }
                    else if (myIndex > dealIndex)
                    {
                        otherPlayers[(MAX_PLAYER - 1) + dealIndex - myIndex].DrawCard(card);
                    }
                    else
                    {
                        otherPlayers[dealIndex - (myIndex + 1)].DrawCard(card);
                    }
                }

                DeactivateAllButton();
                break;

            // ベッティングの準備ができた。
            case PROTOCOL.READY_TO_BET:
                DeactivateBossPlayerImage();
                DeactivateCurrentImage();

                bossIndex = msg.PopInt();
                byte activeCase = msg.PopByte();

                if (myIndex == bossIndex)
                {
                    gamePlayer.bossPlayerImg.gameObject.SetActive(true);
                    ActivateButton(activeCase);
                }
                else if (myIndex > bossIndex)
                {
                    otherPlayers[(MAX_PLAYER - 1) + bossIndex - myIndex].bossPlayerImg.gameObject.SetActive(true);
                }
                else
                {
                    otherPlayers[bossIndex - (myIndex + 1)].bossPlayerImg.gameObject.SetActive(true);
                }

                timerBar.SetTimeLimit(TIME_SEC);

                msgToServer = CPacket.Create((short)PROTOCOL.CURRENT_HAND_REQ);
                msgToServer.Push(myIndex);
                NetworkManager._instance.Send(msgToServer);
                CPacket.Destroy(msgToServer);
                break;

            // 今の組み合わせで出来る役を表示する
            case PROTOCOL.CURRENT_HAND:
                CLogManager.Log("CURRENT HAND");
                short evalHand = msg.PopShort();
                DisplayEvaluatedHand(evalHand);
                break;

            // ベッティング
            case PROTOCOL.BETTING_RES:
                int beforeTurn = msg.PopInt();           // ベッティングするボタンを押すと次のプレイヤーのターンになるので前のインデックスを取得
                byte betIndex = msg.PopByte();
                activeCase = msg.PopByte();
                currentTurn = msg.PopInt();
                totalchip = msg.PopLong();
                long raiseAmount = msg.PopLong();
                playerChips = msg.PopLong();
                long betChip = msg.PopLong();

                timerBar.SetTimeLimit(TIME_SEC);

                totalChipText.text = totalchip.ToString();
                raiseAmountText.text = raiseAmount.ToString();

                DeactivateChipImage();
                ActivateChipImage(totalchip);

                CLogManager.Log("Active Case = " + activeCase);

                if (myIndex == beforeTurn)
                {
                    gamePlayer.myChips.text = playerChips.ToString();
                    gamePlayer.bettingChips.text = betChip.ToString();
                    DisplayBettingImage(gamePlayer, betIndex);

                    if (betIndex == 0)
                        gamePlayer.ReverseCardToBack();
                }
                else if (myIndex > beforeTurn)
                {
                    if (otherPlayers[(MAX_PLAYER - 1) + beforeTurn - myIndex].gameObject.activeSelf)
                    {
                        otherPlayers[(MAX_PLAYER - 1) + beforeTurn - myIndex].myChips.text = playerChips.ToString();
                        otherPlayers[(MAX_PLAYER - 1) + beforeTurn - myIndex].bettingChips.text = betChip.ToString();
                        DisplayBettingImage(otherPlayers[(MAX_PLAYER - 1) + beforeTurn - myIndex], betIndex);

                        if (betIndex == 0)
                            otherPlayers[(MAX_PLAYER - 1) + beforeTurn - myIndex].ReverseCardToBack();
                    }
                }
                else
                {
                    if (otherPlayers[beforeTurn - (myIndex + 1)].gameObject.activeSelf)
                    {
                        otherPlayers[beforeTurn - (myIndex + 1)].myChips.text = playerChips.ToString();
                        otherPlayers[beforeTurn - (myIndex + 1)].bettingChips.text = betChip.ToString();
                        DisplayBettingImage(otherPlayers[beforeTurn - (myIndex + 1)], betIndex);

                        if (betIndex == 0)
                            otherPlayers[beforeTurn - (myIndex + 1)].ReverseCardToBack();
                    }
                }

                // 自分のターンが来たら、ボタンを活性化
                if (myIndex == currentTurn)
                {
                    ActivateButton(activeCase);
                    DeactivateCurrentImage();
                    gamePlayer.CurrentTurnImg.gameObject.SetActive(true);
                }
                else if (myIndex > currentTurn)
                {
                    DeactivateCurrentImage();
                    otherPlayers[(MAX_PLAYER - 1) + currentTurn - myIndex].CurrentTurnImg.gameObject.SetActive(true);
                    DeactivateAllButton();
                }
                else
                {
                    DeactivateCurrentImage();
                    otherPlayers[currentTurn - (myIndex + 1)].CurrentTurnImg.gameObject.SetActive(true);
                    DeactivateAllButton();
                }

                SoundManager._instance.RandomSoundEffect(
                    SoundManager._instance.efxClip[0],
                    SoundManager._instance.efxClip[1],
                    SoundManager._instance.efxClip[2],
                    SoundManager._instance.efxClip[3],
                    SoundManager._instance.efxClip[4],
                    SoundManager._instance.efxClip[5]);
                break;

            // セブンスストリートまでベッティングが終わったら、結果を発表
            case PROTOCOL.SHOW_RESULT:
                DeactivateAllButton();
                timerBar.gameObject.SetActive(false);
                bossIndex = msg.PopInt();       // 勝者プレイヤーのインデックスを取得
                short bossHand = msg.PopShort();
                int playerCount = msg.PopInt();
                short eval;
                int highRank;

                if (myIndex == bossIndex)
                {
                    gamePlayer.WinnerEffect.SetActive(true);
                }
                else if (myIndex > bossIndex)
                {
                    otherPlayers[(MAX_PLAYER - 1) + bossIndex - myIndex].WinnerEffect.SetActive(true);
                }
                else
                {
                    otherPlayers[bossIndex - (myIndex + 1)].WinnerEffect.SetActive(true);
                }

                gamePlayer.evaluatorPanel.gameObject.SetActive(true);
                for(int i = 0; i < MAX_PLAYER - 1; ++i)
                {
                    if (otherPlayers[i].gameObject.activeSelf)
                        otherPlayers[i].evaluatorPanel.gameObject.SetActive(true);
                }

                for (int i = 0; i < MAX_PLAYER; ++i)
                {
                    if (msg.PopByte() == 0)
                        continue;

                    eval = msg.PopShort();
                    highRank = msg.PopInt();

                    if (myIndex == i)
                    {
                        gamePlayer.DisplayTextEvaluatedResult(eval, highRank);
                    }
                    else if (myIndex > i)
                    {
                        otherPlayers[(MAX_PLAYER - 1) + i - myIndex].DisplayTextEvaluatedResult(eval, highRank); ;
                    }
                    else
                    {
                        otherPlayers[i - (myIndex + 1)].DisplayTextEvaluatedResult(eval, highRank);
                    }                    
                }

                if (playerCount > 1)
                    ActivateBossHandEffect(bossHand);           // スクリーンの中央に一番強い役を表示するエフェクト(フォーカード、フルハウス、フラッシュ)

                ClearCardImages();

                for (int i = 0; i < MAX_PLAYER; ++i)
                {
                    // 空席は飛ばす。
                    if (msg.PopByte() == 0)
                        continue;

                    if (playerCount > 1)
                        DrawCardImages(msg, i, 7, true);
                }

                SoundManager._instance.isPlaying = false;
                SoundManager._instance.soundState = SoundManager.SoundState.Win;

                //isRestart = true;
                Invoke("InitializeClient", 5.0f);                

                break;

            default:
                CLogManager.Log("Strange Protocol : " + protocolId.ToString());
                break;
        }
    }

    /// <summary>
    /// 状況によって、ボタンを活性化するメソッド(caseはサーバーから取得)
    /// </summary>
    /// <param name="activeCase"></param>
    public void ActivateButton(int activeCase)
    {
        switch (activeCase)
        {
            case 0:                             // フォースストリート(カードが4枚の状態)であり、ボスのプレイヤーが行う。
                betBtn[2].interactable = true;  // ブリングイン
                betBtn[4].interactable = true;  // コンプリート
                break;
            case 1:                                     // 賭けられたチップがない場合
                betBtn[0].interactable = true;          // フォールド
                betBtn[1].interactable = true;          // チェック
                betBtn[6].gameObject.SetActive(true);   // ベットのボタンをアクティブに。
                betBtn[6].interactable = true;          // ベット
                break;
            case 2:                             // 賭けられたチップがないと同時に、チップが足りない場合
                betBtn[0].interactable = true;  // フォールド
                betBtn[1].interactable = true;  // チェック
                break;
            case 3:                             // 賭けられたチップをレイズする場合
                betBtn[0].interactable = true;  // フォールド
                betBtn[3].interactable = true;  // コール
                betBtn[5].interactable = true;  // レイズ
                break;
            case 4:                             // レイズするためのチップが足りない場合
                betBtn[0].interactable = true;  // フォールド
                betBtn[3].interactable = true;  // コール
                break;
        }
    }

    public void DeactivateAllButton()
    {
        for (int i = 0; i < 7; ++i)
        {
            betBtn[i].interactable = false;
        }
        betBtn[6].gameObject.SetActive(false);
    }

    public void ActivateChipImage(long amount)
    {
        long tempDigit = amount;
        long pow;
        for (int i = 9; i >= 0; --i)
        {
            pow = (long)Mathf.Pow(10, i + 2);
            digits[i] = tempDigit / pow;
            tempDigit = tempDigit % pow;
        }

        for (int i = 0; i < 10; ++i)
        {
            for (int j = 0; j < digits[i]; ++j)
            {
                chipImg[i * 9 + j].gameObject.SetActive(true);
            }
        }
    }

    public void DeactivateChipImage()
    {
        for (int i = 0; i < 90; ++i)
        {
            chipImg[i].gameObject.SetActive(false);
        }
    }

    void DisplayBettingImage(CPlayer player, byte index)
    {
        switch (index)
        {
            case 0:
                player.bettingImg.sprite = bettingSprites[20];  // フォールド
                break;
            case 1:
                player.bettingImg.sprite = bettingSprites[3];   // チェック
                break;
            case 2:
            case 4:
                player.bettingImg.sprite = bettingSprites[1];  // ベット(ブリングイン、コンプリート)
                break;
            case 3:
                player.bettingImg.sprite = bettingSprites[2];  // コール
                break;
            case 5:
                player.bettingImg.sprite = bettingSprites[22];  // レイズ
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 今の組み合わせで出来る役を見せる(ゲームルームの左下に表示される)
    /// </summary>
    /// <param name="evalHand"></param>
    void DisplayEvaluatedHand(short evalHand)
    {
        switch (evalHand)
        {
            case 0:
                evaluatedResultText.text = "HIGH CARD";
                break;
            case 1:
                evaluatedResultText.text = "ONE PAIR";
                break;
            case 2:
                evaluatedResultText.text = "TWO PAIRS";
                break;
            case 3:
                evaluatedResultText.text = "THREE OF A KIND";
                break;
            case 4:
                evaluatedResultText.text = "STRAIGHT";
                break;
            case 5:
                evaluatedResultText.text = "FLUSH";
                break;
            case 6:
                evaluatedResultText.text = "FULL HOUSE";
                break;
            case 7:
                evaluatedResultText.text = "FOUR OF A KIND";
                break;
            case 8:
                evaluatedResultText.text = "STRAIGHT FLUSH";
                break;
            case 9:
                evaluatedResultText.text = "ROYAL STRAIGHT FLUSH";
                break;
        }
    }

    void DeactivateBossPlayerImage()
    {
        gamePlayer.bossPlayerImg.gameObject.SetActive(false);

        for (int i = 0; i < MAX_PLAYER - 1; ++i)
            otherPlayers[i].bossPlayerImg.gameObject.SetActive(false);
    }

    void DeactivateCurrentImage()
    {
        gamePlayer.CurrentTurnImg.gameObject.SetActive(false);

        for (int i = 0; i < MAX_PLAYER - 1; ++i)
            otherPlayers[i].CurrentTurnImg.gameObject.SetActive(false);
    }

    void DrawCardImages(CPacket message, int compareIndex, int cardnum, bool isHidden)
    {
        CCard card = null;
        int cardNo = 0;

        for (int num = 0; num < cardnum; ++num)
        {
            card = new CCard((CARD_SUIT)message.PopInt(), (CARD_RANK)message.PopInt());
            cardNo = card.Suit * 13 + (card.Rank - 2);

            if (myIndex == compareIndex)
            {
                gamePlayer.DrawCard(card);
                gamePlayer.ShowCard(num, true);
            }
            else if (myIndex > compareIndex)
            {
                otherPlayers[(MAX_PLAYER - 1) + compareIndex - myIndex].DrawCard(card);
                otherPlayers[(MAX_PLAYER - 1) + compareIndex - myIndex].ShowCard(num, isHidden);
            }
            else
            {
                otherPlayers[compareIndex - (myIndex + 1)].DrawCard(card);
                otherPlayers[compareIndex - (myIndex + 1)].ShowCard(num, isHidden);
            }
        }
    }

    void DrawCardImages(CPacket message, int otherIndex, int cardnum)
    {
        CCard card = null;
        int cardNo = 0;

        for (int num = 0; num < cardnum; ++num)
        {
            card = new CCard((CARD_SUIT)message.PopInt(), (CARD_RANK)message.PopInt());
            cardNo = card.Suit * 13 + (card.Rank - 2);

            CLogManager.Log("CARD SUIT = " + card.Suit + " Rank = " + card.Rank);
                        
            otherPlayers[otherIndex].DrawCard(card);
            otherPlayers[otherIndex].ShowCardForObserver(num);
        }
    }

    void ClearCardImages()
    {
        gamePlayer.ClearCardList();
        otherPlayers[0].ClearCardList();
        otherPlayers[1].ClearCardList();
        otherPlayers[2].ClearCardList();
        otherPlayers[3].ClearCardList();
    }

    /// <summary>
    /// 一番強い役が出来たプレイヤーの役をスクリーンの中央に表示するメソッド
    /// </summary>
    /// <param name="bossHand"></param>
    void ActivateBossHandEffect(short bossHand)
    {
        if (bossHand == 7)
        {
            fourCardEffect.SetActive(true);
        }
        else if (bossHand == 6)
        {
            fullHouseEffect.SetActive(true);
        }
        else if (bossHand == 5)
        {
            flushEffect.SetActive(true);
        }

        return;
    }

    void InitializeClient()
    {
        totalChipText.text = 0.ToString();
        raiseAmountText.text = 0.ToString();
        evaluatedResultText.text = "WAITING";
        DeactivateAllButton();
        DeactivateChipImage();

        ClearCardImages();
        gamePlayer.bettingImg.sprite = bettingSprites[21];
        gamePlayer.InitPlayerObj();
        for(int i = 0; i < MAX_PLAYER - 1; ++i)
        {
            otherPlayers[i].bettingImg.sprite = bettingSprites[21];
            otherPlayers[i].InitPlayerObj();
        }

        if (hostIndex == myIndex)
        {
            startBtn.gameObject.SetActive(true);
        }

        fourCardEffect.SetActive(false);
        fullHouseEffect.SetActive(false);
        flushEffect.SetActive(false);

        SoundManager._instance.isPlaying = false;
        SoundManager._instance.soundState = SoundManager.SoundState.Room;

        CLogManager.Log("Initialize");
    }
}