using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class VsCpuTimer : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI _timerText;

    float _timerValue;

    bool _mesuring = false;
    float TimerValue {  
        get { return _timerValue; } 
        set 
        {
            _timerValue = value;
            _timerText.text = _timerValue.ToString("f1");
        } 
    }

    void Update()
    {
        if (_mesuring)
        {
            TimerValue += Time.deltaTime;
        }
    }

    public void Mesure()
    {
        _mesuring = true;
    }

    public void Stop()
    {
        _mesuring = false;
    }

    public void ResetTimer()
    {
        _mesuring = false;
        TimerValue = 0;
    }

    public string GetTime()
    {
        return _timerValue.ToString("f1");
    }
}
