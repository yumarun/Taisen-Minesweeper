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
            _durationBetweenAttack = 0.5f;
        }
    }

    public (int, int, int) Action(CellInfo[,] cpuInfo, float deltaTime)
    {
        // arg1
        //  -2: won! 
        //  -1: do nothing
        //  0: left click
        //  1: right click
        // arg1, arg2
        //  -1, -1: arg1 == -1 
        //  otherwise: x, y, that is coordinates to be operated
        



        _elapsedTimeSincePrevAction += deltaTime;
        if (_elapsedTimeSincePrevAction > _durationBetweenAttack)
        {
            _elapsedTimeSincePrevAction = 0;

            var (openableCells, checkableCells) = Calclate(cpuInfo);




            bool existingOpenableCellsOrCheckableCells = false;



            for (int y = Board.BoardHeight - 1; y >= 0; y--)
            {
                for (int x = 0; x < Board.BoardWidth; x++)
                {
                    if (openableCells[y, x])
                    {
                        existingOpenableCellsOrCheckableCells = true;
                        return (0, x, y);

                    }
                }
            }
            for (int y = Board.BoardHeight - 1; y >= 0; y--)
            {
                for (int x = 0; x < Board.BoardWidth; x++)
                {
                    if (checkableCells[y, x])
                    {
                        existingOpenableCellsOrCheckableCells = true;
                        return (1, x, y);
                    }
                }
            }

            if (!existingOpenableCellsOrCheckableCells)
            {
                // select a unopened cell from the board.
                for (int y = Board.BoardHeight - 1; y >= 0; y--)
                {
                    for (int x = 0; x < Board.BoardWidth; x++)
                    {
                        
                        if (!cpuInfo[y, x].IsOpend && !cpuInfo[y, x].IsSafeBomb)
                        {
                            return (0, x, y);
                        }
                    }
                }
            }

            return (-2, -1, -1);

        }

        return (-1, -1, -1);
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

                    if (cpuInfo[ny, nx].IsFlagged || cpuInfo[ny, nx].IsSafeBomb)
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

        j1 = 1;
        while (j1 < cellSets.Count)
        {
            for (int i = 0; i < j1; i++)
            {
                if (cellSets[i].Item1.Count > 0 && cellSets[j1].Item1.Count > 0)
                {

                    if (cellSets[i].Item1.Count > cellSets[j1].Item1.Count)
                    {
                        continue;
                        CheckCellSets(cellSets[j1], cellSets[i]);
                    }
                    else if (cellSets[i].Item1.Count < cellSets[j1].Item1.Count)
                    {
                        continue;

                        CheckCellSets(cellSets[i], cellSets[j1]);
                    }
                }
            }
            j1++;
        }

        

        void CheckCellSets((SortedSet<(int, int)>, int) subset, (SortedSet<(int, int)>, int) superset)
        {
            Debug.Log(205);
            var subset2 = ChangeSet2(subset.Item1);
            Debug.Log(207);

            var super2 = ChangeSet2(superset.Item1);
            Debug.Log(208);


            if (!subset2.IsSubsetOf(super2)) { return; }

            var diffBoms = superset.Item2 - subset.Item2;
            super2.ExceptWith(subset2);


            var diffCellSet = ChangeSet1(super2);

            Debug.Log(221);


            string subset_str = "";
            string super_str = "";
            string diff_str = "";
            foreach (var item in subset.Item1)
            {
                subset_str += item.ToString() + " ";
            }
            foreach (var item in superset.Item1)
            {
                super_str += item.ToString() + " "; 
            }
            foreach (var item in diffCellSet)
            {
                diff_str += item.ToString() + " ";  
            }


            Debug.Log($"subset: {subset_str}, super: {super_str}, diff: {diff_str}");

            if (diffBoms == 0)
            {
                foreach (var (y, x) in diffCellSet)
                {
                    Debug.Log($"from 215 {y} {x}");
                    openableCells[y, x] = true;
                }
            }
            else if (diffBoms == diffCellSet.Count)
            {
                foreach (var (y, x) in diffCellSet)
                {
                    checkableCells[y, x] = true;
                }
            }
            else
            {
                bool exists = false;
                foreach (var cellset in cellSets)
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

        return (openableCells, checkableCells);
    }

    bool InBoard(int y, int x)
    {
        return (0 <= y && y < Board.BoardHeight && 0 <= x && x < Board.BoardWidth);
    }

    SortedSet<(int, int)> ChangeSet1(SortedSet<int> st)
    {
        var ret = new SortedSet<(int, int)>();
        foreach (var now in st)
        {
            ret.Add((now / Board.BoardWidth, now % Board.BoardWidth));
        }
        return ret;
    }

    SortedSet<int> ChangeSet2(SortedSet<(int, int)> st)
    {
        var ret = new SortedSet<int>();
        foreach (var now in st)
        {
            ret.Add(now.Item1 * Board.BoardWidth + now.Item2);
        }
        return ret;
    }
}
