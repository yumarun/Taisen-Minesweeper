using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    GameObject _startPanel;

    [SerializeField]
    ClientNetworkManager _client;
    bool _matching = false;
    float _matchingElapsedTime = 0;
    const float MATCHING_INTERVAL = 5f;

    bool _battling = false;
    float _battlingElapsedTime = 0;
    const float BATTLING_INTERVAL = 3f;

    [SerializeField]
    MinesweeperManager _minesweeperManager;

    [SerializeField]
    MinimapManager _minimapManager;

    int _amountOfInitOpeningLines = 5;
    public static readonly float BOARD_UNCLICKABLE_DURATION = 3f;

    [SerializeField] GameObject _waitingOpponentPanel;

    [SerializeField] GameObject _panelOnGameFinished;
    [SerializeField] GameObject _textWon;
    [SerializeField] GameObject _textLost;

    bool _onMatchDecided = false;


    bool _friendMatching = false;
    [SerializeField]
    GameObject _friendMatchPanel;

    [SerializeField]
    TMP_InputField _joinFriendRoomInputField;

    string _friendRoomPassword = "";
    bool _OnFriendRoomMakeFinished = false;
    [SerializeField]
    GameObject _friendRoomPasswordPanel;
    [SerializeField]
    TMP_InputField _friendRoomPasswordInputField;

    bool _joinFriendsRoom = false;
    float _joinFriendsRoomElapsedTime = 0;

    GoogleAuthentification _googleAuth;

    // �ʏ�: 0, 
    // �I�� & won: 1,
    // �I�� & los: 2t
    int _onGameFinished;

    [SerializeField]
    UserProfileManager _userProfileManager;


    [SerializeField]
    GameObject _opponentProfilePanel;

    [SerializeField]
    TextMeshProUGUI _opponentNameText;

    [SerializeField]
    TextMeshProUGUI _opponentRatingText;


    void Start()
    {
        if (UserProfileManager.IsProfileSet())
        {
            UserProfileManager.UnactiveateSpeechBubble();
            UserProfileManager.SetNameAndRating();
        }
    }
    void Update()
    {
        if (_matching)
        {
            _matchingElapsedTime += Time.deltaTime;
            if (_matchingElapsedTime > MATCHING_INTERVAL )
            {
                _matchingElapsedTime = 0;
                _client.RequestMatching(
                    UserProfileManager.GetName(), 
                    UserProfileManager.GetToken(), 
                    UserProfileManager.GetRating() );
            }
        }

        if (_friendMatching)
        {
            if (_friendRoomPassword == "")
            {
                _matchingElapsedTime += Time.deltaTime;
                if (_matchingElapsedTime > MATCHING_INTERVAL)
                {
                    _matchingElapsedTime = 0;
                    _client.RequestFriendMatching();
                }
            }
        }

        if (_battling)
        {
            _battlingElapsedTime += Time.deltaTime;
            if (_battlingElapsedTime > BATTLING_INTERVAL )
            {
                _battlingElapsedTime = 0;

                _client.SendBoardInfo(_minesweeperManager.GetBoardState());
            }
        }

        if (_onMatchDecided)
        {
            _onMatchDecided = false;
            _waitingOpponentPanel.SetActive(false);
            SetOpponentProfileToDisplay();
            _opponentProfilePanel.SetActive(true);
        }

        if (_onGameFinished >= 1)
        {
            bool win;
            if (_onGameFinished == 1)
            {
                win = true;
            }
            else
            {
                win = false;    
            }

            if (win)
            {
                _textWon.SetActive(true);
            }
            else
            {
                _textLost.SetActive(true);
            }
            _panelOnGameFinished.SetActive(true);

        }

        if (_OnFriendRoomMakeFinished)
        {
            _OnFriendRoomMakeFinished = false;
            _friendRoomPasswordInputField.text = _friendRoomPassword;
            _friendRoomPasswordPanel.SetActive(true);
        }

        if (_joinFriendsRoom)
        {
            _joinFriendsRoomElapsedTime += Time.deltaTime;
            if (_joinFriendsRoomElapsedTime > MATCHING_INTERVAL )
            {
                _joinFriendsRoomElapsedTime = 0;
                _client.RequestJoinFriendsRoiom(_joinFriendRoomInputField.text);
            }
        }
    }

    public void OnPlayButtonClicked()
    {
        _onGameFinished = 0;
        _client.Init();
        MatchStart();
        _startPanel.SetActive(false);
        _waitingOpponentPanel.SetActive(true);
    }

    public void OnMakeRoomButtonClicked()
    {
        _onGameFinished = 0;
        _client.Init();
        FriendMatchStart();
        _startPanel.SetActive(false);
        _waitingOpponentPanel.SetActive(true);
    }

    void MatchStart()
    {
        _matching = true;
    }

    void FriendMatchStart()
    {
        _friendMatching = true;
    }

    public void OnMatchDecided()
    {
        _onMatchDecided = true;
        InitGame();
        _friendMatching = false;
        _matching = false;
        _joinFriendsRoom = false;
        _battling = true;
    }

    void InitGame()
    {
        _minesweeperManager.GoingInit = true;
        _minesweeperManager.AmountOfInitOpeningLines = _amountOfInitOpeningLines;
        _minimapManager.GoCreateMinimap = true;

    }

    void SetOpponentProfileToDisplay()
    {
        string name;
        int rating;
        (name, rating) = ClientNetworkManager.GetOpponentNameAndRating();
        _opponentNameText.text = name;
        _opponentRatingText.text = rating.ToString();
    }

    public void OnOpponentBoardSent(int[] board, int addedLinesLength)
    {
        _minimapManager.SetMinimapToBuffer(board);

        _minimapManager.GoUpdateMinimap = true;

        _minesweeperManager.AddLines(addedLinesLength);
    }

    public int GetOpenedCellNum()
    {
        return _minesweeperManager.GetOpenedCellNum();
    }

    public void OnGameFinished(bool youWin)
    {
        _battling = false;
        if (youWin)
        {
            if (_onGameFinished == 0)
            {
                _onGameFinished = 1;
            }
        }
        else
        {
            if (_onGameFinished == 0)
            {
                _onGameFinished = 2;
            }
        }
    }

    public void GoStartScene()
    {
        SceneManager.LoadScene("Start");
    }

    public void GoVsCpuScene()
    {
        SceneManager.LoadScene("VsCPU");
    }

    public void OnFriendMatchButtonClicked()
    {
        _friendMatchPanel.SetActive(true);
    }

    public void OnFriendBackButtonClicked()
    {
        _friendRoomPasswordPanel.SetActive(false);
        _friendMatchPanel.SetActive(false);
    }

    public void OnFriendMakeRoomSucceeded(string password)
    {
        _friendRoomPassword = password;
        _OnFriendRoomMakeFinished = true;
    }

    public void OnJoinRoomButtonClicked()
    {

        _onGameFinished = 0;
        _client.Init();
        _startPanel.SetActive(false);
        _waitingOpponentPanel.SetActive(true);
        _joinFriendsRoom = true;
        
    }


    public void GoRuleScene()
    {
        SceneManager.LoadScene("Rules");
    }


    
    void GetUserProfile(string token)
    {
        StartCoroutine(GetUserProfileAsync(token));
    }

    


    IEnumerator GetUserProfileAsync(string token)
    {
        UnityWebRequest req = UnityWebRequest.Post("https://tmapiserver.yumarun.net:8080/api/NameAndRating/", token);
        req.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
        yield return req.SendWebRequest();

        if (req.error != null)
        {
            Debug.Log(req.error);
        }

        Debug.Log(req.downloadHandler.text);
        var res = req.downloadHandler.text;
        var ress = res.Split('\n');
        if (ress[1] == "-1")
        {
            Debug.Log("not registered");
            _userProfileManager.SetToken(token);
            //Debug.Log($"313: {UserProfileManager.GetToken()}");
            _userProfileManager.ActivateNameInputPanel();

        }
        else
        {
            Debug.Log("registered");
            UserProfileManager.SetNameAndRatingAndToken(ress[0], int.Parse(ress[1]), token);
            UserProfileManager.UnactiveateSpeechBubble();
            Debug.Log(UserProfileManager.GetRating());
        }
        

    }

    public void OnLRegisterLoginButtonClicked()
    {
        _googleAuth = new GoogleAuthentification(GetUserProfile);
        _googleAuth.TryAuthorize();
    }

    public void GoRankingScene()
    {
        SceneManager.LoadScene("Ranking");
    }
}
