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

    
    LinesAdder _addLines;

    [SerializeField]
    CpuController _cpu;

    public static readonly int AmountOfInitOpeningLines = 5;


    bool _startCountDown = false;
    float _elapsedTimeForStartCountdown = 0;

    [SerializeField]
    TextMeshProUGUI _readyGoText;

    [SerializeField]
    GameObject _readyGoPanel;

    bool _running = false;

    bool _unclickableFromMissClick = false;
    float _elapsedTimdeSinceUnclickable = 0;
    readonly float BOARD_UNCLICKABLE_DURATION = 3f;
    [SerializeField]
    GameObject _unclickablePanel;

    readonly float SEND_INFO_TO_OPPONENT_DURATION = 3f;
    float _elapsedTimeForSendingInfo = 0;

    public static readonly int MAX_ADDED_LINES_NUM = 5;

    readonly int ENEMY_LEVELS_NUM = 1;
   
    /// <summary>
    /// -1: default
    /// 0: usr won
    /// 1: cpu won
    /// </summary>
    public static int WinLose;

    int _totalInflictedDamageToCpu;

    [SerializeField]
    GameObject _lostPanel;

    [SerializeField]
    GameObject _wonPanel;

    void Start()
    {
        
        UpdateEnemyList();

    }

    void Update()
    {
        if (_startCountDown)
        {
            _readyGoPanel.SetActive(true);
            _elapsedTimeForStartCountdown += Time.deltaTime;
            if (_elapsedTimeForStartCountdown > 1f)
            {
                _readyGoText.text = "GO!";
            }
            
            if (_elapsedTimeForStartCountdown > 1.7f)
            {
                _elapsedTimeForStartCountdown = 0f;
                _readyGoPanel.SetActive(false);
                _startCountDown = false;
                _running = true;
                _cpu.StartRunning();
            }

        }

        if (_running)
        {
            _timer.Mesure();
            var cpuCond = _cpu.Process();


            if (cpuCond == 2 || WinLose == 1) // if cpu won
            {
                WinLose = -1;
                EndGame(false);
            }
            else if (cpuCond == 1 || WinLose == 0) // if cpu lost
            {
                WinLose = -1;
                EndGame(true);
            }

            if (!_unclickableFromMissClick)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    HandleButtonDown(0);
                }

                if (Input.GetMouseButtonDown(1))
                {
                    HandleButtonDown(1);
                }
            }
            else
            {
                _unclickablePanel.SetActive(true);
                _elapsedTimdeSinceUnclickable += Time.deltaTime;
                if (_elapsedTimdeSinceUnclickable > BOARD_UNCLICKABLE_DURATION)
                {
                    _unclickablePanel.SetActive(false);
                    _elapsedTimdeSinceUnclickable = 0;
                    _unclickableFromMissClick = false;
                }
            }

            _elapsedTimeForSendingInfo += Time.deltaTime;
            if (_elapsedTimeForSendingInfo > SEND_INFO_TO_OPPONENT_DURATION)
            {
                _elapsedTimeForSendingInfo = 0;
                

                var cpuinfo = _cpu.GetInfo();
                //Debug.Log($"cpuinfo: {cpuinfo}");
                UpdateUserBoard(cpuinfo);
                var myInfo = GetInfo();
                _cpu.UpdateWithOpponentState(myInfo / 10);
            }

            

        } // <-- _running
    }

    void HandleButtonDown(int mouseButton)
    {
        
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            var clickedCell = hit.collider.gameObject.GetComponent<Cell>();
            if (clickedCell != null && hit.collider.gameObject.tag == "UserCell")
            {
                if (mouseButton == 0)
                {
                    _unclickableFromMissClick = _myBoard.TryOpenCell(clickedCell.Y, clickedCell.X, true);
                }
                else if (mouseButton == 1)
                {
                    _myBoard.TryFlagCell(clickedCell.Y, clickedCell.X);
                }
            }

        }
        
    }

    void UpdateEnemyList()
    {
        if (_defeatedEnemies == null)
        {
            _defeatedEnemies = new bool[ENEMY_LEVELS_NUM];
        }

        // todo: webgl”Å‚Æ‚»‚êˆÈŠO‚Å•ªŠò
        //  apply _defatedEnemis to the UI.
        
    }

    public void OnCpuSelected(int lv)
    {
        WinLose = -1;
        _totalInflictedDamageToCpu = 0;
        _cpuLevel = lv;
        _opponentSelectingPanel.SetActive(false);
        _battleScenePanel.SetActive(true);
        _cpuLevelText.text = $"Level: {lv}";

        // initialize timer
        _timer.ResetTimer();


        // initialize my board
        MakeMyBoard();

        // initialize cpu, minimap
        MakeCpuAndMinimap(lv);

        // start (count down)
        StartCountDown();

    }
     
    void MakeMyBoard()
    {
        _myBoard = new Board(_cellImageAsset, _cellPrefab, Board.BoardType.UserBoardInVsCpuMode);

        var tmpCellsinfo = MakeCellsInfo(Board.BoardHeight, Board.BoardWidth, Board.AmountOfMinesAtFirst);

        _myBoard.Make(tmpCellsinfo);

        _addLines = new LinesAdder(Board.BoardWidth, Board.BoardHeight, Board.BoardType.UserBoardInVsCpuMode);

        for (int i = 0; i < AmountOfInitOpeningLines; i++)
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

    void MakeCpuAndMinimap(int level)
    {
        // CpuController.
        _cpu.Initialize(level, _cellImageAsset, _cellPrefab);


    }

    void StartCountDown()
    {
        _startCountDown = true;
    }

    public static int CalcDamage(ref int totalInflictedDamage, int totalErasedCellAmount)
    {
        var damage = ((totalErasedCellAmount - totalInflictedDamage) / 10) * 10;
        damage = Mathf.Min(damage, MAX_ADDED_LINES_NUM * 10);
        totalInflictedDamage += damage;
        return damage;
    }

    void UpdateUserBoard(int cpuInfo)
    {
        _addLines.AddLines(ref _myBoard, cpuInfo);
    }
    
    int GetInfo()
    {
        var damageToCpu = CalcDamage(ref _totalInflictedDamageToCpu, _myBoard.GetAmountOfOpenedCells());
        return damageToCpu;
    }

    void EndGame(bool userWon)
    {
        if (userWon)
        {
            Debug.Log("Usr WOn!!!");
            _wonPanel.SetActive(true);
            OnGameEnd();
        }
        else
        {
            Debug.Log("Cpu WOn!!!");
            _lostPanel.SetActive(true);
            OnGameEnd();
        }
    }

    void OnGameEnd()
    {
        _timer.Stop();
        _running = false;
    }

    public void BackToCPUSelectScene()
    {
        

        Destroy(GameObject.Find(Board.CELLS_PARENT_GAMEOBJECT_NAME_FOR_USR));
        Destroy(GameObject.Find(Board.CELLS_PARENT_GAMEOBJECT_NAME_FOR_CPU));

        _wonPanel.SetActive(false);
        _lostPanel.SetActive(false);

        _opponentSelectingPanel.SetActive(true);
        _battleScenePanel.SetActive(false);
    }
}
