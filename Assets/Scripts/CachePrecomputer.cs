using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Utility script to precompute and save the pattern cache
/// Attach to a GameObject and call PrecomputeCache() to generate the cache file
/// This only needs to be run once (or when word lists change)
/// </summary>
public class CachePrecomputer : MonoBehaviour
{
    [Header("Precomputation")]
    [Tooltip("Click this button in the Inspector to start precomputation")]
    [SerializeField] private bool precomputeNow = false;

    private void Update()
    {
        // Check if button was pressed in Inspector
        if (precomputeNow)
        {
            precomputeNow = false;
            PrecomputeCache();
        }
    }

    /// <summary>
    /// Precomputes all patterns and saves to file
    /// 13,000 x 13,000 = 169 million patterns (~169MB file)
    /// Takes 1-3 minutes depending on CPU
    /// </summary>
    [ContextMenu("Precompute Cache")]
    public void PrecomputeCache()
    {
        Debug.Log("Starting pattern cache precomputation (13k x 13k)...");
        Debug.Log("This will take 1-3 minutes. Do not close Unity!");

        PatternCache cache = new PatternCache();

        // Get word lists from WordList singleton
        if (WordList.Instance == null)
        {
            Debug.LogError("WordList not initialized! Make sure it's in the scene.");
            return;
        }

        var allValidWords = WordList.Instance.GetAllValidWords();

        if (allValidWords.Count == 0)
        {
            Debug.LogError("Word list is empty! Cannot precompute.");
            return;
        }

        // Precompute 13k x 13k matrix (both guesses AND answers from all valid words)
        Debug.Log($"Computing {allValidWords.Count} x {allValidWords.Count} = {allValidWords.Count * allValidWords.Count:N0} patterns");
        cache.PrecomputeAndSave(allValidWords, allValidWords);

        Debug.Log("Precomputation complete! Cache file saved to StreamingAssets folder.");
        Debug.Log("You can now run the game and it will use the cached patterns.");
    }

    /// <summary>
    /// Precomputes top 20 initial guesses and saves to file
    /// Requires pattern cache to be generated first
    /// </summary>
    [ContextMenu("Precompute Initial Guesses")]
    public void PrecomputeInitialGuesses()
    {
        Debug.Log("Computing initial guesses cache...");

        if (WordList.Instance == null)
        {
            Debug.LogError("WordList not initialized! Make sure it's in the scene.");
            return;
        }

        // Load pattern cache first
        PatternCache patternCache = new PatternCache();
        if (!patternCache.LoadFromFile())
        {
            Debug.LogError("Pattern cache required! Run pattern precomputation first.");
            return;
        }

        // Create frequency model
        var sortedWords = WordList.Instance.GetAllValidWordsSorted();
        var frequencyModel = new WordFrequencyModel(sortedWords);

        // Create bot and calculate
        var allWords = WordList.Instance.GetAllValidWords();
        var bot = new WordleBot(allWords, patternCache, frequencyModel);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        bot.GetBestGuess();
        Debug.Log($"Calculated in {sw.ElapsedMilliseconds}ms");

        // Get top 20 and save
        var entropies = bot.GetWordEntropies();
        var top20 = entropies.OrderByDescending(x => x.Value).Take(20).ToList();

        var cache = new InitialGuessesCache();
        cache.SaveToFile(top20);

        Debug.Log("Initial guesses cache complete!");
    }
}
