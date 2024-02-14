using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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


    bool _goAddLines = false;
    int _addedLinesLength = 0;
    float _elapsedTimeFromAddLinesCall = 0;

    public int AmountOfInitOpeningLines = -1; // will be initialized when match start.

    bool _unclickableFromMissClick = false;
    public static bool GoMakeBoardUnClickable = false;
    float _elapsedTimeSinceBoardBecameUnclickable = 0;
    [SerializeField] GameObject _mineClickedNotificationPanel;

    [SerializeField] GameObject _timeTextUntilAddLinesRun;

    bool _doingReadyGo = false;
    float _elapsedTimeFromStartReadyGo = 0;
    [SerializeField] GameObject _readyGoPanel;
    [SerializeField] GameObject _readyGoText;
    
    public void Init()
    {
        _board = new Board(_cellImageAsset, _cellPrefab, Board.BoardType.UserBoardInOnlineMode);

        var tmpCellsinfo = MakeCellsInfo(Board.BoardHeight, Board.BoardWidth, Board.AmountOfMinesAtFirst);

        _board.Make(tmpCellsinfo);


        _uiManager.InitializeUI(Board.AmountOfMinesAtFirst);


        _addLines = new LinesAdder(Board.BoardWidth, Board.BoardHeight, Board.BoardType.UserBoardInOnlineMode);


        // TODO: è„êîçsÇÃÉZÉãÇãÛÇØÇÈ
        for (int i = 0; i < AmountOfInitOpeningLines; i++)
        {
            for (int j = 0; j < Board.BoardWidth; j++)
            {
                
                _board.TryOpenCell(Board.BoardHeight - i - 1, j, false);
            }
        }

        _board.CountAndSetAmountOfAlreadyOpenedCellsAndMines();
    }
    

    void Update()
    {

        if (GoingInit)
        {
            Init();
            GoingInit = false;
            _initializeFinished = true;
            _doingReadyGo = true;
            //Debug.Log("Initizalize finished.");
        }

        if (_doingReadyGo)
        {
            _elapsedTimeFromStartReadyGo += Time.deltaTime;
            _readyGoPanel.SetActive(true);
            if (_elapsedTimeFromStartReadyGo >= 1)
            {
                if (_readyGoText.GetComponent<TextMeshProUGUI>().text != "Go!")
                {
                    _readyGoText.GetComponent<TextMeshProUGUI>().text = "Go!";
                }

                if (_elapsedTimeFromStartReadyGo >= 1.5f)
                {
                    _readyGoPanel.SetActive(false);
                    _doingReadyGo = false;
                }

            }

        }

        if (_initializeFinished)
        {
            

            if (!_unclickableFromMissClick && !_doingReadyGo)
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

        if (_goAddLines)
        {
            if (!_timeTextUntilAddLinesRun.activeSelf)
            {
                _timeTextUntilAddLinesRun.SetActive(true);
            }
            _elapsedTimeFromAddLinesCall += Time.deltaTime;
            float remainingTime = LinesAdder.DURATION_FROM_ADDLINES_CALL - _elapsedTimeFromAddLinesCall;
            _timeTextUntilAddLinesRun.GetComponent<Text>().text = $"Until AddLines: {remainingTime.ToString("f2")}";
            if (_elapsedTimeFromAddLinesCall > LinesAdder.DURATION_FROM_ADDLINES_CALL)
            {
                _elapsedTimeFromAddLinesCall = 0;
                _goAddLines = false;
                _addLines.AddLines(ref _board, _addedLinesLength);
                _timeTextUntilAddLinesRun.SetActive(false);

            }
            
        }

        

        

        if (GoMakeBoardUnClickable)
        {
            GoMakeBoardUnClickable = false;

            if (_initializeFinished)
            {
                _unclickableFromMissClick = true;
                _mineClickedNotificationPanel.SetActive(true);
            }
            
        }

        if (_unclickableFromMissClick)
        {
            _elapsedTimeSinceBoardBecameUnclickable += Time.deltaTime;
            if (_elapsedTimeSinceBoardBecameUnclickable > GameManager.BOARD_UNCLICKABLE_DURATION)
            {
                _elapsedTimeSinceBoardBecameUnclickable = 0;
                _unclickableFromMissClick = false;
                _mineClickedNotificationPanel.SetActive(false);
            }
        }
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

    public void OnAddLines()
    {
        _addLines.AddLines(ref _board, 3);

    }

    public void AddLines(int addedLinesLength)
    {
        if (addedLinesLength > 0)
        {
            _goAddLines = true;
        }
        _addedLinesLength = addedLinesLength;
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