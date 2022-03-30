using UnityEngine;
using System.Collections;

public class JsonType
{
    public string userName;
    public float betAmount;
    public float targetNum;
    public string token;
    public float amount;
}

public class ReceiveJsonObject
{
    public double amount;
    public bool gameResult;
    public float earnAmount;
    public float randomNum;
    public string errMessage;
    public ReceiveJsonObject()
    {
    }
    public static ReceiveJsonObject CreateFromJSON(string data)
    {
        return JsonUtility.FromJson<ReceiveJsonObject>(data);
    }
}