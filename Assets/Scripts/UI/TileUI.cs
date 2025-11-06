using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TileUI : MonoBehaviour
{
    private TextMeshProUGUI letterText;
    private Image background;

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
        switch (result)
        {
            case LetterResult.Absent:
                background.color = new Color32(122, 124, 126, 255);
                break;
            case LetterResult.Present:
                background.color = new Color32(199, 181, 103, 255);
                break;
            case LetterResult.Correct:
                background.color = new Color32(119, 177, 95, 255);
                break;
        }
    }

    public void Reset()
    {
        letterText.text = " ";
        background.color = Color.black;
    }

    public bool IsEmpty()
    {
        return string.IsNullOrEmpty(letterText.text.ToString().Trim());
    }
}
