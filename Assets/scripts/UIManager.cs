using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using UnitySocketIO;
using UnitySocketIO.Events;
using TMPro;
using UnityEngine.UI;
using SimpleJSON;

public class UIManager : MonoBehaviour
{
    public SocketIOController io;

    public TMP_Text walletAmount_Text;
    public TMP_Text info_Text;
    public TMP_Text resultText;

    public Button BetBtn;

    private bool connectedToServer = false;

    public TMP_InputField AmountField;
    public TMP_InputField TargetField;

    public GameObject rocket;

    private Vector3 initPosition = new Vector3();

    private bool canDischarge = false;

    BetPlayer _player = new BetPlayer();

    private float randomNum;

    // GameReadyStatus Send
    [DllImport("__Internal")]
    private static extern void GameReady(string msg);
    // Start is called before the first frame update
    void Start()
    {
        AmountField.text = "10.0";
        TargetField.text = "2.0";
        info_Text.text = "";
        resultText.text = "0.00x";

        initPosition = rocket.transform.position;
        canDischarge = false;

        io.Connect();

        io.On("connect", (e) =>
        {
            connectedToServer = true;
            Debug.Log("Game started");

            io.On("bet result", (res) =>
            {
                StartCoroutine(BetResult(res));
            });

            io.On("error message", (res) =>
            {
                ShowError(res);
            });
        });

#if UNITY_WEBGL == true && UNITY_EDITOR == false
            GameReady("Ready");
#endif
    }

    // Update is called once per frame
    void Update()
    {
        if (canDischarge)
        {
            rocket.transform.Translate(Vector3.forward * Time.deltaTime * 100f);
        }
        else
            rocket.transform.position = initPosition;
    }

    void ShowError(SocketIOEvent socketIOEvent)
    {
        var res = ReceiveJsonObject.CreateFromJSON(socketIOEvent.data);
        info_Text.text = res.errMessage;
        BetBtn.interactable = true;
    }

    IEnumerator BetResult(SocketIOEvent socketIOEvent)
    {
        var res = ReceiveJsonObject.CreateFromJSON(socketIOEvent.data);
        canDischarge = true;
        randomNum = res.randomNum;
        if (res.gameResult)
        {
            resultText.color = new Color32(0, 255, 17, 255);}
        else
        {
            resultText.color = Color.red;
        }
        StartCoroutine(UpdateResultText(res.randomNum));
        yield return new WaitForSeconds(2f);
        if (res.gameResult)
        {
            info_Text.text = "You Win!  You earned " + float.Parse(AmountField.text).ToString("F2") + " + " + (res.earnAmount - float.Parse(AmountField.text)).ToString("F2") + "!";
        }
        else
        {
            info_Text.text = "You Lose!";
        }
        resultText.text = res.randomNum.ToString("F2") + "x";
        canDischarge = false;
        BetBtn.interactable = true;
        walletAmount_Text.text = res.amount.ToString("F2");        
    }

    IEnumerator UpdateResultText(float random)
    {
        // Animation for increasing and decreasing of coins amount
        const float seconds = 1.5f;
        float elapsedTime = 0;
        while (elapsedTime < seconds)
        {
            resultText.text = Mathf.Lerp(0f, random, (elapsedTime / seconds)).ToString("F2")+"x";
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
    }


    public void RequestToken(string data)
    {
        JSONNode usersInfo = JSON.Parse(data);
        _player.token = usersInfo["token"];
        _player.username = usersInfo["userName"];

        float i_balance = float.Parse(usersInfo["amount"]);
        walletAmount_Text.text = i_balance.ToString("F2");
    }

    public void MinBtn_Clicked()
    {
        AmountField.text = "10.0";
    }

    public void CrossBtn_Clicked()
    {
        float amount = float.Parse(AmountField.text);
        if (amount >= 100000f)
            AmountField.text = "100000.0";
        else
            AmountField.text = (amount * 2.0f).ToString("F2");
    }

    public void HalfBtn_Clicked()
    {
        float amount = float.Parse(AmountField.text);
        if (amount <= 10f)
            AmountField.text = "10.0";
        else
            AmountField.text = (amount / 2.0f).ToString("F2");
    }

    public void MaxBtn_Clicked()
    {
        float myTotalAmount = float.Parse(string.IsNullOrEmpty(walletAmount_Text.text) ? "0" : walletAmount_Text.text);
        if (myTotalAmount >= 100000f)
            AmountField.text = "100000.0";
        else if (myTotalAmount >= 10f && myTotalAmount < 100000f)
            AmountField.text = myTotalAmount.ToString("F2");
    }

    public void AmountField_Changed()
    {
        if (float.Parse(AmountField.text) < 10f)
            AmountField.text = "10.0";
        else if (float.Parse(AmountField.text) > 100000f)
        {
            AmountField.text = "100000.0";
        }
    }

    public void PlusBtn()
    {
        float targetNum = float.Parse(TargetField.text);
        TargetField.text = (targetNum + 1.0f).ToString("F2");
    }

    public void MinusBtn()
    {
        float targetNum = float.Parse(TargetField.text);
        if(targetNum > 2f)
            TargetField.text = (targetNum - 1.0f).ToString("F2");
        else if(targetNum <=2)
            TargetField.text = "1.01";
    }

    public void TargetField_EditEnd()
    {
        if (float.Parse(TargetField.text) <= 1.01f)
            TargetField.text = "1.01";        
    }

    public void BetBtnClicked()
    {
        if (connectedToServer)
        {
            info_Text.text = "";
            JsonType JObject = new JsonType();
            float myTotalAmount = float.Parse(string.IsNullOrEmpty(walletAmount_Text.text) ? "0" : walletAmount_Text.text);
            float betamount = float.Parse(string.IsNullOrEmpty(AmountField.text) ? "0" : AmountField.text);
            float targetNum = float.Parse(string.IsNullOrEmpty(TargetField.text) ? "0" : TargetField.text);
            if (betamount <= myTotalAmount)
            {
                BetBtn.interactable = false;
                JObject.userName = _player.username;
                JObject.betAmount = betamount;
                JObject.token = _player.token;
                JObject.amount = myTotalAmount;
                JObject.targetNum = targetNum;
                io.Emit("bet info", JsonUtility.ToJson(JObject));
            }
            else if (betamount > myTotalAmount)
                info_Text.text = "Insufficient Funds";
        }
        else
        {
            info_Text.text = "Can't connect to Game Server!";
        }
    }
}

public class BetPlayer
{
    public string username;
    public string token;
}