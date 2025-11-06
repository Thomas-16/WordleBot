using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private TopGuessesUI textGuessesUI;
    [SerializeField] private GridUI gridUI;

    [SerializeField] private string answer;

    private WordleBot wordleBot;
    private PatternCache patternCache;

    void Start()
    {
        answer = WordList.Instance.GetRandomAnswer();

        // Initialize pattern cache (loads from file if available)
        patternCache = new PatternCache();
        bool cacheLoaded = patternCache.LoadFromFile();

        if (!cacheLoaded)
        {
            Debug.LogWarning("Pattern cache not found. Bot will run slower without precomputed patterns.");
            Debug.LogWarning("Run the CachePrecomputer script to generate the cache file.");
        }

        // Create bot with cache - use all 13k valid words
        wordleBot = new WordleBot(WordList.Instance.GetAllValidWords(), patternCache);

        GetAndDisplayBestGuesses();
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

        GuessResult guessResult = PatternMatcher.EvaluateGuess(guess, this.answer);
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

        answer = WordList.Instance.GetRandomAnswer();

        wordleBot = new WordleBot(WordList.Instance.GetAllValidWords(), patternCache);
        GetAndDisplayBestGuesses();
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
