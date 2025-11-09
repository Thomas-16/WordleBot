using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GridUI : MonoBehaviour
{
    [SerializeField] private Transform[] rowTransforms;
    [SerializeField] private Transform guessesInfoContainer;
    [SerializeField] private TextMeshProUGUI guessActualInfoPrefab;
    [SerializeField] private GraphicRaycaster raycaster;


    [SerializeField] private bool allowClickSwitching;

    private TileUI[,] tiles;
    private int currentRow = 0;
    private string currentRowString;
    public bool GridFilled { get; private set; }
    public bool HasWon { get; private set; }

    void Awake()
    {
        ResetEntireGrid();
    }

    void Update()
    {
        if (!allowClickSwitching) return;
        if (!Input.GetMouseButtonDown(0)) return;

        if (!EventSystem.current || !EventSystem.current.IsPointerOverGameObject()) return;

        TileUI targetTile = RaycastTileUnderMouse();
        if (!targetTile) return;

        int tileRow = -1;

        for (int r = 0; r < tiles.GetLength(0); r++)
        {
            for (int c = 0; c < tiles.GetLength(1); c++)
            {
                if (tiles[r, c] == targetTile)
                {
                    tileRow = r;
                    break;
                }
            }
            if (tileRow != -1) break;
        }

        if (tileRow != currentRow) return;

        switch (targetTile.State)
        {
            case TileUI.TileState.Empty:
                targetTile.SetTileState(TileUI.TileState.Grey); break;
            case TileUI.TileState.Grey:
                targetTile.SetTileState(TileUI.TileState.Yellow); break;
            case TileUI.TileState.Yellow:
                targetTile.SetTileState(TileUI.TileState.Green); break;
            case TileUI.TileState.Green:
                targetTile.SetTileState(TileUI.TileState.Empty); break;
        }
    }

    TileUI RaycastTileUnderMouse()
    {
        var ped = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        var results = new List<RaycastResult>();
        raycaster.Raycast(ped, results);

        foreach (var r in results)
        {
            // Your tiles might be on a child; search up the hierarchy.
            var tile = r.gameObject.GetComponentInParent<TileUI>();
            if (tile) return tile;
        }
        return null;
    }

    public void UpdateCurrentRow(string input)
    {
        if (GridFilled || HasWon) return;

        input = input.Trim();
        if (input.Length > 5) Debug.LogWarning("[GridUI] entered a word longer than 5 letters");

        currentRowString = input;

        // Update grid ui
        for (int i = 0; i < input.Length; i++)
        {
            tiles[currentRow, i].SetLetter(input[i]);
        }
    }
    
    public void ConfirmGuess(GuessResult guessResult)
    {
        if (GridFilled || HasWon) return;

        if (PatternMatcher.IsWinningPattern(guessResult)) HasWon = true;

        LetterResult[] results = guessResult.results;
        for (int i = 0; i < 5; i++)
        {
            LetterResult result = results[i];
            tiles[currentRow, i].SetLetterResult(result);
        }

        currentRow++;
        currentRowString = "";

        if (currentRow == 6) GridFilled = true;
    }

    public void DeleteLetter()
    {
        if (GridFilled || HasWon) return;

        int i = 4;
        while (i >= 0 && tiles[currentRow, i].IsEmpty())
        {
            i--;
        }
        if (i == -1) return;

        tiles[currentRow, i].SetLetter(' ');

        currentRowString = currentRowString.Remove(currentRowString.Length - 1);
    }
    public void ClearRow()
    {
        if (GridFilled) return;

        for (int i = 0; i < 5; i++)
        {
            tiles[currentRow, i].SetLetter(' ');
        }

        currentRowString = "";
    }

    public void ResetEntireGrid()
    {
        tiles = new TileUI[6, 5];

        for (int r = 0; r < 6; r++)
        {
            for (int c = 0; c < 5; c++)
            {
                TileUI tile = rowTransforms[r].GetChild(c).GetComponent<TileUI>();
                tile.Reset();
                tiles[r, c] = tile;
            }
        }

        currentRowString = "";
        currentRow = 0;
        GridFilled = false;
        HasWon = false;
    }

    public void AddGuessInfoDisplay(float info)
    {
        TextMeshProUGUI infoText = Instantiate(guessActualInfoPrefab, guessesInfoContainer);
        infoText.text = $"{Math.Round(info, 2)} bits";
    }
    public void ClearGuessInfoContainer()
    {
        foreach (Transform child in guessesInfoContainer)
        {
            Destroy(child.gameObject);
        }
    }

    public string GetCurrentRowString() => currentRowString;
    public int GetCurrentRowIndex() => currentRow;

    public TileUI[] GetCurrentRowTile()
    {
        return Enumerable.Range(0, tiles.GetLength(1))
                              .Select(c => tiles[currentRow, c])
                              .ToArray();
    }
}
