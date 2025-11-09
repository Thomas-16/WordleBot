using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TileUI : MonoBehaviour
{
    private TextMeshProUGUI letterText;
    private Image background;

    public TileState State { get; private set; } = TileState.Empty;
    public LetterResult LetterResult { get; private set; }

    void Awake()
    {
        letterText = GetComponentInChildren<TextMeshProUGUI>();
        background = GetComponent<Image>();
    }

    public void SetLetter(char letter)
    {
        letterText.text = letter.ToString().ToUpper();
    }

    public void SetLetterResult(LetterResult result)
    {
        this.LetterResult = result;
        switch (result)
        {
            case LetterResult.Absent:
                background.color = new Color32(122, 124, 126, 255);
                this.State = TileState.Grey;
                break;
            case LetterResult.Present:
                background.color = new Color32(199, 181, 103, 255);
                this.State = TileState.Yellow;
                break;
            case LetterResult.Correct:
                background.color = new Color32(119, 177, 95, 255);
                this.State = TileState.Green;
                break;
        }
    }

    public void SetTileState(TileState newState)
    {
        this.State = newState;
        switch (newState)
        {
            case TileState.Empty:
                background.color = new Color32(0, 0, 0, 255);
                this.LetterResult = LetterResult.Absent;
                break;
            case TileState.Grey:
                background.color = new Color32(122, 124, 126, 255);
                this.LetterResult = LetterResult.Absent;
                break;
            case TileState.Yellow:
                background.color = new Color32(199, 181, 103, 255);
                this.LetterResult = LetterResult.Present;
                break;
            case TileState.Green:
                background.color = new Color32(119, 177, 95, 255);
                this.LetterResult = LetterResult.Correct;
                break;
        }
    }

    public void Reset()
    {
        letterText.text = " ";
        background.color = Color.black;
        LetterResult = LetterResult.Absent;
        State = TileState.Empty;
    }

    public bool IsEmpty()
    {
        return string.IsNullOrEmpty(letterText.text.ToString().Trim());
    }



    public enum TileState
    {
        Empty = 0,
        Grey = 1,
        Yellow = 2,
        Green = 3
    }
}
