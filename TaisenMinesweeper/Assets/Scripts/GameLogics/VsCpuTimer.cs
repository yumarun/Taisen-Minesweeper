using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class VsCpuTimer : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI _timerText;

    float _timerValue;
    public float TimerValue {  
        get { return _timerValue; } 
        set 
        {
            _timerValue = value;
            _timerText.text = _timerValue.ToString("f1");
        } 
    }

}
