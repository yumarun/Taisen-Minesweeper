using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

public class Board
{
    public enum BoardType
    {
        UserBoardInOnlineMode,
        UserBoardInVsCpuMode,
        CPUBoardInVsCpuMode
    }

    public static readonly int BoardHeight = 15;
    public static readonly int BoardWidth = 15;
    public static readonly int AmountOfMinesAtFirst = 45;


    

    GameObject _cellPrefab;

    Cell[,] _cells;

    int _amountOfMines;



    int _amountOfOpenedCellsFromClick;
    int _amountOfAutomaticallyOpenedCells;
    int _amountOfAlreadyOpenedCells;



    CellImageAsset _cellImageAsset;


    int _flagCount;

    BoardType _boardType;

    public static readonly string CELLS_PARENT_GAMEOBJECT_NAME_FOR_CPU = "cellsParentForCPU";
    public static readonly string CELLS_PARENT_GAMEOBJECT_NAME_FOR_USR = "cellsParentForUsr";

    GameObject _cellsParentForCPU;
    GameObject _cellsParentForUsr;

    public Board(CellImageAsset cellImages, GameObject cellPrefab, BoardType boardType) // TODO: vsCPUでなくenum値を渡すように
    {
        _cellImageAsset = cellImages;
        _cellPrefab = cellPrefab;
        _cells = new Cell[BoardHeight, BoardWidth];
        _amountOfOpenedCellsFromClick = 0;
        _amountOfAutomaticallyOpenedCells = 0;
        _amountOfAlreadyOpenedCells = 0;
        _amountOfMines = AmountOfMinesAtFirst;
        _boardType = boardType;
        _flagCount = AmountOfMinesAtFirst;
    }

    

    public void Make(CellInfo[,] tmpCellsInfo)
    {
        if (_boardType == BoardType.UserBoardInVsCpuMode)
        {
            _cellsParentForUsr = new GameObject(CELLS_PARENT_GAMEOBJECT_NAME_FOR_USR);

        }
        else if (_boardType == BoardType.CPUBoardInVsCpuMode)
        {
            _cellsParentForCPU = new GameObject(CELLS_PARENT_GAMEOBJECT_NAME_FOR_CPU);
        }
        for (int i = 0; i < BoardHeight; i++)
        {
            for (int j = 0; j < BoardWidth; j++)
            {
                if (_boardType == BoardType.UserBoardInOnlineMode)
                {
                    var cell = GameObject.Instantiate(_cellPrefab) as GameObject;
                    cell.GetComponent<SpriteRenderer>().sprite = _cellImageAsset._unOpenedUnflagedImage;
                    cell.GetComponent<Cell>().Initialize(j, i); // xとy代入
                    cell.transform.position = new Vector3(j, i, 0);

                    _cells[i, j] = cell.GetComponent<Cell>();
                    _cells[i, j].WrittenValue = tmpCellsInfo[i, j].WrittenValue;
                }
                else if (_boardType == BoardType.UserBoardInVsCpuMode)
                {
                    var cell = GameObject.Instantiate(_cellPrefab) as GameObject;
                    cell.tag = "UserCell";
                    cell.GetComponent<SpriteRenderer>().sprite = _cellImageAsset._unOpenedUnflagedImage;
                    cell.GetComponent<Cell>().Initialize(j, i); // xとy代入
                    cell.transform.position = new Vector3(j, i, 0);
                    cell.transform.parent = _cellsParentForUsr.transform;

                    _cells[i, j] = cell.GetComponent<Cell>();
                    _cells[i, j].WrittenValue = tmpCellsInfo[i, j].WrittenValue;
                }
                else if (_boardType == BoardType.CPUBoardInVsCpuMode)
                {

                    var cell = GameObject.Instantiate(_cellPrefab) as GameObject;
                    cell.GetComponent<SpriteRenderer>().sprite = _cellImageAsset._unOpenedUnflagedImage;
                    cell.GetComponent<Cell>().Initialize(j, i); // xとy代入
                    cell.transform.position = new Vector3(j, i, 0);
                    cell.transform.parent = _cellsParentForCPU.transform;

                    _cells[i, j] = cell.GetComponent<Cell>();
                    _cells[i, j].WrittenValue = tmpCellsInfo[i, j].WrittenValue;
                }

            }
        }

        if (_boardType == BoardType.UserBoardInVsCpuMode)
        {
            _cellsParentForUsr.transform.position = new Vector3(-6f, -4.6f);
            _cellsParentForUsr.transform.localScale = Vector3.one * 0.5f;

        }
        else if (_boardType == BoardType.CPUBoardInVsCpuMode)
        {
            _cellsParentForCPU.transform.position = new Vector3(3.8f, -3f);
            _cellsParentForCPU.transform.localScale = Vector3.one * 0.3f;
        }

        
    }

