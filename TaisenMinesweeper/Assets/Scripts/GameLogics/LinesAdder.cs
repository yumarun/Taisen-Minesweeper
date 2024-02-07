using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LinesAdder
{
    readonly int _boardWidth;
    readonly int _boardHeight;

    public static readonly float DURATION_FROM_ADDLINES_CALL = 1f;


    Board.BoardType _boardType;


    public LinesAdder(int boardWidth, int boardHeight, Board.BoardType boardType)
    {
        _boardWidth = boardWidth;
        _boardHeight = boardHeight;
        _boardType = boardType;
    }


    public void AddLines(ref Board board, int addedLinesLength)
    {
        if (addedLinesLength <= 0)
        {
            
            return;
        }

        Debug.Log($"AddLines() called. Length: {addedLinesLength}");

        // ボードの上から3行に未開封のマスがあった場合，プレイヤーは負け
        var (safeMinesNum, unsafeMinesNum) = board.GetExsitingMinesCountInSquare(0, Board.BoardHeight - 1, Board.BoardWidth - 1, Board.BoardHeight - addedLinesLength);
        if (unsafeMinesNum != 0)
        {
            Debug.Log("未開封のマスがありプレイヤーのまけ");
            if (_boardType == Board.BoardType.UserBoardInOnlineMode)
            {
                ClientNetworkManager.SendWinOrLoseResult(false);
            }
            else if (_boardType == Board.BoardType.UserBoardInVsCpuMode)
            {
                VsCpuManager.WinLose = 1;
            }
            else if (_boardType == Board.BoardType.CPUBoardInVsCpuMode)
            {
                VsCpuManager.WinLose = 0;

            }
        }

        var nowBoardState = board.GetState();

        var newBoardState = nowBoardState;

        int minesNumInNewLines = Board.AmountOfMinesAtFirst * addedLinesLength / Board.BoardHeight;
        var newLines = MakeNewLines(addedLinesLength, Board.BoardWidth, minesNumInNewLines);

        


        UpdateBoardStateWithLines(ref newBoardState, newLines); 
        
        board.UpdateWithBoardState(newBoardState, addedLinesLength);

        board.CountAndSetAmountOfAlreadyOpenedCellsAndMines();
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

        // 全行のWritternValueを更新
        for (int y = 0; y < _boardHeight; y++)
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


}
