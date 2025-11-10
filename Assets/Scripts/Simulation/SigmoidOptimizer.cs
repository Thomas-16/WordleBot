using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// Optimizes sigmoid parameters (midpoint and steepness) using coarse-to-fine grid search.
/// Runs full simulations for each parameter combination to find optimal values.
/// </summary>
public class SigmoidOptimizer : MonoBehaviour
{
    public static SigmoidOptimizer Instance { get; private set; }

    [Header("Optimization Settings")]
    [SerializeField] private bool useCoarseSearch = true;
    [SerializeField] private string fixedFirstGuess = "TARES";

    [Header("Coarse Search Parameters")]
    [SerializeField] private float[] coarseMidpoints = { 0.2f, 0.4f, 0.6f, 0.8f };
    [SerializeField] private float[] coarseSteepness = { 5f, 10f, 15f, 20f };

    [Header("Fine Search Parameters (set after coarse search)")]
    [SerializeField] private float fineMidpointCenter = 0.5f;
    [SerializeField] private float fineMidpointRange = 0.1f;
    [SerializeField] private int fineMidpointSteps = 3;

    [SerializeField] private float fineSteepnessCenter = 10f;
    [SerializeField] private float fineSteepnessRange = 2f;
    [SerializeField] private int fineSteepnessSteps = 3;

    [Header("Runtime Info")]
    [SerializeField] private bool isRunning = false;
    [SerializeField] private int currentCombinationIndex = 0;
    [SerializeField] private int totalCombinations = 0;
    [SerializeField] private int totalGamesPerCombination = 0;

    private PatternCache patternCache;
    private List<string> allPossibleAnswers;

    // Current parameter combination being tested
    private float currentMidpoint;
    private float currentSteepness;

    // Results tracking
    private List<OptimizationResult> results = new List<OptimizationResult>();
    private OptimizationResult bestResult;

    // Current simulation state
    private int currentGameIndex = 0;
    private int gamesCompleted = 0;
    private int totalGuesses = 0;
    private int wins = 0;
    private int losses = 0;
    private int[] solveDistribution = new int[7];

    // Current game state
    private string currentAnswer;
    private WordleBot currentBot;
    private WordFrequencyModel currentFrequencyModel;

    public Action<OptimizationResult> OnCombinationComplete;
    public Action<OptimizationResult> OnOptimizationComplete;

    void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Starts the optimization process (coarse or fine search)
    /// </summary>
    public void StartOptimization()
    {
        if (isRunning)
        {
            Debug.LogWarning("Optimization already running!");
            return;
        }

        // Initialize pattern cache
        patternCache = new PatternCache();
        bool cacheLoaded = patternCache.LoadFromFile();

        if (!cacheLoaded)
        {
            Debug.LogError("Pattern cache not loaded! Cannot run optimization without cache.");
            return;
        }

        // Get all possible answers
        allPossibleAnswers = WordList.Instance.GetPossibleAnswers();
        totalGamesPerCombination = allPossibleAnswers.Count;

        // Calculate total combinations
        if (useCoarseSearch)
        {
            totalCombinations = coarseMidpoints.Length * coarseSteepness.Length;
            Debug.Log($"=== STARTING COARSE GRID SEARCH ===");
            Debug.Log($"Midpoints: {string.Join(", ", coarseMidpoints)}");
            Debug.Log($"Steepness: {string.Join(", ", coarseSteepness)}");
        }
        else
        {
            totalCombinations = fineMidpointSteps * fineSteepnessSteps;
            Debug.Log($"=== STARTING FINE GRID SEARCH ===");
            Debug.Log($"Midpoint: {fineMidpointCenter} ± {fineMidpointRange} ({fineMidpointSteps} steps)");
            Debug.Log($"Steepness: {fineSteepnessCenter} ± {fineSteepnessRange} ({fineSteepnessSteps} steps)");
        }

        Debug.Log($"Total combinations: {totalCombinations}");
        Debug.Log($"Games per combination: {totalGamesPerCombination}");
        Debug.Log($"Total games: {totalCombinations * totalGamesPerCombination}");
        Debug.Log($"Fixed first guess: {fixedFirstGuess}");

        // Reset results
        results.Clear();
        bestResult = null;

        // Start optimization coroutine
        StartCoroutine(RunOptimization());
    }

