using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class VsCpuManager : MonoBehaviour
{
    // for storing battle record data.
    bool[] _defeatedEnemies;

    [SerializeField]
    GameObject _opponentSelectingPanel;

    [SerializeField]
    GameObject _battleScenePanel;

    int _cpuLevel;
    [SerializeField]
    TextMeshProUGUI _cpuLevelText;

    

    [SerializeField]
    VsCpuTimer _timer;

    Board _myBoard;

    [SerializeField]
    CellImageAsset _cellImageAsset;

    [SerializeField]
    GameObject _cellPrefab;

    [SerializeField]
    UIManager _uiManager;

    [SerializeField]
    LinesAdder _addLines;

    readonly int _amountOfInitOpeningLines = 5;

    void Start()
    {
        
        UpdateEnemyList();
        




    }

    void Update()
    {
        
    }

    void UpdateEnemyList()
    {
        if (_defeatedEnemies == null)
        {
            _defeatedEnemies = new bool[0];
        }

        // todo: webglî≈Ç∆ÇªÇÍà»äOÇ≈ï™äÚ
        //  apply _defatedEnemis to the UI.

    }

    public void OnCpuSelected(int lv)
    {
        _cpuLevel = lv;
        _opponentSelectingPanel.SetActive(false);
        _battleScenePanel.SetActive(true);
        _cpuLevelText.text = $"Level: {lv}";

        // initialize timer
        _timer.TimerValue = 0f;


        // initialize my board
        MakeMyBoard();

        // initialize cpu, minimap
        MakeCpuAndMinimap();

        // start (count down)


    }
     
    void MakeMyBoard()
    {
        _myBoard = new Board(_cellImageAsset, _cellPrefab, _uiManager, false);

        var tmpCellsinfo = MakeCellsInfo(Board.BoardHeight, Board.BoardWidth, Board.AmountOfMinesAtFirst);

        _myBoard.Make(tmpCellsinfo);


        _uiManager.InitializeUI(Board.AmountOfMinesAtFirst);


        _addLines = new LinesAdder(Board.BoardWidth, Board.BoardHeight);


        // TODO: è„êîçsÇÃÉZÉãÇãÛÇØÇÈ
        for (int i = 0; i < _amountOfInitOpeningLines; i++)
        {
            for (int j = 0; j < Board.BoardWidth; j++)
            {

                _myBoard.TryOpenCell(Board.BoardHeight - i - 1, j, false);
            }
        }

        _myBoard.CountAndSetAmountOfAlreadyOpenedCellsAndMines();
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

    void MakeCpuAndMinimap()
    {

    }
}
