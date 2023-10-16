using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

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

    void Update()
    {
        if (_matching)
        {
            _matchingElapsedTime += Time.deltaTime;
            if (_matchingElapsedTime > MATCHING_INTERVAL )
            {
                _matchingElapsedTime = 0;
                _client.RequestMatching();
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
    }

    public void OnPlayButtonClicked()
    {
        _client.Init();
        MatchStart();
        _startPanel.SetActive(false);
    }

    void MatchStart()
    {
        _matching = true;
    }

    public void OnMatchDecided()
    {
        Debug.Log("MATCH_DECIDED");
        InitGame();

        _matching = false;
        _battling = true;
    }

    void InitGame()
    {
        _minesweeperManager.GoingInit = true;
        _minesweeperManager.AmountOfInitOpeningLines = _amountOfInitOpeningLines;
        _minimapManager.GoCreateMinimap = true;
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

    public void OnGameFinished()
    {
        _battling = false;
    }
}
