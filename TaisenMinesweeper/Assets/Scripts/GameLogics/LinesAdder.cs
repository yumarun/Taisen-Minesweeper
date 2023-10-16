using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LinesAdder
{
    readonly int _boardWidth;
    readonly int _boardHeight;


    public LinesAdder(int boardWidth, int boardHeight)
    {
        _boardWidth = boardWidth;
        _boardHeight = boardHeight;
    }


    public void AddLines(ref Board board, int addedLinesLength)
    {
        Debug.Log($"addedLinesLength: {addedLinesLength}");

        if (addedLinesLength == 0)
        {
            
            return;
        }

        // ボードの上から3行に未開封のマスがあった場合，プレイヤーは負け
        var (safeMinesNum, unsafeMinesNum) = board.GetExsitingMinesCountInSquare(0, Board.BoardHeight - 1, Board.BoardWidth - 1, Board.BoardHeight - addedLinesLength);
        if (unsafeMinesNum != 0)
        {
            Debug.Log("未開封のマスがありプレイヤーのまけ");
        }

        


        var nowBoardState = board.GetState();

        int amountOfOpenedCellsToBeErased = GetAmountOfOpenedCellsToBeErased(addedLinesLength, nowBoardState);
        board.AddAmountOfErasedCells(amountOfOpenedCellsToBeErased);

        var newBoardState = nowBoardState;

        int minesNumInNewLines = Board.AmountOfMinesAtFirst * addedLinesLength / Board.BoardHeight;
        //Debug.Log($"minesNumInNewLines: {minesNumInNewLines}");
        var newLines = MakeNewLines(addedLinesLength, Board.BoardWidth, minesNumInNewLines);


        UpdateBoardStateWithLines(ref newBoardState, newLines); 
        
        board.UpdateWithBoardState(newBoardState, addedLinesLength);

        // Board._amountOfMinesを更新
        var addedMinesNum = GetMinesNumInNewLines(board);


        board.AddAmountOfMines(addedMinesNum - (safeMinesNum + unsafeMinesNum));

    }

    int GetAmountOfOpenedCellsToBeErased(int addedLinesLength, CellInfo[,] oldBoardState)
    {
        int amountOfOpenedCellsToBeErased = 0;
        for (int i = 0; i < addedLinesLength; i++)
        {
            for (int j = 0; j < _boardWidth; j++)
            {
                CellInfo cell = oldBoardState[_boardHeight - i - 1, j];
                if (cell.IsOpend && !cell.IsSafeBomb)
                {
                    amountOfOpenedCellsToBeErased++;
                }
            }
        }

        Debug.Log($"amountOfOpenedCellsToBeErased: {amountOfOpenedCellsToBeErased}");
        return amountOfOpenedCellsToBeErased;
    }

    CellInfo[,] MakeNewLines(int height, int width, int mineNum)
    {
        var ret = new CellInfo[height, width];
        var minePositions = new List<int>();
        while (minePositions.Count != mineNum) 
        {
            var np = Random.Range(0, height * width - 1);
            if (!minePositions.Contains(np))
            {
                minePositions.Add(np);
            }
        }

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                ret[i, j] = new CellInfo();
                ret[i, j].X = j;
                ret[i, j].Y = i;
            }
        }

        foreach(var mpos in minePositions)
        {
            ret[(int)mpos / width, mpos % width].WrittenValue = -1;
        }

        

        return ret;
    }

    void UpdateBoardStateWithLines(ref CellInfo[,] dstState, CellInfo[,] newLines)
    {


        var nLinesHeight = newLines.GetLength(0);
        //Debug.Log($"nLinesHeight: {nLinesHeight}");


        // ボードを上に3行ずらす & 新しい3行追加
        for (int i = 0; i < _boardHeight - nLinesHeight; i++)
        {
            for (int j = 0; j < _boardWidth; j++)
            {
                var prevY = _boardHeight - 1 - i - nLinesHeight;
                var nxtY = _boardHeight - 1 - i;
                dstState[nxtY, j].SetParams(dstState[prevY, j]);
            }
        }
        for (int i = 0; i < nLinesHeight; i++)
        {
            for (int j = 0; j < _boardWidth; j++)
            {
                dstState[i, j].SetParams(newLines[i, j]);
            }
        }

        // 0~4行までのWritternValueを更新
        for (int y = 0; y < nLinesHeight + 1; y++)
        {
            for (int x = 0; x < _boardWidth; x++)
            {
                if (dstState[y, x].WrittenValue == -1)
                {
                    continue;
                }

                int[] dx = new int[] { -1, -1, -1, 0, 0, 1, 1, 1 };
                int[] dy = new int[] { 1, -1, 0, 1, -1, 1, -1, 0 };

                int mineCount = 0;
                for (int k = 0; k < 8; k++)
                {
                    int ny = y + dy[k];
                    int nx = x + dx[k];
                    if (ny < 0 || ny >= Board.BoardHeight || nx < 0 || nx >= Board.BoardWidth)
                    {
                        continue;
                    }
                    if (dstState[ny, nx].WrittenValue == -1)
                    {
                        mineCount++;
                    }
                }

                if (mineCount >= 0)
                {
                    dstState[y, x].WrittenValue = mineCount;
                } 
                else
                {
                    dstState[y, x].WrittenValue = -1;

                }
            }
        }
        
        
    }

    int GetMinesNumInNewLines(Board board)
    {
        var (newSafeMinesNum, newUnsafeMinesNum) = board.GetExsitingMinesCountInSquare(0, 2, _boardWidth-1, 0);
        return newSafeMinesNum + newUnsafeMinesNum;
    }
}
