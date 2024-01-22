using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

public class ClientNetworkManager: MonoBehaviour 
{
    UnityEvent _onMatchingFinished;
    [SerializeField] GameManager _gameManager;

    static HybridWebSocket.WebSocket _webSocket;
    static string _opponentIpAddr = "";
    int _nowBattlingMsgNum = 0;


    public void Init()
    {
        string url = "wss://tmserver.yumarun.net:8080/ws";
        _webSocket = HybridWebSocket.WebSocketFactory.CreateInstance(url);
        _webSocket.OnOpen += OnOpen;
        _webSocket.OnMessage += OnMessage;
        _webSocket.Connect();
        _onMatchingFinished = new UnityEvent();
        _onMatchingFinished.AddListener(_gameManager.OnMatchDecided);

        _opponentIpAddr = "";

    }

    private void OnOpen()
    {
        Debug.Log("Connected to server...");
    }

 

    void OnMessage(byte[] bytes)
    {
        var bytes_str = Encoding.UTF8.GetString(bytes);

        Debug.Log("WebSocket message received: " + bytes_str);
        string[] msgsp = bytes_str.Split('\n');

        if (msgsp.Length == 1)
        {
            if (msgsp[0] == "opponent disconnected. you win.")
            {
                Debug.Log("opponent disconnected.you win.");
                _gameManager.OnGameFinished(true);
            }
            else if (msgsp[0] == "YouWon")
            {
                Debug.Log("You won!!!!!!!");
                _gameManager.OnGameFinished(true);
            }
            else if (msgsp[0] == "YouLost")
            {
                Debug.Log("You lost...........");
                _gameManager.OnGameFinished(false);
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
            else if (msgsp[0] == "friendmatchok")
            {
                _opponentIpAddr = msgsp[1];
                Debug.Log($"friend matchi finished. OppenentAddr: {_opponentIpAddr}");
                _onMatchingFinished.Invoke();
            }
            else if (msgsp[0] == "makeroomfinished")
            {
                Debug.Log($"make room succeeded. password: {msgsp[1]}");
                _gameManager.OnFriendMakeRoomSucceeded(msgsp[1]);
            }
            else if (msgsp[0] == "battling")
            {
                var msg = JsonUtility.FromJson<BattlingPhaseMessageFromServer>(msgsp[1]);

                _gameManager.OnOpponentBoardSent(msg.LatestBoard, msg.LatestAttackPoint / 10);
            }
            else if (msgsp[0] == "SendNewRating")
            {
                Debug.Log($"newRating set: {msgsp[1]}");
                UserProfileManager.SetNameAndRatingAndToken(UserProfileManager.GetName(), int.Parse(msgsp[1]), UserProfileManager.GetToken());
            }
            else 
            {
                Debug.Log("unregistered message received 2...........");
            }
        }
    }


    public void RequestMatching(string name, string subject, int rating)
    {
        

        var msg = new MatchingPhaseMessageToServer(name, subject, rating);
        string json = JsonUtility.ToJson(msg);

        _webSocket.Send(Encoding.UTF8.GetBytes("matching\n" + json));
    }

    public void RequestFriendMatching()
    {
       

        var msg = new JsonOnFriendMatchRequest("makeroom", "_");
        string json = JsonUtility.ToJson(msg);

        _webSocket.Send(Encoding.UTF8.GetBytes("friendmatching\n" + json));
    }

    public void RequestJoinFriendsRoiom(string password)
    {
        var msg = new JsonOnFriendMatchRequest("joinroom", password);
        string json = JsonUtility.ToJson(msg);

        Debug.Log(json == null);
        Debug.Log(_webSocket == null);

        _webSocket.Send(Encoding.UTF8.GetBytes("friendmatching\n" + json));
    }

    public void SendBoardInfo(int[] board)
    {
        
        var amountOfOpenedCells = _gameManager.GetOpenedCellNum();
        var msg = new BattlingPhaseMessageToServer(board, false, false, _nowBattlingMsgNum++, amountOfOpenedCells, _opponentIpAddr, UserProfileManager.GetName(), UserProfileManager.GetRating(), UserProfileManager.GetToken());
        string json = JsonUtility.ToJson(msg);

        _webSocket.Send(Encoding.UTF8.GetBytes("battling\n" + json));
    }

    [Serializable]
    public class MatchingPhaseMessageToServer
    {
        public string Name;
        public string Subject;
        public int Rating;

        public MatchingPhaseMessageToServer(string name, string subject, int rating)
        {
            Name = name;
            Subject = subject;
            Rating = rating;
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
        public string Name;
        public int Rating;
        public string Subject;

        public BattlingPhaseMessageToServer(int[] boardState, bool lost, bool won, int msgNum, int ap, string opponentIp, string name, int rating, string subject)
        {
            LatestBoard = boardState;
            Lost = lost;
            Won = won;
            LatestMsgNum = msgNum;
            TotalNumberOfUsrDefusedCells = ap;
            OpponentAddr = opponentIp;
            Name = name;
            Rating = rating;
            Subject = subject;
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

    [Serializable]
    public class JsonOnFriendMatchRequest
    {
        public string Call;
        public string Arg;
        public JsonOnFriendMatchRequest(string call, string arg)
        {
            Call = call;
            Arg = arg;
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
            var msg = new BattlingPhaseMessageToServer(new int[0], false, true, -1, -1, _opponentIpAddr, UserProfileManager.GetName(), UserProfileManager.GetRating(), UserProfileManager.GetToken());
            string json = JsonUtility.ToJson(msg);

            _webSocket.Send(Encoding.UTF8.GetBytes("battling\n" + json));
        }
        else
        {
            var msg = new BattlingPhaseMessageToServer(new int[0], true, false, -1, -1, _opponentIpAddr, UserProfileManager.GetName(), UserProfileManager.GetRating(), UserProfileManager.GetToken());
            string json = JsonUtility.ToJson(msg);

            _webSocket.Send(Encoding.UTF8.GetBytes("battling\n" + json));
        }
    }
}
