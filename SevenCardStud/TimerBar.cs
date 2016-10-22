using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TimerBar : MonoBehaviour
{

    Image foregroundImage;
    Image backgroundImage;

    public Sprite[] barColorSprites;

    float timeVal;
    float timeLimit;

    public float TimeVal
    {
        get { return timeVal; }
        set { timeVal = value; }
    }
    

    void Start()
    {
        foregroundImage = GameObject.Find("Canvas").transform.FindChild("TimerBar/Foreground").GetComponent<Image>();
        backgroundImage = GameObject.Find("Canvas").transform.FindChild("TimerBar/Background").GetComponent<Image>();
        timeVal = timeLimit = 10.0f;
    }

    void OnEnable()
    {
        timeVal = timeLimit;
    }

    void Update()
    {
        ReduceFillAmount();
    }

    public void SetTimeLimit(float _timeLimit)
    {
        this.gameObject.SetActive(true);
        timeLimit = _timeLimit;
        timeVal = timeLimit;
    }

    public void ReduceFillAmount()
    {
        timeVal -= Time.deltaTime;

        foregroundImage.fillAmount = timeVal / timeLimit;

        if(foregroundImage.fillAmount >= 0.6f)
        {
            // Green
            foregroundImage.sprite = barColorSprites[0];
        }
        else if(foregroundImage.fillAmount >= 0.35f)
        {
            // Yellow
            foregroundImage.sprite = barColorSprites[1];
        }
        else
        {
            // Red
            foregroundImage.sprite = barColorSprites[2];
        }

        if(timeVal < 0.0f)
        {
            timeVal = timeLimit;
        }
    }
}