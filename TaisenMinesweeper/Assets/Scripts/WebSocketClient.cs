using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using WebSocketSharp;


public class WebSocketClient : MonoBehaviour
{
    private WebSocket webSocket;

    private void Start()
    {
        string url = ""; // 接続するWebSocketサーバーのurl
        using (var sr = new StreamReader("Assets/Scripts/ServerIpAddress.txt"))
        {
            url="ws://"+sr.ReadToEnd()+"/ws";
        }
        webSocket = new WebSocket(url);
        webSocket.OnOpen += OnOpen;
        webSocket.OnMessage += OnMessage;
        webSocket.Connect();
    }

    private void OnDestroy()
    {
        if (webSocket != null)
        {
            webSocket.Close();
            webSocket = null;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            if (webSocket != null)
            {
                var json = JsonSerializer("matching", "AAAA");
                webSocket.Send("matching\n" + json);
            }
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            if (webSocket != null)
            {
                webSocket.Send("battling\n" + JsonSerializer2());
            }
        }

    }

    private void OnOpen(object sender, EventArgs e)
    {
        Debug.Log("WebSocket opened");
        var json = JsonSerializer("hello", "Hello, WebSocket server!");
        webSocket.Send("hello\n" + json);
    }

    private void OnMessage(object sender, MessageEventArgs e)
    {
        Debug.Log("WebSocket message received: " + e.Data);
    }

    string JsonSerializer(string state, string message)
    {
        var messageToServer = new MessageToServer(state, message);

        string json = JsonUtility.ToJson(messageToServer);

        return json;
    }

    string JsonSerializer2()
    {
        var msg = new BattlingPhaseMessageToServer();
        var json = JsonUtility.ToJson(msg);
        Debug.Log($"BattlingJson Serizelize test: {json}");
        return json;
    }

    [Serializable]
    public class MessageToServer
    {
        public string State;
        public string Message;

        public MessageToServer(string state, string message)
        {
            State = state;
            Message = message;
        }
    }

    [Serializable]
    public class BattlingPhaseMessageToServer
    {
        public int[] LatestBoard;
        public bool IsLosed;
        public int LatestMsgNum;
        public int LatestAttackPoint;
        public string OpponentAddr;

        public BattlingPhaseMessageToServer()
        {
            LatestBoard = new int[5] { 1, 2, 4, 3, 4};
            IsLosed = true;
            LatestMsgNum = 2;
            LatestAttackPoint = 3;
            OpponentAddr = "123:345";
        }
    }
}
