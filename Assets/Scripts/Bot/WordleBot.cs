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
    private WordFrequencyModel frequencyModel;
    private Dictionary<string, float> currentProbabilities;
    private int initialWordCount;


    public WordleBot(List<string> possibleWords, PatternCache cache = null, WordFrequencyModel freqModel = null)
    {
        this.remainingPossibleWords = possibleWords;
        this.wordEntropies = new Dictionary<string, float>();
        this.patternCache = cache;
        this.frequencyModel = freqModel;
        this.initialWordCount = possibleWords.Count;

        // Initialize probabilities (normalized for current word set)
        if (frequencyModel != null)
        {
            currentProbabilities = frequencyModel.GetNormalizedProbabilities(possibleWords);
        }
        else
        {
            // Fallback to uniform probabilities
            currentProbabilities = new Dictionary<string, float>();
            float uniformProb = 1f / possibleWords.Count;
            foreach (string word in possibleWords)
            {
                currentProbabilities[word] = uniformProb;
            }
        }
    }

    public string GetBestGuess()
    {
#if UNITY_WEBGL
        // Sequential for WebGL (single-threaded)
        wordEntropies.Clear();
        foreach (string guess in remainingPossibleWords)
        {
            float entropy = CalculateExpectedEntropy(guess);
            wordEntropies[guess] = entropy;
        }
#else
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
#endif

        // Find best guess from results
        string bestGuess = "";
        float maxEntropy = -1;

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
        // Use direct entropy calculation from cache with probabilities
        if (patternCache != null && patternCache.IsInitialized())
        {
            return patternCache.CalculateEntropy(guess, remainingPossibleWords, currentProbabilities);
        }

        // Fallback: build distribution and calculate manually with probability weighting
        Dictionary<int, List<string>> patterns = PatternMatcher.GetPatternDistribution(guess, remainingPossibleWords);

        float sum = 0;
        foreach (var pattern in patterns)
        {
            int patternId = pattern.Key;
            List<string> possibleAnswers = pattern.Value;

            // Calculate probability of this pattern (sum of word probabilities in this bucket)
            float patternProbability = 0f;
            foreach (string answer in possibleAnswers)
            {
                if (currentProbabilities.TryGetValue(answer, out float prob))
                {
                    patternProbability += prob;
                }
            }

            // Skip patterns with zero probability
            if (patternProbability > 0f)
            {
                // Entropy contribution: p * log2(1/p)
                sum += patternProbability * Mathf.Log(1f / patternProbability, 2);
            }
        }

        return sum;
    }

    public void ProcessFeedback(string guess, GuessResult result)
    {
        // Filter remaining words based on pattern
        remainingPossibleWords = PatternMatcher.FilterByPattern(
            remainingPossibleWords, guess, result
        );

        // Renormalize probabilities for the new filtered set
        if (frequencyModel != null)
        {
            currentProbabilities = frequencyModel.GetNormalizedProbabilities(remainingPossibleWords);
        }
        else
        {
            // Update uniform probabilities
            currentProbabilities.Clear();
            float uniformProb = 1f / remainingPossibleWords.Count;
            foreach (string word in remainingPossibleWords)
            {
                currentProbabilities[word] = uniformProb;
            }
        }

        // Clear entropy cache since possibilities changed
        wordEntropies.Clear();
    }

    public Dictionary<string, float> GetWordEntropies() { return wordEntropies; }

    public int GetRemainingPossibilitiesCount() { return remainingPossibleWords.Count; }

    public bool IsInitialState() { return remainingPossibleWords.Count == initialWordCount; }

    public void SetWordEntropies(Dictionary<string, float> entropies) { wordEntropies = new Dictionary<string, float>(entropies); }
}
