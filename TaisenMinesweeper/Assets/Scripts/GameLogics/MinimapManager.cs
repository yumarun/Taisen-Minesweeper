using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapManager : MonoBehaviour
{
    // -1: 初期値
    // 0-8: 空いてる状態のその値
    // 9: 普通のflag
    // 10: check


    [SerializeField]
    CellImageAsset _cellImageAsset;

    [SerializeField]
    GameObject _cellPrefab;

    [SerializeField]
    GameObject _minimapPanel;

    [HideInInspector]
    public bool GoCreateMinimap = false;

    [HideInInspector]
    public bool GoUpdateMinimap = false;

    const float _cellSize = 2.4f;
    const float _cellOffset = 10f;

    int[] _opponentBoardBuffer;
    Image[] _map;

    void Update()
    {
        if (GoCreateMinimap)
        {
            CreateMinimap(Board.BoardWidth, Board.BoardHeight);
            InitializeBuffer(Board.BoardWidth, Board.BoardHeight);
            GoCreateMinimap = false;
        }

        if (GoUpdateMinimap)
        {
            UpdateMinimap();
            GoUpdateMinimap = false;
        }
    }
    void CreateMinimap(int boardWidth, int boardHeight)
    {
        _map = new Image[boardWidth * boardHeight];
        for (int i = 0; i < boardHeight; i++)
        {
            for (int j = 0; j < boardWidth; j++)
            {
                GameObject cell = Instantiate(_cellPrefab) as GameObject;
                cell.transform.SetParent(_minimapPanel.transform);
                cell.GetComponent<RectTransform>().localPosition = new Vector3(_cellOffset + j * 5 * _cellSize - 200, _cellOffset + i * 5 * _cellSize - 200, 0);
                cell.GetComponent<RectTransform>().localScale = new Vector3(_cellSize, _cellSize, 1);
                _map[i * boardWidth + j] = cell.GetComponent<Image>();
            }
        }
    }

    void InitializeBuffer(int boardWidth, int boardHeight)
    {
        _opponentBoardBuffer = new int[boardWidth * boardHeight];
        for (int i = 0; i < boardHeight; i++)
        {
            for (int j = 0; j < boardWidth; j++)
            {
                _opponentBoardBuffer[i * boardWidth + j] = -1;
            }
        }

    }


    public void SetMinimapToBuffer(int[] board)
    {
        _opponentBoardBuffer = board;
    }
    void UpdateMinimap() 
    {
        // バッファの情報からminimapを更新
        for (int y = 0; y < Board.BoardHeight; y++)
        {
            for (int x = 0; x < Board.BoardWidth; x++)
            {
                if (_opponentBoardBuffer[y * Board.BoardWidth + x] == -1)
                {
                    _map[y * Board.BoardWidth + x].sprite = _cellImageAsset._unOpenedUnflagedImage;
                }
                else if (_opponentBoardBuffer[y * Board.BoardWidth + x] == 9)
                {
                    _map[y * Board.BoardWidth + x].sprite = _cellImageAsset._unOpenedFlagedImage;
                }
                else if (_opponentBoardBuffer[y * Board.BoardWidth + x] == 10)
                {
                    _map[y * Board.BoardWidth + x].sprite = _cellImageAsset._mineDefusedCellImg;
                }
                else
                {

                    var numberInCell = _opponentBoardBuffer[y * Board.BoardWidth + x];
                    _map[y * Board.BoardWidth + x].sprite = _cellImageAsset._numberImages[numberInCell];
                }
            }
        }
    }
}
