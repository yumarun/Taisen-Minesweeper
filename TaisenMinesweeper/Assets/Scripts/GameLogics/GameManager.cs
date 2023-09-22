using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    GameObject _startPanel;


    ClientNetworkManager _client;
    bool _matching;
    float _matchingElapsedTime = 0;
    float MATCHING_INTERVAL = 5f;

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
    }

    public void OnPlayButtonClicked()
    {
        _client = new ClientNetworkManager(OnMatchDecided);
        MatchStart();
        _startPanel.SetActive(false);
    }

    void MatchStart()
    {
        _matching = true;
    }

    void OnMatchDecided()
    {
        Debug.Log("MATCH_DECIDED");
    }
}
