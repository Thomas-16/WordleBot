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
}
