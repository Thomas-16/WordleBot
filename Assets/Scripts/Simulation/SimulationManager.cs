using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runs the WordleBot through all possible Wordle answers to gather statistics
/// Uses fixed first guess
/// </summary>
public class SimulationManager : MonoBehaviour
{
    public static SimulationManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private string fixedFirstGuess = "TARES";
    [SerializeField] private float delayBetweenGames = 0f; // Delay in seconds between each game simulation

    [Header("Runtime Info")]
    [SerializeField] private bool isRunning = false;
    [SerializeField] private int currentGameIndex = 0;
    [SerializeField] private int totalGames = 0;

    private PatternCache patternCache;
    private List<string> allPossibleAnswers;

    // Statistics tracking
    private int gamesCompleted = 0;
    private int totalGuesses = 0;
    private int wins = 0;
    private int losses = 0;
    private int[] solveDistribution = new int[7]; // Index 0 = failed, 1-6 = solved in N guesses

    // Current game data
    private string currentAnswer;
    private WordleBot currentBot;
    private List<string> currentGuesses;
    private int currentGuessCount;

    public Action OnSimulationComplete;

    void Awake()
    {
        Instance = this;
    }

    public void StartSimulation()
    {
        if (isRunning)
        {
            Debug.LogWarning("Simulation already running!");
            return;
        }

        // Initialize pattern cache
        patternCache = new PatternCache();
        bool cacheLoaded = patternCache.LoadFromFile();

        if (!cacheLoaded)
        {
            Debug.LogError("Pattern cache not loaded! Cannot run simulation without cache.");
            return;
        }

        // Get all possible answers (the 2300 curated list)
        allPossibleAnswers = WordList.Instance.GetPossibleAnswers();
        totalGames = allPossibleAnswers.Count;

        Debug.Log($"Starting simulation with {totalGames} possible answers");
        Debug.Log($"Fixed first guess: {fixedFirstGuess}");

        // Reset statistics
        ResetStatistics();

        // Start simulation coroutine
        StartCoroutine(RunSimulation());
    }

    private IEnumerator RunSimulation()
    {
        isRunning = true;
        System.Diagnostics.Stopwatch totalTimer = System.Diagnostics.Stopwatch.StartNew();

        for (currentGameIndex = 0; currentGameIndex < totalGames; currentGameIndex++)
        {
            currentAnswer = allPossibleAnswers[currentGameIndex];

            // Run one game
            SimulateGame(currentAnswer);

            // Optional delay between games (useful for UI updates)
            if (delayBetweenGames > 0)
            {
                yield return new WaitForSeconds(delayBetweenGames);
            }
            else
            {
                // Yield every 10 games to keep UI responsive
                if (currentGameIndex % 10 == 0)
                {
                    yield return null;
                }
            }
        }

        totalTimer.Stop();
        isRunning = false;

        OnSimulationComplete?.Invoke();
    }

    // Simulate one game of Wordle
    private void SimulateGame(string answer)
    {
        answer = answer.ToUpper();

        // Create new bot for this game
        currentBot = new WordleBot(WordList.Instance.GetAllValidWords(), patternCache);
        currentGuesses = new List<string>();
        currentGuessCount = 0;

        // Debug logging for first game
        if (currentGameIndex == 0)
        {
            Debug.Log($"Initial word count: {currentBot.GetRemainingPossibilitiesCount()}");
        }

        bool solved = false;
        int maxGuesses = 6;

        for (int i = 0; i < maxGuesses; i++)
        {
            string guess;

            // First guess
            if (i == 0)
            {
                guess = fixedFirstGuess.ToUpper();
            }
            else
            {
                // Get best guess from bot
                guess = currentBot.GetBestGuess();
            }

            // Validate guess is 5 letters
            if (string.IsNullOrEmpty(guess) || guess.Length != 5)
            {
                Debug.LogError($"Invalid guess: '{guess}' (length: {guess?.Length ?? 0})");
                break;
            }

            currentGuesses.Add(guess);
            currentGuessCount++;

            // Evaluate guess against answer
            GuessResult result = PatternMatcher.EvaluateGuess(guess, answer);

            // Check if solved
            if (PatternMatcher.IsWinningPattern(result))
            {
                solved = true;
                break;
            }

            // Process feedback for next iteration
            currentBot.ProcessFeedback(guess, result);
            int remainingPossibilities = currentBot.GetRemainingPossibilitiesCount();

            // Check if we have no remaining possibilities (shouldn't happen)
            if (remainingPossibilities == 0)
            {
                break;
            }
        }

        Debug.Log($"Answer: {answer}, Solved: {solved}, Guesses count: {currentGuessCount}");

        // Update statistics
        UpdateStatistics(solved, currentGuessCount);
    }

    private void UpdateStatistics(bool solved, int guessCount)
    {
        gamesCompleted++;
        totalGuesses += guessCount;

        if (solved)
        {
            wins++;
            solveDistribution[guessCount]++; // Index 1-6 for solved in N guesses
        }
        else
        {
            losses++;
            solveDistribution[0]++; // Index 0 for failed
        }
    }

    private void ResetStatistics()
    {
        currentGameIndex = 0;
        gamesCompleted = 0;
        totalGuesses = 0;
        wins = 0;
        losses = 0;
        solveDistribution = new int[7];
        currentGuesses = new List<string>();
        currentGuessCount = 0;
    }

    // Public getters for UI to access statistics in real-time
    public bool IsRunning() => isRunning;
    public int GetCurrentGameNum() => currentGameIndex + 1;
    public int GetTotalGames() => totalGames;
    public float GetProgress() => totalGames > 0 ? (float)currentGameIndex / totalGames : 0f;
    public int GetGamesCompleted() => gamesCompleted;
    public int GetWins() => wins;
    public int GetLosses() => losses;
    public float GetAverageGuesses() => gamesCompleted > 0 ? (float)totalGuesses / gamesCompleted : 0f;
    public int[] GetSolveDistribution() => solveDistribution;
    public string GetCurrentAnswer() => currentAnswer;
    public List<string> GetCurrentGuesses() => currentGuesses;
    public int GetCurrentGuessCount() => currentGuessCount;
}
