using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[Serializable]
public class UIManager: System.Object
{
    [SerializeField]
    TextMeshProUGUI _FlagCountText;

    [SerializeField]
    TextMeshProUGUI _openedCountText;

    public void InitializeUI(int amountOfMine)
    {
        _FlagCountText.text = amountOfMine.ToString();
    }

    public void IncrementFlagCount()
    {
        if (_FlagCountText != null)
        {
            int nowFlagCount = int.Parse(_FlagCountText.text);
            _FlagCountText.text = (nowFlagCount + 1).ToString();
        }
    }

    public void DecrementFlagCount()
    {
        if (_FlagCountText != null)
        {
            int nowFlagCount = int.Parse(_FlagCountText.text);
            _FlagCountText.text = (nowFlagCount - 1).ToString();

        }
    }

    public int GetFlagCount()
    {
        return int.Parse(_FlagCountText.text);
    }

    public static void SetOpenedCountText(int count)
    {
        var opoenedCountText = GameObject.Find("Text_OpenedCnt").GetComponent<TextMeshProUGUI>();
        opoenedCountText.text = count.ToString();
    }

    
}
