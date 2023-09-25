using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour
{
    public int X 
    { 
        get { return _cellInfo.X;  } 
        set { _cellInfo.X = value; } 
    }
    public int Y 
    { 
        get { return _cellInfo.Y; } 
        set { _cellInfo.Y = value; } 
    }
    public bool IsOpend 
    { 
        get { return _cellInfo.IsOpend; } 
        set { _cellInfo.IsOpend = value; } 
    }
    public bool IsFlagged 
    { 
        get { return _cellInfo.IsFlagged; } 
        set { _cellInfo.IsFlagged = value; } 
    }
    public int WrittenValue // -2: 初期値, -1: 爆弾, 0--9: その数値
    { 
        get { return _cellInfo.WrittenValue; } 
        set { _cellInfo.WrittenValue = value; _wval = value; } 
    }

    public bool IsSafeBomb
    {
        get { return _cellInfo.IsSafeBomb; }
        private set { _cellInfo.IsSafeBomb= value; }
    }



    [SerializeField]
    int _wval = -5; // あとでけす

    CellInfo _cellInfo;

    [SerializeField]
    CellImageAsset _cellImageAsset;
    public void Initialize(int x, int y)
    {
        _cellInfo = new CellInfo();
        X = x;
        Y = y;
        IsOpend= false;
        IsFlagged= false;
    }

    public void Open(Sprite sprite, ref int amountOfOpendCells, bool fromClick)
    {
        if (IsFlagged)
        {
            return;
        }

        IsOpend= true;
        gameObject.GetComponent<SpriteRenderer>().sprite= sprite;

        if (fromClick)
        {
            amountOfOpendCells++;
        }

        UIManager.SetOpenedCountText(amountOfOpendCells);
    }

    public void Flag(Sprite flagSprite)
    {
        gameObject.GetComponent<SpriteRenderer>().sprite= flagSprite;
        Debug.Log("フラグされた");
    }

    public void UnFlag(Sprite unOpendSprite)
    {
        gameObject.GetComponent<SpriteRenderer>().sprite = unOpendSprite;
    }


    public CellInfo GetInfo()
    {
        return _cellInfo;
    }

    void SetAllParams(bool isOpened, bool isFlagged, int writtenValue)
    {
        IsOpend= isOpened;
        IsFlagged= isFlagged;
        WrittenValue= writtenValue;
    }

    public void UpdateCell(CellInfo info)
    {
        
        // 数値変換
        SetAllParams(info.IsOpend, info.IsFlagged, info.WrittenValue);

        // sprite変更
        var sr = gameObject.GetComponent<SpriteRenderer>();
        if (IsOpend)
        {
            sr.sprite = _cellImageAsset._numberImages[info.WrittenValue];
        }
        else
        {
            if (IsFlagged)
            {
                sr.sprite = _cellImageAsset._unOpenedFlagedImage;
            }
            else
            {
                if (info.IsSafeBomb)
                {
                    sr.sprite = _cellImageAsset._mineDefusedCellImg;
                }
                else
                {
                    sr.sprite = _cellImageAsset._unOpenedUnflagedImage;
                }
            }

        }
    }

    public void MaskSafeMineCell()
    {
        var sr = gameObject.GetComponent<SpriteRenderer>();
        sr.sprite = _cellImageAsset._mineDefusedCellImg;
        IsSafeBomb = true;
    }
}


