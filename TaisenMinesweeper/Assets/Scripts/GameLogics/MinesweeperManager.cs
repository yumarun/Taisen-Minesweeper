using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinesweeperManager : MonoBehaviour
{

    [SerializeField]
    GameObject _cellPrefab;


    [SerializeField]
    UIManager _uiManager;
    
    Board _board;

    LinesAdder _addLines;

    [SerializeField]
    CellImageAsset _cellImageAsset;

    public bool GoingInit = false;
    bool _initializeFinished = false;

    
    public void Init()
    {

        _board = new Board(_cellImageAsset, _cellPrefab, _uiManager);

        var tmpCellsinfo = MakeCellsInfo(Board.BoardHeight, Board.BoardWidth, Board.AmountOfMinesAtFirst);

        _board.Make(tmpCellsinfo);


        _uiManager.InitializeUI(Board.AmountOfMinesAtFirst);


        _addLines = new LinesAdder(Board.BoardWidth, Board.BoardHeight);


    }
    

    void Update()
    {

        if (GoingInit)
        {
            Init();
            GoingInit = false;
            _initializeFinished = true;
        }

        if (_initializeFinished)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    var clickedCell = hit.collider.gameObject.GetComponent<Cell>();
                    _board.TryOpenCell(clickedCell.Y, clickedCell.X, true);
                }
            }

            if (Input.GetMouseButtonDown(1))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    var clickedCell = hit.collider.gameObject.GetComponent<Cell>();
                    _board.TryFlagCell(clickedCell.Y, clickedCell.X);
                }
            }
        }
        
    }

    
    public static CellInfo[,] MakeCellsInfo(int boardHeight, int boardWidth, int AmountOfMines)
    {
        var dst = new CellInfo[boardHeight, boardWidth];
        for (int i = 0; i < boardHeight; i++)
        {
            for (int j = 0; j < boardWidth; j++)
            {
                dst[i, j] = new CellInfo();
                
            }
        }

        List<int> minePoss= new List<int>();
        while (minePoss.Count < AmountOfMines)
        {
            int val = UnityEngine.Random.Range(0, boardHeight * boardWidth - 1);
            if (!minePoss.Contains(val))
            {
                minePoss.Add(val);
            }
        }

        foreach (var val in minePoss)
        {
            dst[val / boardWidth, val % boardWidth].WrittenValue = -1;
        }

        int[] dx = new int[]{ -1, -1, -1, 0, 0, 1, 1, 1 };
        int[] dy = new int[]{ -1, 0, 1, -1, 1, -1, 0, 1 };

        for (int i = 0; i < boardHeight; i++)
        {
            for (int j = 0; j < boardWidth; j++)
            {
                if (dst[i, j].WrittenValue == -1) // ”š’e‚È‚çcontinue
                {
                    continue;
                }

                int mineCount = 0;
                for (int k = 0; k < 8; k++)
                {
                    int ny = i + dy[k];
                    int nx = j + dx[k];
                    if (nx < 0 || boardWidth <= nx || ny < 0 || boardHeight <= ny)
                    {
                        continue;
                    }

                    if (dst[ny, nx].WrittenValue == -1)
                    {
                        mineCount++;
                    }
                }
                dst[i, j].WrittenValue = mineCount;
            }
        }


        return dst;
    }

    public void OnAddLines()
    {
        _addLines.AddLines(ref _board);

    }

    public int[] GetBoardState()
    {
        var ret = new int[Board.BoardWidth * Board.BoardHeight];

        var nowState = _board.GetState();
        for (int y = 0; y < Board.BoardHeight; y++)
        {
            for (int x = 0; x < Board.BoardWidth; x++)
            {
                if (nowState[y, x].IsSafeBomb)
                {
                    ret[x + y * Board.BoardWidth] = 10;
                }
                else if (nowState[y, x].IsFlagged)
                {
                    ret[x + y * Board.BoardWidth] = 9;

                }
                else if (!nowState[y, x].IsOpend)
                {
                    ret[x + y * Board.BoardWidth] = -1;
                }
                else
                {
                    ret[x + y * Board.BoardWidth] = nowState[y, x].WrittenValue;
                }
            }
        }

        return ret;
    }

    public int GetOpenedCellNum()
    {
        return _board.GetAmountOfOpenedCells();
    }
}