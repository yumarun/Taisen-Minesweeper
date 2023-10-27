using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CpuController : MonoBehaviour
{
    bool _initialized;
    bool _running;

    Board _board;

    void Start()
    {
        _initialized = false;   
    }

    

    public void Iniialize(int level)
    {
        //    

        _initialized = true;
    }

    public void Run()
    {
        if (!_initialized)
        {
            Debug.LogError("CPU is not initizlized and was tried to run.");
            return;
        }

        MakeBoard();

        _running = true;
    }

    public void Process() // �Ԃ�l��void����Ȃ��ς���
    {
        if (!_running)
        {
            Debug.LogError("CPU isn't running and can't process.");
            return;
        }
    }

    public void UpdateWithOpponentState()
    {
        if (!_running)
        {
            Debug.LogError("CPU isn't running and can't update with opponent state.");
            return;
        }
    }

    void MakeBoard()
    {

    }
}
