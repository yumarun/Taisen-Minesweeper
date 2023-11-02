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
    int _level;

    int _amountOfTotalOpenedCellsFromClick;
    int _amountOfTotalInflictedDamageToUser;

    void Start()
    {
        _initialized = false;   
    }

    

    public void Initialize(int level, CellImageAsset cellImageAsset, GameObject cellPrefab)
    {
        _level = level;
        _cellImageAsset = cellImageAsset;
        _cellPrefab = cellPrefab;
        MakeBoard();
        _amountOfTotalOpenedCellsFromClick = 0;
        _amountOfTotalInflictedDamageToUser = 0;
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

    public bool Process() // �Ԃ�l��void����Ȃ��ς���
    {
        var youLost = false;

        if (!_running)
        {
            Debug.LogError("CPU isn't running and can't process.");
            return youLost;
        }

        Debug.Log("Runnning!");



        return youLost;
    }

    public void UpdateWithOpponentState() // �����ǉ�
    {
        if (!_running)
        {
            Debug.LogError("CPU isn't running and can't update with opponent state.");
            return;
        }
    }

    public int GetInfo()
    {
        var damageToUser = VsCpuManager.CalcDamage(ref _amountOfTotalInflictedDamageToUser, 
            _amountOfTotalOpenedCellsFromClick);
        return damageToUser;
    }

    void MakeBoard()
    {
        _board = new Board(_cellImageAsset, _cellPrefab, Board.BoardType.CPUBoardInVsCpuMode);

        var tmpCellsinfo = MakeCellsInfo(Board.BoardHeight, Board.BoardWidth, Board.AmountOfMinesAtFirst);

        _board.Make(tmpCellsinfo);



        _addLines = new LinesAdder(Board.BoardWidth, Board.BoardHeight);


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
                if (dst[i, j].WrittenValue == -1) // ���e�Ȃ�continue
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
