using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="CellImages", menuName ="ScriptableObjects/CellImages")]
public class CellImageAsset : ScriptableObject
{
    public Sprite _unOpenedUnflagedImage;

    public Sprite _unOpenedFlagedImage;

    public Sprite[] _numberImages;

    public Sprite _mineCellImage;

    public Sprite _mineDefusedCellImg;
}
