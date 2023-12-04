using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class RulePageController : MonoBehaviour
{
    [SerializeField]
    GameObject[] _pages;

    [SerializeField]
    Image _leftBtnImage;

    [SerializeField]
    Image _rightBtnImage;

    [SerializeField]
    Sprite _leftArrow;

    [SerializeField]
    Sprite _rightArrow;

    [SerializeField]
    Sprite _homeImage;

    int _nowPage;

    void Start()
    {
        _nowPage = 0;  
    }

    public void OnRightBtnClicked()
    {
        if (_nowPage == _pages.Length - 1 )
        {
            SceneManager.LoadScene("Start");

        }
        else
        {
            _nowPage++;
            _pages[_nowPage].SetActive(true);
            _pages[_nowPage - 1].SetActive(false);
            if (_nowPage == 1)
            {
                _leftBtnImage.sprite = _leftArrow;
            }
            if (_nowPage == _pages.Length -1 )
            {
                _rightBtnImage.sprite = _homeImage;
            }
        }
    }

    public void OnLeftBtnClicked()
    {
        if (_nowPage == 0)
        {
            SceneManager.LoadScene("Start");
        }
        else
        {
            _nowPage--;
            _pages[_nowPage].SetActive(true);
            _pages[_nowPage + 1].SetActive(false);
            if (_nowPage == 0)
            {
                _leftBtnImage.sprite = _homeImage;
            }
            if (_nowPage == _pages.Length - 2)
            {
                _rightBtnImage.sprite = _rightArrow;
            }

        }
    }

    
}
