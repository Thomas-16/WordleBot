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
    /// Takes 10-30 seconds depending on CPU
    /// </summary>
    [ContextMenu("Precompute Cache")]
    public void PrecomputeCache()
    {
        Debug.Log("Starting pattern cache precomputation...");
        Debug.Log("This will take 10-30 seconds. Do not close Unity!");

        PatternCache cache = new PatternCache();

        // Get word lists from WordList singleton
        if (WordList.Instance == null)
        {
            Debug.LogError("WordList not initialized! Make sure it's in the scene.");
            return;
        }

        var allGuesses = WordList.Instance.GetAllValidWords();
        var allAnswers = WordList.Instance.GetPossibleAnswers();

        if (allGuesses.Count == 0 || allAnswers.Count == 0)
        {
            Debug.LogError("Word lists are empty! Cannot precompute.");
            return;
        }

        // Precompute and save
        cache.PrecomputeAndSave(allGuesses, allAnswers);

        Debug.Log("Precomputation complete! Cache file saved to StreamingAssets folder.");
        Debug.Log("You can now run the game and it will use the cached patterns.");
    }
}
