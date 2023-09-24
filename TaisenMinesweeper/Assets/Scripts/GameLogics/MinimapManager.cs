using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinimapManager : MonoBehaviour
{
    // -1: èâä˙íl
    // 0-8: ãÛÇ¢ÇƒÇÈèÛë‘ÇÃÇªÇÃíl
    // 9: ïÅí ÇÃflag
    // 10: check


    [SerializeField]
    CellImageAsset _cellImageAsset;

    [SerializeField]
    GameObject _cellPrefab;

    [SerializeField]
    GameObject _minimapPanel;

    [HideInInspector]
    public bool GoCreateMinimap = false;

    [HideInInspector]
    public bool GoUpdateMinimap = false;

    const float _cellSize = 2.4f;
    const float _cellOffset = 10f;

    void Update()
    {
        if (GoCreateMinimap)
        {
            CreateMinimap(Board.BoardWidth, Board.BoardHeight);
            GoCreateMinimap = false;
        }

        if (GoUpdateMinimap)
        {
            //UpdateMinimap()
            GoUpdateMinimap = false;
        }
    }
    void CreateMinimap(int boardWidth, int boardHeight)
    {

        for (int i = 0; i < boardHeight; i++)
        {
            for (int j = 0; j < boardWidth; j++)
            {
                GameObject cell = Instantiate(_cellPrefab) as GameObject;
                cell.transform.SetParent(_minimapPanel.transform);
                cell.GetComponent<RectTransform>().localPosition = new Vector3(_cellOffset + j * 5 * _cellSize - 200, _cellOffset + i * 5 * _cellSize - 200, 0);
                cell.GetComponent<RectTransform>().localScale = new Vector3(_cellSize, _cellSize, 1);

            }
        }

    }

    public void UpdateMinimap(int[] board) 
    {


    }
}
