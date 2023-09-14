using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CellInfo
{

    public int X;
    public int Y;
    public bool IsOpend;
    public bool IsFlagged;
    public int WrittenValue; // -1: îöíe, 0--9: ÇªÇÃêîíl,     -2: èâä˙íl
    public bool IsSafeBomb;

    public CellInfo()
    {
        
        IsOpend= false;
        IsFlagged= false;
        WrittenValue= -2;
        IsSafeBomb= false;
    }


    public void SetParams(CellInfo cellInfo)
    {
        IsOpend= cellInfo.IsOpend;
        IsFlagged= cellInfo.IsFlagged;
        WrittenValue= cellInfo.WrittenValue;
        IsSafeBomb= cellInfo.IsSafeBomb;
    }

}
