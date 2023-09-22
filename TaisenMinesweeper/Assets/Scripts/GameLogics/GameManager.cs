using System.Collections;
using System.Collections.Generic;
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
    const float BATTLING_INTERVAL = 5f;

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
                _client.SendBoardInfo();
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
        _matching = false;
        _battling = true;
    }
}
