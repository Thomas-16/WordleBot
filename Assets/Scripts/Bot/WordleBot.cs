using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class WordleBot
{
    private List<string> remainingPossibleWords;
    private Dictionary<string, float> wordEntropies;
    private PatternCache patternCache;


    public WordleBot(List<string> possibleWords, PatternCache cache = null)
    {
        this.remainingPossibleWords = possibleWords;
        this.wordEntropies = new Dictionary<string, float>();
        this.patternCache = cache;
    }

    public string GetBestGuess()
    {
        // Use concurrent dictionary for thread-safe parallel computation
        ConcurrentDictionary<string, float> concurrentEntropies = new ConcurrentDictionary<string, float>();

        // Calculate entropies in parallel
        Parallel.ForEach(remainingPossibleWords, guess =>
        {
            float entropy = CalculateExpectedEntropy(guess);
            concurrentEntropies[guess] = entropy;
        });

        // Transfer results to instance dictionary
        wordEntropies = new Dictionary<string, float>(concurrentEntropies);

        // Find best guess from results
        string bestGuess = "";
        float maxEntropy = 0;

        foreach (var kvp in wordEntropies)
        {
            if (kvp.Value > maxEntropy)
            {
                maxEntropy = kvp.Value;
                bestGuess = kvp.Key;
            }
        }

        return bestGuess;
    }

    public float CalculateExpectedEntropy(string guess)
    {
        // Use direct entropy calculation from cache (avoids building distribution dictionary)
        if (patternCache != null && patternCache.IsInitialized())
        {
            return patternCache.CalculateEntropy(guess, remainingPossibleWords);
        }

        // Fallback: build distribution and calculate manually
        Dictionary<int, List<string>> patterns = PatternMatcher.GetPatternDistribution(guess, remainingPossibleWords);

        float sum = 0;
        foreach (var pattern in patterns)
        {
            int patternId = pattern.Key;
            List<string> possibleAnswers = pattern.Value;

            float p = (float)possibleAnswers.Count / remainingPossibleWords.Count;

            sum += p * Mathf.Log(1 / p, 2);
        }

        return sum;
    }

    public void ProcessFeedback(string guess, GuessResult result)
    {
        // Filter remaining words based on pattern
        remainingPossibleWords = PatternMatcher.FilterByPattern(
            remainingPossibleWords, guess, result
        );

        // Clear entropy cache since possibilities changed
        wordEntropies.Clear();
    }

    public Dictionary<string, float> GetWordEntropies() { return wordEntropies; }

    public int GetRemainingPossibilitiesCount() { return remainingPossibleWords.Count; }
}