    private IEnumerator RunOptimization()
    {
        isRunning = true;
        System.Diagnostics.Stopwatch totalTimer = System.Diagnostics.Stopwatch.StartNew();

        // Get parameter combinations to test
        List<(float midpoint, float steepness)> combinations = GetParameterCombinations();

        currentCombinationIndex = 0;

        foreach (var combo in combinations)
        {
            currentMidpoint = combo.midpoint;
            currentSteepness = combo.steepness;
            currentCombinationIndex++;

            Debug.Log($"--- Testing Combination {currentCombinationIndex}/{totalCombinations} ---");
            Debug.Log($"Midpoint: {currentMidpoint}, Steepness: {currentSteepness}");

            // Run full simulation with these parameters
            System.Diagnostics.Stopwatch comboTimer = System.Diagnostics.Stopwatch.StartNew();
            yield return StartCoroutine(RunSimulationForParameters(currentMidpoint, currentSteepness));
            comboTimer.Stop();

            // Calculate results
            float avgGuesses = gamesCompleted > 0 ? (float)totalGuesses / gamesCompleted : 0f;
            float winRate = gamesCompleted > 0 ? (float)wins / gamesCompleted : 0f;

            OptimizationResult result = new OptimizationResult
            {
                midpoint = currentMidpoint,
                steepness = currentSteepness,
                averageGuesses = avgGuesses,
                winRate = winRate,
                gamesCompleted = gamesCompleted,
                totalGuesses = totalGuesses,
                wins = wins,
                losses = losses,
                solveDistribution = (int[])solveDistribution.Clone(),
                executionTimeMs = comboTimer.ElapsedMilliseconds
            };

            results.Add(result);

            Debug.Log($"Results: Avg = {avgGuesses:F3}, Win Rate = {winRate:P2}, Time = {comboTimer.ElapsedMilliseconds}ms");
            Debug.Log($"Distribution: {string.Join(", ", solveDistribution)}");

            // Track best result
            if (bestResult == null || avgGuesses < bestResult.averageGuesses)
            {
                bestResult = result;
                Debug.Log($"*** NEW BEST: Midpoint={currentMidpoint}, Steepness={currentSteepness}, Avg={avgGuesses:F3} ***");
            }

            OnCombinationComplete?.Invoke(result);

            // Small delay between combinations to keep UI responsive
            yield return null;
        }

        totalTimer.Stop();
        isRunning = false;

        string summary = BuildSummaryText(totalTimer.ElapsedMilliseconds);

        // Print to console
        Debug.Log(summary);

        // Write to file
        WriteResultsToFile(summary);

        OnOptimizationComplete?.Invoke(bestResult);
    }

    private IEnumerator RunSimulationForParameters(float midpoint, float steepness)
    {
        // Create frequency model with these parameters
        List<string> sortedWords = WordList.Instance.GetAllValidWordsSorted();
        currentFrequencyModel = new WordFrequencyModel(sortedWords, midpoint, steepness);

        // Reset statistics
        ResetStatistics();

        // Run all games
        for (currentGameIndex = 0; currentGameIndex < totalGamesPerCombination; currentGameIndex++)
        {
            currentAnswer = allPossibleAnswers[currentGameIndex];
            SimulateGame(currentAnswer);

            // Yield every 10 games to keep UI responsive
            if (currentGameIndex % 10 == 0)
            {
                yield return null;
            }
        }
    }

    private void SimulateGame(string answer)
    {
        answer = answer.ToUpper();

        // Create new bot for this game with current frequency model
        currentBot = new WordleBot(WordList.Instance.GetAllValidWords(), patternCache, currentFrequencyModel);

        bool solved = false;
        int guessCount = 0;
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

            // Validate guess
            if (string.IsNullOrEmpty(guess) || guess.Length != 5)
            {
                Debug.LogError($"Invalid guess: '{guess}' for answer '{answer}'");
                break;
            }

            guessCount++;

            // Evaluate guess
            GuessResult result = PatternMatcher.EvaluateGuess(guess, answer);

            // Check if solved
            if (PatternMatcher.IsWinningPattern(result))
            {
                solved = true;
                break;
            }

            // Process feedback
            currentBot.ProcessFeedback(guess, result);

            if (currentBot.GetRemainingPossibilitiesCount() == 0)
            {
                break;
            }
        }

