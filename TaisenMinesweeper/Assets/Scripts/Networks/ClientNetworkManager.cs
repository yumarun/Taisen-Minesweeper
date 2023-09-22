using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using WebSocketSharp;

public class ClientNetworkManager
{
    WebSocket _webSocket;
    UnityAction _onMatchingFinished;
    string _opponentIpAddr = "";

    public ClientNetworkManager(UnityAction OnMatchingFinished)
    {
        string url = ""; 
        using (var sr = new StreamReader("Assets/Scripts/ServerIpAddress.txt"))
        {
            url = "ws://" + sr.ReadToEnd() + "/ws";
        }
        _onMatchingFinished = OnMatchingFinished;
        _webSocket = new WebSocket(url);
        _webSocket.OnOpen += OnOpen;
        _webSocket.OnMessage += OnMessage;
        _webSocket.Connect();
    }

    void OnOpen(object sender, EventArgs e)
    {
        Debug.Log("Connected to server...");
    }

    void OnMessage(object sender, MessageEventArgs e)
    {
        Debug.Log("WebSocket message received: " + e.Data);
        string[] msgsp = e.Data.Split(' ');
        if (msgsp.Length >= 2 && msgsp[0] == "match!!oponent:")
        {
            _opponentIpAddr = msgsp[1];
            Debug.Log($"Matchi finished. OppenentAddr: {_opponentIpAddr}");
            _onMatchingFinished();
        }
    }

    public void RequestMatching()
    {
        var msg = new MatchingPhaseMessageToServer("Taro", "123");
        string json = JsonUtility.ToJson(msg);

        _webSocket.Send("matching\n" + json);
    }

    [Serializable]
    public class MatchingPhaseMessageToServer
    {
        public string State;
        public string Message;

        public MatchingPhaseMessageToServer(string state, string message)
        {
            State = state;
            Message = message;
        }
    }
}
