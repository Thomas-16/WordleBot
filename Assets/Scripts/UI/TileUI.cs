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
}
