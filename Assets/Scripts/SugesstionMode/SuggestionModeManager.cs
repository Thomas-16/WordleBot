using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SuggestionModeManager : MonoBehaviour
{
    [SerializeField] private TopGuessesUI textGuessesUI;
    [SerializeField] private GridUI gridUI;
    [SerializeField] private GameObject warningText;


    private WordleBot wordleBot;
    private PatternCache patternCache;
    private WordFrequencyModel frequencyModel;


    void Start()
    {
        // Initialize pattern cache (loads from file if available)
        patternCache = new PatternCache();
        bool cacheLoaded = patternCache.LoadFromFile();

        if (!cacheLoaded)
        {
            Debug.LogWarning("Pattern cache not found. Bot will run slower without precomputed patterns.");
            Debug.LogWarning("Run the CachePrecomputer script to generate the cache file.");
        }

        // Initialize word frequency model with sorted word list
        List<string> sortedWords = WordList.Instance.GetAllValidWordsSorted();
        frequencyModel = new WordFrequencyModel(sortedWords);
        Debug.Log("Word frequency model initialized");
        Debug.Log(frequencyModel.GetDiagnostics());

        // Create bot with cache and frequency model - use all 13k valid words
        wordleBot = new WordleBot(WordList.Instance.GetAllValidWords(), patternCache, frequencyModel);

        GetAndDisplayBestGuesses();

        warningText.SetActive(false);
    }

    private void GetAndDisplayBestGuesses()
    {
        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
        Debug.Log($"Best guess: {wordleBot.GetBestGuess()}, time took: {sw.ElapsedMilliseconds} ms");
        sw.Stop();

        Dictionary<string, float> wordEntropies = wordleBot.GetWordEntropies();
        Debug.Log($"Possibilities: {wordEntropies.Count}");
        List<KeyValuePair<string, float>> topGuesses = wordEntropies.OrderByDescending(x => x.Value).Take(14).ToList();
        textGuessesUI.DisplayGuesses(topGuesses);
    }

    private void HandleLetterKeyPressed(char letter)
    {
        if (gridUI == null || gridUI.GridFilled || gridUI.HasWon || gridUI.GetCurrentRowString().Length == 5) return;

        gridUI.UpdateCurrentRow(gridUI.GetCurrentRowString() + letter);
    }

    private void HandleEnterPressed()
    {
        if (gridUI == null || gridUI.GridFilled || gridUI.HasWon) return;

        string guess = gridUI.GetCurrentRowString();
        if (guess.Length < 5 || !WordList.Instance.IsValidWord(guess)) return;

        TileUI[] currentRowTiles = gridUI.GetCurrentRowTile();

        bool isRowFull = true;
        foreach (TileUI tile in currentRowTiles)
        {
            if (tile.State == TileUI.TileState.Empty) isRowFull = false;
        }
        if (!isRowFull)
        {
            StartCoroutine(ShowWarningTextTemporarily());
            return;
        }

        GuessResult guessResult = new GuessResult(guess, currentRowTiles[0].LetterResult, currentRowTiles[1].LetterResult, currentRowTiles[2].LetterResult, currentRowTiles[3].LetterResult, currentRowTiles[4].LetterResult);

        gridUI.ConfirmGuess(guessResult);

        // Calculate actual information gained from this guess
        int possibilitiesBefore = wordleBot.GetRemainingPossibilitiesCount();
        wordleBot.ProcessFeedback(guess, guessResult);
        int possibilitiesAfter = wordleBot.GetRemainingPossibilitiesCount();

        // Information gained = log2(before / after) = log2(before) - log2(after)
        float actualInformation = Mathf.Log(possibilitiesBefore, 2) - Mathf.Log(possibilitiesAfter, 2);
        gridUI.AddGuessInfoDisplay(actualInformation);

        GetAndDisplayBestGuesses();
    }
    
    private void HandleDeletePressed()
    {
        if (gridUI == null || gridUI.GridFilled || gridUI.HasWon || gridUI.GetCurrentRowString().Length == 0) return;

        gridUI.DeleteLetter();

    }

    public void OnResetButtonClicked()
    {
        gridUI.ResetEntireGrid();
        gridUI.ClearGuessInfoContainer();

        wordleBot = new WordleBot(WordList.Instance.GetAllValidWords(), patternCache, frequencyModel);
        GetAndDisplayBestGuesses();
    }

    IEnumerator ShowWarningTextTemporarily()
    {
        warningText.SetActive(true);
        yield return new WaitForSeconds(2f);
        warningText.SetActive(false);
    }


    void OnEnable()
    {
        InputManager.Instance.OnLetterPressed += HandleLetterKeyPressed;
        InputManager.Instance.OnEnterPressed += HandleEnterPressed;
        InputManager.Instance.OnDeletePressed += HandleDeletePressed;
    }
    void OnDisable()
    {
        InputManager.Instance.OnLetterPressed -= HandleLetterKeyPressed;
        InputManager.Instance.OnEnterPressed -= HandleEnterPressed;
        InputManager.Instance.OnDeletePressed -= HandleDeletePressed;
    }
}
