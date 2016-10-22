using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using SevenPokerGameServer;

/// <summary>
/// プレイヤーの手持ちカードとチップの数を画面に表す。
/// </summary>
public class CPlayer : MonoBehaviour
{
    List<CCard> myDeck;                 // 自分の手持ちカードのリスト
    public List<Image> myCardImg;
    public Text myChips;
    public Text bettingChips;
    public Image bettingImg;
    public Image bossPlayerImg;
    public Image CurrentTurnImg;
    public GameObject WinnerEffect;
    public Image evaluatorPanel;
    public Text evaluatorText;

    public CPlayer()
    {
        myDeck = new List<CCard>(7);
        myCardImg = new List<Image>(7);
    }

    void Start()
    {
        evaluatorText = evaluatorPanel.transform.FindChild("Text").GetComponent<Text>();
    }

    public List<CCard> GetDeck()
    {
        return myDeck;
    }

    public void ClearCardList()
    {
        myDeck.Clear();
    }

    public void DisableCardImg()
    {
        for (int i = 0; i < myCardImg.Count; ++i)
            myCardImg[i].gameObject.SetActive(false);
    }

    public void DrawCard(CCard card)
    {
        myDeck.Add(card);
    }

    public void ShowCard(int index, bool isFace)
    {
        int num = myDeck[index].Suit * 13 + (myDeck[index].Rank - 2);

        myCardImg[index].gameObject.SetActive(true);
        if (isFace)
            myCardImg[index].sprite = Resources.Load<Sprite>("Images/Cards/BigCard_" + num.ToString());
        else
            myCardImg[index].sprite = Resources.Load<Sprite>("Images/Cards/BigCard_52");
    }

    public void ShowCardForObserver(int index)
    {
        int num = myDeck[index].Suit * 13 + (myDeck[index].Rank - 2);

        myCardImg[index].gameObject.SetActive(true);
        if (index >= 2 && index < 6)
            myCardImg[index].sprite = Resources.Load<Sprite>("Images/Cards/BigCard_" + num.ToString());
        else
            myCardImg[index].sprite = Resources.Load<Sprite>("Images/Cards/BigCard_52");
    }

    public void ReverseCardToBack()
    {
        for(int i = 0; i < myDeck.Count; ++i)
        {
            myCardImg[i].sprite = Resources.Load<Sprite>("Images/Cards/DarkBigCard_52");
        }
    }

    public void DiscardCard(int index)
    {
        myDeck.RemoveAt(index);

        for (int i = 0; i < 4; ++i)
        {
            if (index == i)
                continue;
            else if (index > i)
                myCardImg[i].sprite = myCardImg[i].sprite;
            else
                myCardImg[i - 1].sprite = myCardImg[i].sprite;
        }
        myCardImg[3].gameObject.SetActive(false);
    }

    public void InitPlayerObj()
    {
        DisableCardImg();
        bettingChips.text = 0.ToString();
        bossPlayerImg.gameObject.SetActive(false);
        CurrentTurnImg.gameObject.SetActive(false);
        WinnerEffect.SetActive(false);
        evaluatorPanel.gameObject.SetActive(false);
    }

    public void DisplayTextEvaluatedResult(short eval, int highRank)
    {
        switch(eval)
        {
            case 0:
                evaluatorText.text = "ノーペア " + (CARD_RANK)highRank;
                break;
            case 1:
                evaluatorText.text = "ワンペア " + (CARD_RANK)highRank;
                break;
            case 2:
                evaluatorText.text = "ツーペア " + (CARD_RANK)highRank;
                break;
            case 3:
                evaluatorText.text = "スリーカード " + (CARD_RANK)highRank;
                break;
            case 4:
                evaluatorText.text = "ストレート " + (CARD_RANK)highRank;
                break;
            case 5:
                evaluatorText.text = "フラッシュ " + (CARD_RANK)highRank;
                break;
            case 6:
                evaluatorText.text = "フルハウス " + (CARD_RANK)highRank;
                break;
            case 7:
                evaluatorText.text = "フォーカード " + (CARD_RANK)highRank;
                break;
            case 8:
                evaluatorText.text = "ストレートフラッシュ " + (CARD_RANK)highRank;
                break;
            case 9:
                evaluatorText.text = "ロイヤルフラッシュ " + (CARD_RANK)highRank;
                break;
        }
    }
}