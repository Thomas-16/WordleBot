using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TopGuessesUI : MonoBehaviour
{
    [SerializeField] private Transform guessesContainer;
    [SerializeField] private Transform expectedInfoContainer;

    [SerializeField] private TextMeshProUGUI guessTextPrefab;
    [SerializeField] private TextMeshProUGUI expectedInfoTextPrefab;

    public void ClearGuesses()
    {
        foreach (Transform child in guessesContainer)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in expectedInfoContainer)
        {
            Destroy(child.gameObject);
        }
    }

    public void DisplayGuesses(List<KeyValuePair<string, float>> guesses)
    {
        ClearGuesses();

        foreach (var guess in guesses)
        {
            TextMeshProUGUI guessText = Instantiate(guessTextPrefab, guessesContainer);
            guessText.text = guess.Key;

            TextMeshProUGUI expectedInfoText = Instantiate(expectedInfoTextPrefab, expectedInfoContainer);
            expectedInfoText.text = Math.Round(guess.Value, 3).ToString();
        }
    }
}
