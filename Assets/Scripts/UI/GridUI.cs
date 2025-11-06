using System;
using TMPro;
using UnityEngine;

public class GridUI : MonoBehaviour
{
    [SerializeField] private Transform[] rowTransforms;
    [SerializeField] private Transform guessesInfoContainer;
    [SerializeField] private TextMeshProUGUI guessActualInfoPrefab;

    private TileUI[,] tiles;
    private int currentRow = 0;
    private string currentRowString;
    public bool GridFilled { get; private set; }
    public bool HasWon { get; private set; }

    void Awake()
    {
        ResetEntireGrid();
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
}
