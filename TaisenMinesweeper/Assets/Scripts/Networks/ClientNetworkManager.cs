using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using WebSocketSharp;

public class ClientNetworkManager: MonoBehaviour 
{
    UnityEvent _onMatchingFinished;
    [SerializeField] GameManager _gameManager;

    static WebSocket _webSocket;
    string _opponentIpAddr = "";
    int _nowBattlingMsgNum = 0;


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

        if (msgsp.Length == 1)
        {
            if (msgsp[0] == "opponent disconnected. you win.")
            {
                Debug.Log("opponent disconnected.you win.");
                _gameManager.OnGameFinished();
            }
            else if (msgsp[0] == "YouWon")
            {
                Debug.Log("You won!!!!!!!");
                _gameManager.OnGameFinished();
            }
            else if (msgsp[0] == "YouLost")
            {
                Debug.Log("You lost...........");
                _gameManager.OnGameFinished();
            }
            else
            {
                Debug.Log("unregistered message received 1...........");
            }
        }
        else if (msgsp.Length >= 2)
        {
            if (msgsp[0] == "match!op:")
            {
                _opponentIpAddr = msgsp[1];
                Debug.Log($"Matchi finished. OppenentAddr: {_opponentIpAddr}");
                _onMatchingFinished.Invoke();
            }
            else if (msgsp[0] == "battling")
            {
                // ���json�����
                var msg = JsonUtility.FromJson<BattlingPhaseMessageFromServer>(msgsp[1]);

                _gameManager.OnOpponentBoardSent(msg.LatestBoard, msg.LatestAttackPoint / 10);
            }
            else 
            {
                Debug.Log("unregistered message received 2...........");
            }
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
        var amountOfOpenedCells = _gameManager.GetOpenedCellNum();
        var msg = new BattlingPhaseMessageToServer(board, false, false, _nowBattlingMsgNum++, amountOfOpenedCells, _opponentIpAddr);
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
        public bool Lost;
        public bool Won;
        public int LatestMsgNum;
        public int TotalNumberOfUsrDefusedCells;
        public string OpponentAddr;

        public BattlingPhaseMessageToServer(int[] boardState, bool lost, bool won, int msgNum, int ap, string opponentIp)
        {
            LatestBoard = boardState;
            Lost = lost;
            Won = won;
            LatestMsgNum = msgNum;
            TotalNumberOfUsrDefusedCells = ap;
            OpponentAddr = opponentIp;
        }
    }

    [Serializable]
    public class BattlingPhaseMessageFromServer
    {
        public int[] LatestBoard;
        public bool IsLosed;
        public int LatestMsgNum;
        public int LatestAttackPoint;
        public string OpponentAddr;

        public BattlingPhaseMessageFromServer(int[] boardState, bool isLosed, int msgNum, int ap, string opponentIp)
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

    public static void SendWinOrLoseResult(bool win)
    {
        if (win)
        {
            var msg = new BattlingPhaseMessageToServer(new int[0], false, true, -1, -1, "-1");
            string json = JsonUtility.ToJson(msg);

            _webSocket.Send("battling\n" + json);
        }
        else
        {
            var msg = new BattlingPhaseMessageToServer(new int[0], true, false, -1, -1, "-1");
            string json = JsonUtility.ToJson(msg);

            _webSocket.Send("battling\n" + json);
        }
    }
}
