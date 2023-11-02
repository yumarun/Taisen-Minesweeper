using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CpuBehaveDecider 
{

    int _level;
    float _durationBetweenAttack;
    float _elapsedTimeSincePrevAction = 0f;

    readonly int[] _dx = new int[] { -1, 0, 1, -1, 1, -1, 0, 1 };
    readonly int[] _dy = new int[] { 1, 1, 1, 0, 0, -1, -1, -1 };


    public CpuBehaveDecider(int level)
    {
        _level = level;
        if (_level == 1)
        {
            _durationBetweenAttack = 5f;
        }
    }

    public (int, int, int) Action(CellInfo[,] cpuInfo, CellInfo usrInfo, float deltaTime)
    {
        // arg1
        //  -2: 負け
        //  -1: 何もしない
        //  0: 左クリック
        //  1: 右クリック
        // arg1, arg2
        //  -1, -1: arg1 == -1 or -2
        //  otherwise: x, y, that is coordinates to be operated
        (int, int, int) ret;

        

        _elapsedTimeSincePrevAction += deltaTime;
        if (_elapsedTimeSincePrevAction > _durationBetweenAttack)
        {
            _elapsedTimeSincePrevAction = 0;

            var (a, b) = Calclate(cpuInfo);
            
        }

        
    }

    (bool[,], bool[,]) Calclate(CellInfo[,] cpuInfo)
    {
        var openableCells = new bool[Board.BoardHeight, Board.BoardWidth];
        var checkableCells = new bool[Board.BoardHeight, Board.BoardWidth];

        for (int i = 0; i < Board.BoardHeight; i++)
        {
            for (int j = 0; j < Board.BoardWidth; j++)
            {
                openableCells[i, j] = false;
                checkableCells[i, j] = false;
            }
        }

        var cellSets = new List<(SortedSet<(int, int)>, int)>();

        for (int y = Board.BoardHeight - 1; y >= 0; y--)
        {
            for (int x = 0; x < Board.BoardWidth; x++)
            {
                if (!cpuInfo[y, x].IsOpend) { continue; }
                if (!(1 <= cpuInfo[y, x].WrittenValue && cpuInfo[y, x].WrittenValue <= 8)) { continue; }

                var unopenedCells = new List<int>();
                var flaggedCells = new List<int>();

                for (int k = 0; k < 8; k++)
                {
                    var ny = y + _dy[k];
                    var nx = x + _dx[k];
                    if (ny < 0 || ny >= Board.BoardHeight || nx < 0 || nx >= Board.BoardWidth) { continue; }

                    if (cpuInfo[ny, nx].IsFlagged)
                    {
                        flaggedCells.Add(k);
                    }
                    else if (!cpuInfo[ny, nx].IsOpend)
                    {
                        unopenedCells.Add(k);
                    }
                }

                var remainingBomsNum = cpuInfo[y, x].WrittenValue - flaggedCells.Count;
                if (remainingBomsNum == 0)
                {
                    for (int k = 0; k < unopenedCells.Count; k++)
                    {
                        openableCells[y + _dy[unopenedCells[k]], x + _dx[unopenedCells[k]]] = true;
                    }
                }
                else if (remainingBomsNum == unopenedCells.Count)
                {
                    for (int k = 0; k < unopenedCells.Count; k++)
                    {
                        checkableCells[y + _dy[unopenedCells[k]], x + _dx[unopenedCells[k]]] = true;
                    }
                }
                else if (0 < remainingBomsNum && remainingBomsNum < unopenedCells.Count)
                {
                    var cellSet = new SortedSet<(int, int)>();
                    for (int k = 0; k < unopenedCells.Count; k++)
                    {
                        cellSet.Add((y + _dy[unopenedCells[k]], x + _dx[unopenedCells[k]]));
                    }
                    cellSets.Add((cellSet, remainingBomsNum));
                }
            }
        }

        var j1 = cellSets.Count - 1;
        int i1;
        while (j1 >= 1)
        {
            i1 = j1 - 1;
            while (i1 >= 0)
            {
                if (cellSets[i1].Equals(cellSets[j1]))
                {
                    cellSets.Remove(cellSets[j1]);
                    break;
                }
                i1--;
            }
            j1--;
        }

        for (int i = 0; i <  cellSets.Count; i++)
        {
            for (int j = 0; j < i; j++)
            {

            }
        }

        void CheckCellSets((SortedSet<(int, int)>, int) subset, (SortedSet<(int, int)>, int) superset)
        {
            if (!subset.Item1.IsSubsetOf(superset.Item1)) { return; }

            var diffBoms = superset.Item2 - subset.Item2;
            superset.Item1.ExceptWith(subset.Item1);
            var diffCellSet = superset.Item1;

            if (diffBoms == 0)
            {
                foreach( var (y, x) in diffCellSet )
                {
                    openableCells[y, x] = true;
                }
            }
            else if (diffBoms == diffCellSet.Count)
            {
                foreach( var (y, x) in diffCellSet)
                {
                    checkableCells[y, x] = true;
                }
            }
            else
            {
                bool exists = false;
                foreach(var cellset in cellSets)
                {
                    if (cellset.Item1 == diffCellSet)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    cellSets.Add((diffCellSet, diffBoms));
                }
            }
        }

    }

    bool InBoard(int y, int x)
    {
        return (0 <= y && y < Board.BoardHeight && 0 <= x && x < Board.BoardWidth);
    }
}