    public bool TryOpenCell(int y, int x, bool fromClick)
    {
        bool clickedMine = false;

        if (_cells[y, x].IsOpend)
        {
            return clickedMine;
        }

        if (_cells[y, x].IsFlagged)
        {
            return clickedMine;
        }

        if (_cells[y, x].IsSafeBomb)
        {
            return clickedMine;
        }

        // 爆弾を引いたとき
        if (_cells[y, x].WrittenValue == -1)
        {
            if (fromClick)
            {
                OnExploded();
                clickedMine = true;
            }
            return clickedMine;
        }
        // 数字マスを引いたとき // TODO: 周りの爆弾を見つけてその爆弾について，checkを付けたりする
        if (_cells[y, x].WrittenValue > 0)
        {
            _cells[y, x].Open(_cellImageAsset._numberImages[_cells[y, x].WrittenValue], ref _amountOfOpenedCellsFromClick, ref _amountOfAutomaticallyOpenedCells, fromClick);
            SearchSafeMine(y, x);
            if (IsNowMeetingClearConditions())
            {
                OnClear();
            }

            return clickedMine;
        }

        // 空白マスを引いたとき
        int[] dx = new int[] { -1, 0, 0, 1, -1, -1, 1, 1 };
        int[] dy = new int[] { 0, -1, 1, 0, 1, -1, -1, 1 };

        Stack<(int, int)> dfsStack = new Stack<(int, int)>();
        dfsStack.Push((y, x));
        while (dfsStack.Count != 0)
        {
            var top = dfsStack.Pop();

            var ny = top.Item1;
            var nx = top.Item2;

            if (_cells[ny, nx].IsOpend)
            {
                continue;
            }

            _cells[ny, nx].Open(_cellImageAsset._numberImages[0], ref _amountOfOpenedCellsFromClick, ref _amountOfAutomaticallyOpenedCells, fromClick);
            SearchSafeMine(ny, nx);


            for (int k = 0; k < 8; k++)
            {
                var nny = ny + dy[k];
                var nnx = nx + dx[k];

                if (nny < 0 || nny >= BoardHeight || nnx < 0 || nnx >= BoardWidth)
                {
                    continue;
                }

                if (_cells[nny, nnx].IsOpend)
                {
                    continue;
                }

                if (_cells[nny, nnx].WrittenValue == 0)
                {

                    dfsStack.Push((nny, nnx));
                }
                else
                {
                    _cells[nny, nnx].Open(_cellImageAsset._numberImages[_cells[nny, nnx].WrittenValue], ref _amountOfOpenedCellsFromClick, ref _amountOfAutomaticallyOpenedCells, fromClick);
                    SearchSafeMine(nny, nnx);

                }
            }
        }


        if (IsNowMeetingClearConditions())
        {
            OnClear();
            return clickedMine;
        }

        return clickedMine;

    }


    void OnExploded()
    {
        if (_boardType == BoardType.UserBoardInOnlineMode)
        {
            MinesweeperManager.GoMakeBoardUnClickable = true;
        }
        else
        {
            // TODO
            Debug.Log("you misclicked in cpu mode.");
        }

    }

    public void TryFlagCell(int y, int x)
    {
        if (_cells[y, x].IsOpend)
        {
            return;
        }

        if (_cells[y, x].IsSafeBomb)
        {
            return;
        }

        if (_cells[y, x].IsFlagged)
        {
            _cells[y, x].IsFlagged = false;
            _flagCount++;
            _cells[y, x].UnFlag(_cellImageAsset._unOpenedUnflagedImage);
        }
        else
        {
            if (_flagCount > 0)
            {
                _cells[y, x].IsFlagged = true;
                _flagCount--;
                _cells[y, x].Flag(_cellImageAsset._unOpenedFlagedImage);
            }
        }
    }

    public (int, int) GetExsitingMinesCountInSquare(int leftUpCoordX, int leftUpCoordY, int rightDownCoordX, int rightDownCoordY) 
    {
        int safeMinesCount = 0;
        int allMinesCount = 0;
        for (int i = leftUpCoordY; i >= rightDownCoordY; i--)
        {
            for (int j = leftUpCoordX; j <= rightDownCoordX; j++)
            {
                if (_cells[i, j].WrittenValue == -1)
                {
                    allMinesCount++;
                    if (_cells[i, j].IsSafeBomb)
                    {
                        safeMinesCount++;
                    }
                }
            }
        }
        return (safeMinesCount, allMinesCount - safeMinesCount);
    }

    

