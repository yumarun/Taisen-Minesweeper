using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using WebSocketSharp;

public class ClientNetworkManager: MonoBehaviour 
{
    UnityEvent _onMatchingFinished;
    [SerializeField] GameManager _gameManager;

    WebSocket _webSocket;
    string _opponentIpAddr = "";
    int _nowBattlingMsgNum = 0;



    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            _onMatchingFinished.Invoke();
        }
    }


    public void Init()
    {
        string url = "";
        using (var sr = new StreamReader("Assets/Scripts/ServerIpAddress.txt"))
        {
            url = "ws://" + sr.ReadToEnd() + "/ws";
        }
        _webSocket = new WebSocket(url);
        _webSocket.OnOpen += OnOpen;
        _webSocket.OnMessage += OnMessage;
        _webSocket.Connect();

        _onMatchingFinished = new UnityEvent();
        _onMatchingFinished.AddListener(_gameManager.OnMatchDecided);
    }

    void OnOpen(object sender, EventArgs e)
    {
        Debug.Log("Connected to server...");
    }

    void OnMessage(object sender, MessageEventArgs e)
    {
        Debug.Log("WebSocket message received: " + e.Data);
        string[] msgsp = e.Data.Split('\n');
        if (msgsp.Length >= 2 && msgsp[0] == "match!op:")
        {
            _opponentIpAddr = msgsp[1];
            Debug.Log($"Matchi finished. OppenentAddr: {_opponentIpAddr}");
            _onMatchingFinished.Invoke();
        }
    }


    public void RequestMatching()
    {
        var msg = new MatchingPhaseMessageToServer("Taro", "123");
        string json = JsonUtility.ToJson(msg);

        _webSocket.Send("matching\n" + json);
    }

    public void SendBoardInfo(int[] board)
    {
        var msg = new BattlingPhaseMessageToServer(board, false, _nowBattlingMsgNum++, 0, _opponentIpAddr);
        string json = JsonUtility.ToJson(msg);

        _webSocket.Send("battling\n" + json);
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

    [Serializable]
    public class BattlingPhaseMessageToServer
    {
        public int[] LatestBoard;
        public bool IsLosed;
        public int LatestMsgNum;
        public int LatestAttackPoint;
        public string OpponentAddr;

        public BattlingPhaseMessageToServer(int[] boardState, bool isLosed, int msgNum, int ap, string opponentIp)
        {
            LatestBoard = boardState;
            IsLosed = isLosed;
            LatestMsgNum = msgNum;
            LatestAttackPoint = ap;
            OpponentAddr = opponentIp;
        }
    }

    void OnDestroy()
    {
        if (_webSocket != null)
        {
            _webSocket.Close();
            _webSocket = null;
        }
    }
}