        // Update statistics
        UpdateStatistics(solved, guessCount);
    }

    private List<(float midpoint, float steepness)> GetParameterCombinations()
    {
        List<(float, float)> combinations = new List<(float, float)>();

        if (useCoarseSearch)
        {
            // Coarse grid search
            foreach (float midpoint in coarseMidpoints)
            {
                foreach (float steepness in coarseSteepness)
                {
                    combinations.Add((midpoint, steepness));
                }
            }
        }
        else
        {
            // Fine grid search around center point
            float[] midpoints = GenerateRange(fineMidpointCenter, fineMidpointRange, fineMidpointSteps);
            float[] steepnesses = GenerateRange(fineSteepnessCenter, fineSteepnessRange, fineSteepnessSteps);

            foreach (float midpoint in midpoints)
            {
                foreach (float steepness in steepnesses)
                {
                    combinations.Add((midpoint, steepness));
                }
            }
        }

        return combinations;
    }

    private float[] GenerateRange(float center, float range, int steps)
    {
        if (steps == 1)
            return new float[] { center };

        float[] values = new float[steps];
        float start = center - range;
        float step = (range * 2) / (steps - 1);

        for (int i = 0; i < steps; i++)
        {
            values[i] = start + (step * i);
        }

        return values;
    }

    private void UpdateStatistics(bool solved, int guessCount)
    {
        gamesCompleted++;
        totalGuesses += guessCount;

        if (solved)
        {
            wins++;
            solveDistribution[guessCount]++;
        }
        else
        {
            losses++;
            solveDistribution[0]++;
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
    }

    private string BuildSummaryText(long totalTimeMs)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("\n========================================");
        sb.AppendLine("     OPTIMIZATION COMPLETE");
        sb.AppendLine("========================================");
        sb.AppendLine($"Search type: {(useCoarseSearch ? "COARSE" : "FINE")}");
        sb.AppendLine($"Total time: {totalTimeMs / 1000f:F1}s ({totalTimeMs / 60000f:F1} minutes)");
        sb.AppendLine($"Combinations tested: {results.Count}");
        sb.AppendLine($"Games per combination: {totalGamesPerCombination}");
        sb.AppendLine($"Total games: {results.Count * totalGamesPerCombination}");
        sb.AppendLine($"Fixed first guess: {fixedFirstGuess}");
        sb.AppendLine($"Timestamp: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("");
        sb.AppendLine("=== BEST RESULT ===");
        if (bestResult != null)
        {
            sb.AppendLine($"Midpoint: {bestResult.midpoint}");
            sb.AppendLine($"Steepness: {bestResult.steepness}");
            sb.AppendLine($"Average Guesses: {bestResult.averageGuesses:F4}");
            sb.AppendLine($"Win Rate: {bestResult.winRate:P2}");
            sb.AppendLine($"Wins: {bestResult.wins}");
            sb.AppendLine($"Losses: {bestResult.losses}");
            sb.AppendLine($"Distribution [Fail,1,2,3,4,5,6]: {string.Join(", ", bestResult.solveDistribution)}");
            sb.AppendLine($"Execution time: {bestResult.executionTimeMs}ms");
        }
        sb.AppendLine("");
        sb.AppendLine("=== TOP 5 RESULTS ===");
        var topResults = results.OrderBy(r => r.averageGuesses).Take(5).ToList();
        for (int i = 0; i < topResults.Count; i++)
        {
            var r = topResults[i];
            sb.AppendLine($"{i + 1}. M={r.midpoint:F2}, S={r.steepness:F1} -> Avg={r.averageGuesses:F4}, Win={r.winRate:P2}");
        }
        sb.AppendLine("");
        sb.AppendLine("=== ALL RESULTS (sorted by avg guesses) ===");
        sb.AppendLine("Midpoint, Steepness, AvgGuesses, WinRate, Wins, Losses, ExecutionTimeMs");
        var sortedResults = results.OrderBy(r => r.averageGuesses).ToList();
        foreach (var r in sortedResults)
        {
            sb.AppendLine($"{r.midpoint:F2}, {r.steepness:F2}, {r.averageGuesses:F4}, {r.winRate:P2}, {r.wins}, {r.losses}, {r.executionTimeMs}");
        }
        sb.AppendLine("========================================\n");

        return sb.ToString();
    }

    private void WriteResultsToFile(string summary)
    {
        try
        {
            // Create results directory if it doesn't exist
            string resultsDir = Path.Combine(Application.dataPath, "OptimizationResults");
            if (!Directory.Exists(resultsDir))
            {
                Directory.CreateDirectory(resultsDir);
            }

            // Generate filename with timestamp and search type
            string searchType = useCoarseSearch ? "coarse" : "fine";
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string filename = $"sigmoid_optimization_{searchType}_{timestamp}.txt";
            string filepath = Path.Combine(resultsDir, filename);

            // Write summary to file
            File.WriteAllText(filepath, summary);

            // Also write CSV format for easy analysis
            string csvFilename = $"sigmoid_optimization_{searchType}_{timestamp}.csv";
            string csvFilepath = Path.Combine(resultsDir, csvFilename);
            WriteResultsToCSV(csvFilepath);

            Debug.Log($"Results saved to:");
            Debug.Log($"  Text: {filepath}");
            Debug.Log($"  CSV:  {csvFilepath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to write results to file: {e.Message}");
        }
    }

    private void WriteResultsToCSV(string filepath)
    {
        StringBuilder csv = new StringBuilder();

        // Header
        csv.AppendLine("Midpoint,Steepness,AvgGuesses,WinRate,Wins,Losses,Fail,Solve1,Solve2,Solve3,Solve4,Solve5,Solve6,ExecutionTimeMs");

        // Data rows (sorted by avg guesses)
        var sortedResults = results.OrderBy(r => r.averageGuesses).ToList();
        foreach (var r in sortedResults)
        {
            csv.AppendLine($"{r.midpoint},{r.steepness},{r.averageGuesses:F4},{r.winRate:F4}," +
                          $"{r.wins},{r.losses}," +
                          $"{r.solveDistribution[0]},{r.solveDistribution[1]},{r.solveDistribution[2]}," +
                          $"{r.solveDistribution[3]},{r.solveDistribution[4]},{r.solveDistribution[5]}," +
                          $"{r.solveDistribution[6]},{r.executionTimeMs}");
        }

        File.WriteAllText(filepath, csv.ToString());
    }

    /// <summary>
    /// Switches to fine search mode with parameters centered around best coarse result
    /// </summary>
    public void SetupFineSearchFromBest()
    {
        if (bestResult == null)
        {
            Debug.LogError("No best result available. Run coarse search first!");
            return;
        }

        useCoarseSearch = false;
        fineMidpointCenter = bestResult.midpoint;
        fineSteepnessCenter = bestResult.steepness;

        Debug.Log($"Fine search configured around: Midpoint={fineMidpointCenter}, Steepness={fineSteepnessCenter}");
    }

    // Public getters
    public bool IsRunning() => isRunning;
    public int GetCurrentCombination() => currentCombinationIndex;
    public int GetTotalCombinations() => totalCombinations;
    public float GetProgress() => totalCombinations > 0 ? (float)currentCombinationIndex / totalCombinations : 0f;
    public List<OptimizationResult> GetAllResults() => new List<OptimizationResult>(results);
    public OptimizationResult GetBestResult() => bestResult;
    public string GetCurrentParameters() => $"M={currentMidpoint:F2}, S={currentSteepness:F1}";
}

/// <summary>
/// Stores results from testing one parameter combination
/// </summary>
[System.Serializable]
public class OptimizationResult
{
    public float midpoint;
    public float steepness;
    public float averageGuesses;
    public float winRate;
    public int gamesCompleted;
    public int totalGuesses;
    public int wins;
    public int losses;
    public int[] solveDistribution;
    public long executionTimeMs;

    public override string ToString()
    {
        return $"M={midpoint:F2}, S={steepness:F1} -> Avg={averageGuesses:F4}, Win={winRate:P2}";
    }
}