    bool IsNowMeetingClearConditions()
    {

        if (_amountOfOpenedCellsFromClick + _amountOfAutomaticallyOpenedCells + _amountOfAlreadyOpenedCells 
            == BoardHeight * BoardWidth - _amountOfMines) 
        {
            return true;
        }
        return false;
    }
    void OnClear()
    {
        Debug.Log("<color=yellow>Clear!!!</color>");
        if (_boardType == BoardType.UserBoardInOnlineMode)
        {
            ClientNetworkManager.SendWinOrLoseResult(true);
        }
        else if (_boardType == BoardType.UserBoardInVsCpuMode)
        {
            VsCpuManager.WinLose = 0;
        }
        else if (_boardType == BoardType.CPUBoardInVsCpuMode)
        {
            VsCpuManager.WinLose = 1;
        }
    }

    public CellInfo[,] GetState()
    {
        var ret = new CellInfo[BoardHeight, BoardWidth];
        for (int i = 0; i < BoardHeight; i++)
        {
            for (int j = 0; j < BoardWidth; j++)
            {
                ret[i, j] = _cells[i, j].GetInfo();
            }
        }

        return ret;
    }

    public void UpdateWithBoardState(CellInfo[,] cells, int addedLinesLength)
    {
        // セルのGameObject自体は消さずに，中身だけ変える
        // 画像も更新        
        for (int y = 0; y < BoardHeight; y++)
        {
            for (int x = 0; x < BoardWidth; x++)
            {
                _cells[y, x].UpdateCell(cells[y, x]);
            }
        }

        // 増やした行の一番上の行で，周りに爆弾がなかったら空ける
        for (int x = 0; x < BoardWidth; x++)
        {
            if (_cells[addedLinesLength, x].WrittenValue == 0)
            {
                TryOpenCell(addedLinesLength - 1, x, false);
                if (x != 0)
                {
                    TryOpenCell(addedLinesLength - 1, x - 1, false);
                }
                if (x != BoardWidth - 1)
                {
                    TryOpenCell(addedLinesLength - 1, x + 1, false);
                }
            }
        }

    }

    void SearchSafeMine(int y, int x)
    {

        for (int i = -1; i < 2; i++)
        {
            for (int j = -1; j < 2; j++)
            {
                
                var nx = x + j;
                var ny = y + i;
                if (IsOutOfBoardRange(nx, ny))
                {
                    continue;
                }

                if (_cells[ny, nx].WrittenValue != -1)
                {
                    continue;
                }


                bool isSafe = true;
                int[] dx = { -1, -1, -1, 0, 0, 1, 1, 1 };
                int[] dy = { -1, 0, 1, 1, -1, 1, 0, -1 };
                for (int k = 0; k < 8; k++)
                {
                    var nnx = nx + dx[k];
                    var nny = ny + dy[k];
                    if (IsOutOfBoardRange(nnx, nny))
                    {
                        continue;
                    }
                    var isOpendNum = (_cells[nny, nnx].IsOpend && _cells[nny, nnx].WrittenValue >= 0);
                    var isUnopenedMine = (!_cells[nny, nnx].IsOpend && _cells[nny, nnx].WrittenValue == -1);
                    if (!(isOpendNum || isUnopenedMine))
                    {
                        isSafe= false;
                        break;
                    }
                }
                if (isSafe)
                {
                    _cells[ny, nx].MaskSafeMineCell();
                }
            }
        }
    }

    bool IsOutOfBoardRange(int x, int y)
    {
        return (x < 0 || x >= Board.BoardWidth || y < 0 || y >= Board.BoardHeight);
    }



    public int GetAmountOfOpenedCells()
    {
        return _amountOfOpenedCellsFromClick;
    }

    public void CountAndSetAmountOfAlreadyOpenedCellsAndMines()
    {
        int count = 0;
        int minesCount = 0;
        for (int i = 0; i < Board.BoardHeight; i++)
        {
            for (int j = 0; j <  Board.BoardWidth; j++)
            {
                if (_cells[i, j].IsOpend && 0 <= _cells[i, j].WrittenValue && _cells[i, j].WrittenValue <= 9)
                {
                    count++;
                }

                if (_cells[i, j].WrittenValue == -1)
                {
                    minesCount++;
                }
            }
        }

        _amountOfAlreadyOpenedCells = count;
        _amountOfOpenedCellsFromClick = 0;
        _amountOfAutomaticallyOpenedCells = 0;
        _amountOfMines = minesCount;
    }

    
}