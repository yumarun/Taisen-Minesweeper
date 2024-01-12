using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.Networking;

public class UserProfileManager : MonoBehaviour
{
    static string _name = "";
    static int _rating = -1;
    static string _token = "";

    [SerializeField]
    TMP_InputField _nameInputField;

    [SerializeField]
    GameObject _invalidNameText;

    [SerializeField]
    GameObject _deplicatedNameAlermText;

    [SerializeField]
    GameObject _nameInputPanel;

    public static void SetNameAndRatingAndToken(string name, int rating, string token)
    {
        _name = name;
        _rating = rating;
        _token = token;

        GameObject.Find("Text_name").GetComponent<TextMeshProUGUI>().text = name;
        GameObject.Find("Text_rating").GetComponent<TextMeshProUGUI>().text = _rating.ToString();

    }

    public void ActivateNameInputPanel()
    {
        _nameInputPanel.SetActive(true);
    }

    public static void SetNameAndRating()
    {

        GameObject.Find("Text_name").GetComponent<TextMeshProUGUI>().text = _name;
        GameObject.Find("Text_rating").GetComponent<TextMeshProUGUI>().text = _rating.ToString();

    }

    public static string GetName()
    {
        return _name;
    }

    public static int GetRating()
    {
        return _rating;
    }

    public static string GetToken()
    {
        return _token;
    }

    public void SetToken(string token)
    {
        _token = token;
    }

    public static bool IsProfileSet()
    {
        return !(_rating == -1);
    }

    public static void UnactiveateSpeechBubble()
    {
        GameObject.Find("Panel_unableToRank").SetActive(false);
    }

    public void Register()
    {
        StartCoroutine(RegisterAsync());
    }

    IEnumerator RegisterAsync()
    {

        if (!IsValidName(_nameInputField.text))
        {
            _invalidNameText.SetActive(true);
            _deplicatedNameAlermText.SetActive(false);

        }
        else
        {
            // データベースに名前が存在するか検索
            UnityWebRequest reqCheckName = UnityWebRequest.Post("https://tmapiserver.yumarun.net:8080/api/CheckDeplicateName/", _nameInputField.text);
            reqCheckName.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
            yield return reqCheckName.SendWebRequest();

            if (reqCheckName.error != null)
            {
                Debug.Log(reqCheckName.error);
            }

            if (reqCheckName.downloadHandler.text != "ok")
            {
                _deplicatedNameAlermText.SetActive(true);
                _invalidNameText.SetActive(false);

            }
            else
            {
                _invalidNameText.SetActive(false);
                _deplicatedNameAlermText.SetActive(false);


                // unity側に, 名前と1500を設定
                _name = _nameInputField.text;
                _rating = 1500;

                Debug.Log(122);
                // アカウントと, 名前・レート(1500)を設定
                UnityWebRequest req = UnityWebRequest.Post("https://tmapiserver.yumarun.net:8080/api/Register/", $"{_token} {_name}");
                // Debug.Log($"{_token} {_name}");
                req.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
                yield return req.SendWebRequest();

                if (req.error != null)
                {
                    Debug.Log(req.error);
                }


                // UI更新
                _nameInputPanel.SetActive(false);
                UnactiveateSpeechBubble();
                SetNameAndRating();
            }

            
        }


    }


    bool IsValidName(string name)
    {
        if (name.Length >= 21)
        {
            return false;
        }

        for (int i = 0; i < name.Length; i++)
        {
            bool ok = ('A' <= name[i] && name[i] <= 'Z'
                || 'a' <= name[i] && name[i] <= 'z'
                || '0' <= name[i] && name[i] <= '9'
                || name[i] == '-'
                || name[i] == '_');

            if (!ok)
            {
                return false;
            }
        }

        return true;
    }

    public void OnBackButtonPushed()
    {
        _nameInputPanel.SetActive(false );
    }

}
