using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CpuController : MonoBehaviour
{
    bool _initialized;
    bool _running;

    Board _board;

    CellImageAsset _cellImageAsset;
    GameObject _cellPrefab;

    LinesAdder _addLines;

    int _amountOfTotalOpenedCellsFromClick;
    int _amountOfTotalInflictedDamageToUser;

    CpuBehaveDecider _behaveDecider;

    void Start()
    {
        _initialized = false;   
    }

    

    public void Initialize(int level, CellImageAsset cellImageAsset, GameObject cellPrefab)
    {
        _cellImageAsset = cellImageAsset;
        _cellPrefab = cellPrefab;
        MakeBoard();
        _amountOfTotalOpenedCellsFromClick = 0;
        _amountOfTotalInflictedDamageToUser = 0;
        _behaveDecider = new CpuBehaveDecider(level);
        _addLines = new LinesAdder(Board.BoardWidth, Board.BoardHeight, Board.BoardType.CPUBoardInVsCpuMode);
        _initialized = true;
    }

    public void StartRunning()
    {
        if (!_initialized)
        {
            Debug.LogError("CPU is not initizlized and was tried to run.");
            return;
        }


        _running = true;
    }

    public int Process() // ï‘ÇËílÇÕvoidÇ∂Ç·Ç»Ç≠ïœÇ¶ÇÈ
    {
        // 0: neigher WON nor LOST
        // 1: Cpu lost
        // 2: Cpu won
        var ret = 0;

        if (!_running)
        {
            Debug.LogError("CPU isn't running and can't process.");
            return 0;
        }

        _amountOfTotalOpenedCellsFromClick = _board.GetAmountOfOpenedCells();

        var (action, x, y) = _behaveDecider.Action(_board.GetState(), Time.deltaTime);
        if (action == -1)
        {
            //Debug.Log("Do nothing..");
            return 0;
        }
        else if (action == 0)
        {
            _board.TryOpenCell(y, x, true);
        }
        else if (action == 1)
        {
            _board.TryFlagCell(y, x);
        }
        else if (action == -2)
        {
            return 2;
        }

        return ret;
    }

    

    public void UpdateWithOpponentState(int addedLines) // à¯êîí«â¡
    {
        if (!_running)
        {
            Debug.LogError("CPU isn't running and can't update with opponent state.");
            return;
        }

        _addLines.AddLines(ref _board, addedLines);
    }

    public int GetInfo()
    {

        var damageToUser = VsCpuManager.CalcDamage(ref _amountOfTotalInflictedDamageToUser, 
            _amountOfTotalOpenedCellsFromClick);
        return damageToUser / 10;
    }

    void MakeBoard()
    {
        _board = new Board(_cellImageAsset, _cellPrefab, Board.BoardType.CPUBoardInVsCpuMode);

        var tmpCellsinfo = MakeCellsInfo(Board.BoardHeight, Board.BoardWidth, Board.AmountOfMinesAtFirst);

        _board.Make(tmpCellsinfo);

        _addLines = new LinesAdder(Board.BoardWidth, Board.BoardHeight, Board.BoardType.CPUBoardInVsCpuMode);

        for (int i = 0; i < VsCpuManager.AmountOfInitOpeningLines; i++)
        {
            for (int j = 0; j < Board.BoardWidth; j++)
            {
                _board.TryOpenCell(Board.BoardHeight - i - 1, j, false);
            }
        }
        _board.CountAndSetAmountOfAlreadyOpenedCellsAndMines();
    }

    CellInfo[,] MakeCellsInfo(int boardHeight, int boardWidth, int AmountOfMines)
    {
        var dst = new CellInfo[boardHeight, boardWidth];
        for (int i = 0; i < boardHeight; i++)
        {
            for (int j = 0; j < boardWidth; j++)
            {
                dst[i, j] = new CellInfo();

            }
        }

        List<int> minePoss = new List<int>();
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

        int[] dx = new int[] { -1, -1, -1, 0, 0, 1, 1, 1 };
        int[] dy = new int[] { -1, 0, 1, -1, 1, -1, 0, 1 };

        for (int i = 0; i < boardHeight; i++)
        {
            for (int j = 0; j < boardWidth; j++)
            {
                if (dst[i, j].WrittenValue == -1) // îöíeÇ»ÇÁcontinue
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
}
