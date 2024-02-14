using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RankingManager : MonoBehaviour
{
    [SerializeField]
    GameObject _rankingPref;

    [SerializeField]
    GameObject _myRankingObj;

    [SerializeField]
    GameObject _rankingPrefParent;

    [SerializeField]
    Sprite _number1Image;

    [SerializeField]
    Sprite _number2Image;

    [SerializeField]
    Sprite _number3Image;

    [SerializeField]
    Sprite _otherImage;


    void Start()
    {
        StartCoroutine(GetUsersInfo());
    }

    IEnumerator GetUsersInfo()
    {

        // dbから文字列取得
        UnityWebRequest reqGetUsrInfo = UnityWebRequest.Post("https://tmapiserver.yumarun.net:8080/api/GetAllUser/", "");
        reqGetUsrInfo.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
        yield return reqGetUsrInfo.SendWebRequest();

        if (reqGetUsrInfo.error != null)
        {
            Debug.Log(reqGetUsrInfo.error);
        }

        // parse
        var users = reqGetUsrInfo.downloadHandler.text.Split("\n");
        var usersList = new List<string>(users);
        usersList.RemoveAt(usersList.Count - 1);

        

        // sort
        var sortedUsers = usersList.OrderBy(s => -int.Parse(s.Split(",")[1]));
        var sortedUsersArray = sortedUsers.ToArray();   

        // 文字列の数forループでプレハブ作成 & 代入 & 親設定
        for (int i = 0; i < sortedUsersArray.Length; i++)
        {
            var name_rating = sortedUsersArray[i].Split(",");
            var rankingObj = GameObject.Instantiate(_rankingPref) as GameObject;
            rankingObj.transform.SetParent(_rankingPrefParent.transform);
            var rankImage = rankingObj.transform.GetChild(0).Find("Panel_rank").Find("Image_rank").GetComponent<Image>();
            if (i == 0)
            {
                rankImage.sprite = _number1Image;
            }
            else if (i == 1)
            {
                rankImage.sprite = _number2Image;
            }
            else if (i == 2)
            {
                rankImage.sprite = _number3Image;
            }
            else
            {
                rankImage.sprite = _otherImage;
            }
            var rankText = rankingObj.transform.GetChild(0).Find("Panel_rank").Find("Text_rank").GetComponent<TextMeshProUGUI>();
            rankText.text = (i + 1).ToString();
            var nameText = rankingObj.transform.GetChild(0).Find("Text_name").GetComponent<TextMeshProUGUI>();
            nameText.text = name_rating[0];
            var ratingText = rankingObj.transform.GetChild(0).Find("Text_rating").GetComponent<TextMeshProUGUI>();
            ratingText.text = name_rating[1];

            if (name_rating[0] == UserProfileManager.GetName())
            {
                var myRankImage = _myRankingObj.transform.GetChild(0).Find("Panel_rank").Find("Image_rank").GetComponent<Image>();
                if (i == 0)
                {
                    myRankImage.sprite = _number1Image;
                }
                else if (i == 1)
                {
                    myRankImage.sprite = _number2Image;
                }
                else if (i == 2)
                {
                    myRankImage.sprite = _number3Image;
                }
                else
                {
                    myRankImage.sprite = _otherImage;
                }
                var myRankText = _myRankingObj.transform.GetChild(0).Find("Panel_rank").Find("Text_rank").GetComponent<TextMeshProUGUI>();
                myRankText.text = (i + 1).ToString();
                var myNameText = _myRankingObj.transform.GetChild(0).Find("Text_name").GetComponent<TextMeshProUGUI>();
                myNameText.text = name_rating[0];
                var myRatingText = _myRankingObj.transform.GetChild(0).Find("Text_rating").GetComponent<TextMeshProUGUI>();
                myRatingText.text = name_rating[1];
            }
        }

        if (UserProfileManager.GetName() == "")
        {
            _myRankingObj.SetActive(false);
        }
        
    }

    public void BackToStartScene()
    {
        SceneManager.LoadScene("Start");
    }
}
