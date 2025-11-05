using UnityEngine;

public class GridUI : MonoBehaviour
{
    [SerializeField] private Transform[] rowTransforms;

    private TileUI[,] tiles;
    private int currentRow = 0;
    private string currentRowString;

    void Awake()
    {
        tiles = new TileUI[6, 5];

        for (int r = 0; r < 6; r++)
        {
            for (int c = 0; c < 5; c++)
            {
                TileUI tile = rowTransforms[r].GetChild(c).GetComponent<TileUI>();
                tile.SetLetter(' ');
                tiles[r, c] = tile;
            }
        }

        currentRowString = "";
    }

    public void UpdateCurrentRow(string input)
    {
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
        LetterResult[] results = guessResult.results;
        for (int i = 0; i < 5; i++)
        {
            LetterResult result = results[i];
            tiles[currentRow, i].SetLetterResult(result);
        }

        currentRow++;
    }

    public string GetCurrentRowString() => currentRowString;
}
